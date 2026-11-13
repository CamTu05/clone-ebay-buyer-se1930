using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using System.Data;
using Buyer.Application;
using Buyer.Infrastructure.BuyerSchema.Models;
using Microsoft.EntityFrameworkCore;
namespace Buyer.Infrastructure.BuyerSchema;

public sealed record CheckoutRequest(int AddressId, string Method, [StringLength(50)] string? CouponCode, Dictionary<int, int>? ShippingOptions, int? ListingId = null, int Quantity = 1, int? AuctionId = null, string? IdempotencyKey = null, GuestContact? Guest = null, List<CheckoutSelection>? Items = null);
public sealed record CheckoutSelection(int ListingId, int Quantity);
public sealed record GuestContact([Required, EmailAddress, StringLength(255)] string Email, [Required, StringLength(100)] string FullName, [Required, StringLength(30)] string Phone, [Required, StringLength(255)] string Street, [Required, StringLength(100)] string City, [Required, StringLength(2, MinimumLength = 2)] string CountryCode, string? PostalCode = null);
public sealed class BuyerWorkflowService(BuyerSchemaContext db, IConfiguration? configuration = null)
{
    static BuyerRuleException Fail(string code, string message, int status = 409) => new(code, message, status);
    public async Task RequireBuyer(int id, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(x => x.id == id && x.role == "BUYER" && x.accountStatus == "ACTIVE" && x.emailVerified, ct)) throw Fail("ACCOUNT_UNAVAILABLE", "Active verified buyer required.", 403);
    }
    public async Task AddCart(int uid, int listingId, int quantity, CancellationToken ct)
    {
        if (quantity < 1 || quantity > 1000) throw Fail("QUANTITY_INVALID", "Quantity must be between 1 and 1000.", 400);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await RequireBuyer(uid, ct);
        var l = await db.Listings.Include(x => x.seller).SingleOrDefaultAsync(x => x.id == listingId, ct);
        if (l == null || l.status != "ACTIVE" || l.listingType != "FIXED_PRICE" || l.price == null || l.seller.accountStatus != "ACTIVE" || l.sellerId == uid || (l.publishedAt > DateTime.UtcNow) || (l.endedAt <= DateTime.UtcNow)) throw Fail("LISTING_UNAVAILABLE", "Listing cannot be added to cart.");
        var cart = await db.Carts.Include(x => x.CartItems).SingleOrDefaultAsync(x => x.userId == uid, ct);
        if (cart == null) { cart = new Cart { userId = uid }; db.Carts.Add(cart); }
        var item = cart.CartItems.SingleOrDefault(x => x.listingId == listingId);
        var total = (item?.status == "ACTIVE" ? item.quantity : 0) + quantity;
        if (total > 1000 || total > l.quantity) throw Fail("INSUFFICIENT_STOCK", "Requested quantity exceeds available stock.");
        if (item == null) cart.CartItems.Add(new CartItem { listingId = listingId, quantity = quantity, status = "ACTIVE" });
        else { item.quantity = total; item.status = "ACTIVE"; }
        cart.updatedAt = DateTime.UtcNow; await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task<object> Watchlist(int uid, CancellationToken ct)
    {
        await RequireBuyer(uid, ct);
        return await db.Watchlists.Where(x => x.userId == uid).OrderByDescending(x => x.id).Select(x => new { x.listingId, productId = x.listing.productId, name = x.listing.title, price = x.listing.price ?? (x.listing.Auction != null ? x.listing.Auction.currentPrice : 0), x.listing.listingType, imageUrl = x.listing.ListingImages.OrderByDescending(i => i.isPrimary).Select(i => i.imageUrl).FirstOrDefault(), x.listing.status }).ToListAsync(ct);
    }
    public async Task SaveWatch(int uid, int lid, bool save, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct); await RequireBuyer(uid, ct);
        var row = await db.Watchlists.SingleOrDefaultAsync(x => x.userId == uid && x.listingId == lid, ct);
        if (save && row == null)
        {
            if (!await db.Listings.AnyAsync(x => x.id == lid && x.status == "ACTIVE" && x.seller.accountStatus == "ACTIVE", ct)) throw Fail("LISTING_NOT_FOUND", "Listing not found.", 404);
            db.Watchlists.Add(new Watchlist { userId = uid, listingId = lid });
        }
        if (!save && row != null) db.Watchlists.Remove(row);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task<object> Notifications(int uid, CancellationToken ct) { await RequireBuyer(uid, ct); return await db.Notifications.AsNoTracking().Where(x => x.userId == uid).OrderByDescending(x => x.id).Take(100).Select(x => new { x.id, x.type, x.title, x.content, x.referenceType, x.referenceId, x.isRead, x.createdAt }).ToListAsync(ct); }
    public async Task ReadNotification(int uid, long id, CancellationToken ct) { await RequireBuyer(uid, ct); if (await db.Notifications.Where(x => x.id == id && x.userId == uid).ExecuteUpdateAsync(s => s.SetProperty(x => x.isRead, true).SetProperty(x => x.readAt, DateTime.UtcNow), ct) == 0) throw Fail("NOT_FOUND", "Notification not found.", 404); }
    public async Task<object> Orders(int uid, CancellationToken ct) { await RequireBuyer(uid, ct); return await db.OrderTables.AsNoTracking().Where(x => x.buyerId == uid).OrderByDescending(x => x.id).Take(100).Select(x => new { x.id, x.checkoutGroupId, checkoutStatus = x.checkoutGroup.status, x.checkoutGroup.paymentDeadline, x.status, x.orderDate, x.subtotal, x.discountAmount, x.shippingFee, x.taxAmount, x.totalAmount, x.currency, cancellations = x.CancellationRequests.Select(c => new { c.id, c.status, c.reason }), returns = x.ReturnRequests.Select(r => new { r.id, r.status, r.reasonDetail }), seller = x.seller.username, items = x.OrderItems.Select(i => new { i.id, i.listingId, i.productId, i.productTitle, i.quantity, i.unitPrice, i.imageUrl }).ToList(), shipping = x.ShippingInfo == null ? null : new { x.ShippingInfo.status, x.ShippingInfo.carrier, x.ShippingInfo.trackingNumber, x.ShippingInfo.deliveredAt } }).ToListAsync(ct); }
    public async Task<object> Checkout(int uid, CheckoutRequest r, bool preview, CancellationToken ct)
    {
        if (r.AuctionId != null && (r.ListingId != null || r.Items != null)) throw Fail("SELECTION_INVALID", "Choose a single checkout mode.", 400);
        if (r.Method != "COD" && r.Method != "PAYPAL") throw Fail("METHOD_INVALID", "Choose COD or PAYPAL.", 400);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (uid != 0) await RequireBuyer(uid, ct);
        else if (r.Guest == null || !Validator.TryValidateObject(r.Guest, new ValidationContext(r.Guest), null, true)
            || string.IsNullOrWhiteSpace(r.Guest.Street) || string.IsNullOrWhiteSpace(r.Guest.FullName) || string.IsNullOrWhiteSpace(r.Guest.City)
            || r.Guest.PostalCode?.Length > 20 || r.AuctionId != null)
            throw Fail("GUEST_INVALID", "Valid guest contact and shipping address required.", 400);
        var scope = uid == 0 ? "guest:" + r.Guest!.Email.Trim().ToLowerInvariant() : "checkout:" + uid;
        if (uid == 0 && string.IsNullOrWhiteSpace(r.IdempotencyKey)) throw Fail("KEY_REQUIRED", "Checkout key required.", 400);
        if (r.IdempotencyKey != null && (r.IdempotencyKey.Length < 16 || r.IdempotencyKey.Length > 100)) throw Fail("KEY_INVALID", "Invalid checkout key.", 400);
        var requestHash = AccountLifecycleService.HashToken(JsonSerializer.Serialize(r));
        if (!preview && r.IdempotencyKey != null)
        {
            var receipt = await db.OperationReceipts.FromSqlInterpolated($"SELECT * FROM dbo.OperationReceipt WITH (UPDLOCK,HOLDLOCK) WHERE scope={scope} AND operationKey={r.IdempotencyKey}").SingleOrDefaultAsync(ct);
            if (receipt != null)
            {
                if (receipt.requestHash != requestHash) throw Fail("KEY_REUSED", "Checkout key belongs to a different request.");
                return JsonSerializer.Deserialize<JsonElement>(ReceiptCipher.Unprotect(receipt.responseJson, configuration!));
            }
        }
        var user = uid == 0 ? new User { email = r.Guest!.Email.Trim() } : await db.Users.SingleAsync(x => x.id == uid, ct);
        var address = uid == 0 ? new Address { fullName = r.Guest!.FullName, phone = r.Guest.Phone, street = r.Guest.Street, city = r.Guest.City, country = r.Guest.CountryCode.ToUpperInvariant(), countryCode = r.Guest.CountryCode.ToUpperInvariant(), postalCode = r.Guest.PostalCode } : await db.Addresses.SingleOrDefaultAsync(x => x.id == r.AddressId && x.userId == uid, ct) ?? throw Fail("ADDRESS_NOT_FOUND", "Shipping address not found.", 404);
        if (r.ListingId.HasValue && (r.ListingId <= 0 || r.Quantity < 1 || r.Quantity > 1000))
            throw Fail("BUY_NOW_INVALID", "Choose a valid listing and quantity between 1 and 1000.", 400);
        List<CartItem> items;
        Auction? auction = null;
        if (r.AuctionId != null)
        {
            auction = await db.Auctions.FromSqlInterpolated($"SELECT * FROM dbo.Auction WITH (UPDLOCK,HOLDLOCK) WHERE id={r.AuctionId.Value}").Include(x => x.listing).ThenInclude(x => x.seller).Include(x => x.listing).ThenInclude(x => x.ListingImages).SingleOrDefaultAsync(ct);
            if (auction == null || auction.status != "ENDED" || auction.winnerId != uid || auction.winningPrice == null || auction.resolvedAt == null || auction.resolvedAt < DateTime.UtcNow.AddHours(-48)) throw Fail("AUCTION_CHECKOUT_UNAVAILABLE", "Only the winner can pay for this auction within 48 hours.");
            if (await db.OrderItems.AnyAsync(x => x.listingId == auction.listingId && x.order.status != "CANCELLED", ct)) throw Fail("AUCTION_ORDER_EXISTS", "An order already exists for this auction.");
            items = [new CartItem { listingId = auction.listingId, listing = auction.listing, quantity = 1 }];
        }
        else if (uid == 0)
        {
            var selections = r.ListingId.HasValue ? new List<CheckoutSelection> { new(r.ListingId.Value, r.Quantity) } : r.Items;
            if (selections == null || selections.Count == 0 || selections.Count > 100 || selections.Any(x => x.ListingId <= 0 || x.Quantity < 1 || x.Quantity > 1000) || selections.Select(x => x.ListingId).Distinct().Count() != selections.Count) throw Fail("ITEMS_INVALID", "Choose 1–100 valid items.", 400);
            var ids = selections.Select(x => x.ListingId).ToArray();
            var listings = await db.Listings.Include(x => x.seller).Include(x => x.ListingImages).Where(x => ids.Contains(x.id)).ToListAsync(ct);
            if (listings.Count != selections.Count) throw Fail("LISTING_NOT_FOUND", "Listing not found.", 404);
            items = selections.OrderBy(x => x.ListingId).Select(x => new CartItem { listingId = x.ListingId, listing = listings.Single(l => l.id == x.ListingId), quantity = x.Quantity }).ToList();
        }
        else if (r.ListingId.HasValue)
        {
            var listing = await db.Listings.Include(x => x.seller).Include(x => x.ListingImages).Include(x => x.ListingPromotion)
                .SingleOrDefaultAsync(x => x.id == r.ListingId.Value, ct)
                ?? throw Fail("LISTING_NOT_FOUND", "Listing not found.", 404);
            // A checkout selection only: never attach this item to the buyer's cart.
            items = [new CartItem { listingId = listing.id, listing = listing, quantity = r.Quantity }];
        }
        else items = await db.CartItems.Include(x => x.listing).ThenInclude(x => x.seller).Include(x => x.listing).ThenInclude(x => x.ListingImages).Include(x => x.listing).ThenInclude(x => x.ListingPromotion).Where(x => x.cart.userId == uid && x.status == "ACTIVE").OrderBy(x => x.listingId).ToListAsync(ct);
        if (items.Count == 0) throw Fail("CART_EMPTY", "Your cart is empty.");
        var now = DateTime.UtcNow;
        foreach (var item in items) { var l = item.listing; if ((auction == null ? l.status != "ACTIVE" || l.listingType != "FIXED_PRICE" || l.price == null || l.endedAt <= now : l.status != "SOLD") || l.quantity < item.quantity || l.seller.accountStatus != "ACTIVE" || l.sellerId == uid || l.publishedAt > now) throw Fail("CART_UNAVAILABLE", "An item is unavailable. Review your selection."); }
        decimal UnitPrice(CartItem item)
        {
            if (auction != null && auction.winningPrice.HasValue) return auction.winningPrice.Value;
            var promo = item.listing.ListingPromotion;
            if (promo != null && promo.isActive && promo.startsAt <= now && promo.endsAt > now)
            {
                return promo.salePrice;
            }
            return item.listing.price!.Value;
        }
        var subtotal = items.Sum(x => x.quantity * UnitPrice(x)); decimal discount = 0;
        Coupon? coupon = null;
        if (!string.IsNullOrWhiteSpace(r.CouponCode))
        {
            coupon = await db.Coupons.SingleOrDefaultAsync(x => x.code == r.CouponCode && x.isActive && x.startDate <= now && x.endDate > now, ct);
            if (coupon == null || coupon.usageCount >= coupon.maxUsage || subtotal < (coupon.minOrderValue ?? 0)) throw Fail("COUPON_INVALID", "Coupon cannot be applied.");
            discount = coupon.discountType == "PERCENT" ? decimal.Round(subtotal * coupon.discountValue / 100, 2) : coupon.discountValue;
            discount = Math.Min(subtotal, Math.Min(discount, coupon.maxDiscountAmount ?? discount));
        }
        var orders = new List<OrderTable>(); decimal remaining = discount;
        var sellers = items.GroupBy(x => x.listing.sellerId).ToArray();
        foreach (var seller in sellers)
        {
            var policies = seller.Select(x => x.listing.shippingPolicyId).Distinct().ToArray();
            var options = await db.ShippingOptions.Where(x => x.isActive && x.shippingPolicy.isActive && x.shippingPolicy.sellerId == seller.Key && policies.Contains(x.shippingPolicyId) && x.shippingPolicy.ShippingPolicyDestinationCountries.Any(d => d.countryCode == address.countryCode)).OrderBy(x => x.flatRate).ToListAsync(ct);
            if (policies.Length != 1 || policies[0] == null) throw Fail("SHIPPING_UNAVAILABLE", "Items from this seller require compatible shipping policies.");
            ShippingOption? option = r.ShippingOptions != null && r.ShippingOptions.TryGetValue(seller.Key, out var selected) ? options.SingleOrDefault(x => x.id == selected) : options.FirstOrDefault();
            if (option == null) throw Fail("SHIPPING_UNAVAILABLE", "No valid shipping option for seller.");
            var sub = seller.Sum(x => x.quantity * UnitPrice(x));
            var part = seller == sellers.Last() ? remaining : Math.Min(remaining, (subtotal == 0 ? 0 : decimal.Round(discount * sub / subtotal, 2))); remaining -= part;
            var order = new OrderTable { buyerId = uid == 0 ? null : uid, sellerId = seller.Key, addressId = uid == 0 ? null : address.id, shippingOptionId = option.id, shippingFullName = address.fullName, shippingEmail = user.email, shippingPhone = address.phone, shippingStreet = address.street, shippingAddressLine2 = address.addressLine2, shippingCity = address.city, shippingState = address.state, shippingPostalCode = address.postalCode, shippingCountry = address.country, shippingCountryCode = address.countryCode, shippingServiceName = option.serviceName, subtotal = sub, discountAmount = part, shippingFee = option.flatRate, totalAmount = sub - part + option.flatRate, currency = "VND", status = r.Method == "COD" ? "CONFIRMED" : "PLACED" };
            foreach (var item in seller) order.OrderItems.Add(new OrderItem { listingId = item.listingId, productId = item.listing.productId, quantity = item.quantity, unitPrice = UnitPrice(item), productTitle = item.listing.title, imageUrl = item.listing.ListingImages.OrderByDescending(x => x.isPrimary).Select(x => x.imageUrl).FirstOrDefault() });
            var taxRate = configuration?.GetValue<decimal>("Checkout:TaxRates:" + address.countryCode?.ToUpperInvariant()) ?? 0m;
            if (taxRate < 0 || taxRate > 1) throw Fail("TAX_CONFIGURATION_INVALID", "Tax rate is not configured correctly.", 503);
            order.taxAmount = decimal.Round((sub - part) * taxRate, 2, MidpointRounding.AwayFromZero);
            order.totalAmount += order.taxAmount;
            orders.Add(order);
        }
        var shipping = orders.Sum(x => x.shippingFee);
        var tax = orders.Sum(x => x.taxAmount);
        if (preview) return new { subtotal, discountAmount = discount, taxAmount = tax, shippingFee = shipping, totalAmount = subtotal - discount + shipping + tax, currency = "VND", items = orders.SelectMany(x => x.OrderItems).Select(x => new { x.listingId, x.productId, x.productTitle, x.quantity, x.unitPrice, x.imageUrl, x.order.sellerId }), sellers = orders.Select(x => new { x.sellerId, x.shippingOptionId, x.shippingServiceName, x.totalAmount }), shippingOptions = await db.ShippingOptions.Where(x => x.isActive && x.shippingPolicy.isActive && items.Select(i => i.listing.shippingPolicyId).Contains(x.shippingPolicyId)).Select(x => new { x.id, sellerId = x.shippingPolicy.sellerId, x.serviceName, x.flatRate, x.estimatedMinDays, x.estimatedMaxDays }).ToListAsync(ct) };
        foreach (var item in items)
        {
            var updated = await db.Listings.Where(x => x.id == item.listingId && x.status == (auction == null ? "ACTIVE" : "SOLD") && x.quantity >= item.quantity).ExecuteUpdateAsync(s => s.SetProperty(x => x.quantity, x => x.quantity - item.quantity), ct);
            if (updated != 1) throw Fail("STOCK_CHANGED", "Stock changed; retry checkout.");
        }
        var group = new CheckoutGroup { buyerId = uid == 0 ? null : uid, guestEmail = uid == 0 ? user.email : null, couponId = coupon?.id, subtotal = subtotal, discountAmount = discount, taxAmount = tax, shippingFee = shipping, totalAmount = subtotal - discount + shipping + tax, currency = "VND", status = r.Method == "COD" ? "CONFIRMED" : "PENDING", createdAt = now, confirmedAt = r.Method == "COD" ? now : null, paymentDeadline = r.Method == "PAYPAL" ? now.AddMinutes(15) : null, OrderTables = orders };
        if (r.Method == "COD")
        {
            group.Payments.Add(new Payment { amount = group.totalAmount, method = "COD", status = "PENDING", provider = "COD" });
            if (coupon != null) { coupon.usageCount++; group.CouponUsage = new CouponUsage { couponId = coupon.id, userId = uid == 0 ? null : uid, discountAmount = discount }; }
        }
        db.CheckoutGroups.Add(group);
        if (uid != 0 && !r.ListingId.HasValue && r.AuctionId == null) db.CartItems.RemoveRange(items);
        if (uid != 0) db.Notifications.Add(new Notification { userId = uid, type = "ORDER_CREATED", title = "Order created", content = "Your checkout has been created." });
        string? guestToken = null;
        if (uid == 0) { guestToken = AccountLifecycleService.NewToken(); group.GuestCheckoutAccesses.Add(new GuestCheckoutAccess { tokenHash = AccountLifecycleService.HashToken(guestToken), expiresAt = now.AddDays(30) }); }
        await db.SaveChangesAsync(ct);
        var result = new { checkoutGroupId = group.id, group.totalAmount, group.taxAmount, group.currency, group.status, group.paymentDeadline, guestToken, orderIds = orders.Select(x => x.id).ToArray() };
        if (r.IdempotencyKey != null) { db.OperationReceipts.Add(new OperationReceipt { scope = scope, operationKey = r.IdempotencyKey, requestHash = requestHash, responseJson = ReceiptCipher.Protect(JsonSerializer.Serialize(result), configuration!) }); await db.SaveChangesAsync(ct); }
        await tx.CommitAsync(ct); return result;
    }
    public async Task<object> MockPayment(int uid, long gid, bool success, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct); if (uid != 0) await RequireBuyer(uid, ct);
        var g = await db.CheckoutGroups.Include(x => x.Payments).Include(x => x.OrderTables).Include(x => x.coupon).SingleOrDefaultAsync(x => x.id == gid && x.buyerId == (uid == 0 ? null : uid), ct) ?? throw Fail("NOT_FOUND", "Checkout not found.", 404);
        if (g.Payments.Any(x => x.status == "PAID")) return new { checkoutGroupId = g.id, status = "PAID", alreadyProcessed = true };
        if (g.status != "PENDING" || g.paymentDeadline == null || g.paymentDeadline <= DateTime.UtcNow) throw Fail("PAYMENT_UNAVAILABLE", "Checkout is not eligible for payment.");
        var now = DateTime.UtcNow;
        if (success && g.coupon != null)
        {
            if (!g.coupon.isActive || g.coupon.endDate <= now || g.coupon.usageCount >= g.coupon.maxUsage) throw Fail("COUPON_INVALID", "Coupon is no longer available; cancel checkout and retry.");
            g.coupon.usageCount++; g.CouponUsage = new CouponUsage { couponId = g.coupon.id, userId = uid == 0 ? null : uid, discountAmount = g.discountAmount };
        }
        g.Payments.Add(new Payment { amount = g.totalAmount, method = "PAYPAL", provider = "MOCK", transactionId = "MOCK-" + Guid.NewGuid().ToString("N"), status = success ? "PAID" : "FAILED", paidAt = success ? now : null, failureReason = success ? null : "Simulated payment failure" });
        if (uid != 0) db.Notifications.Add(new Notification { userId = uid, type = "PAYMENT", title = success ? "Payment received" : "Payment failed", content = success ? "Your order is confirmed." : "You can retry payment before the deadline." });
        if (success) { g.status = "CONFIRMED"; g.confirmedAt = now; foreach (var o in g.OrderTables) o.status = "CONFIRMED"; }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return new { checkoutGroupId = g.id, status = success ? "PAID" : "FAILED" };
    }
    public Task CancelPendingCheckout(int uid, long gid, CancellationToken ct) => CancelCheckout(uid, gid, false, ct);
    public Task ExpirePendingCheckout(int uid, long gid, CancellationToken ct) => CancelCheckout(uid, gid, true, ct);
    private async Task CancelCheckout(int uid, long gid, bool expiryOnly, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (!expiryOnly && uid != 0) await RequireBuyer(uid, ct);
        var g = await db.CheckoutGroups.Include(x => x.OrderTables).ThenInclude(x => x.OrderItems).SingleOrDefaultAsync(x => x.id == gid && x.buyerId == uid, ct) ?? throw Fail("NOT_FOUND", "Checkout not found.", 404);
        if (g.status == "CANCELLED" || g.status == "EXPIRED") return;
        if (expiryOnly && (g.paymentDeadline == null || g.paymentDeadline > DateTime.UtcNow)) return;
        if (g.status != "PENDING") throw Fail("CANCELLATION_UNAVAILABLE", "Use an order cancellation request for confirmed orders.");
        var now = DateTime.UtcNow;
        foreach (var o in g.OrderTables) { if (o.stockRestoredAt == null) { foreach (var i in o.OrderItems) await db.Listings.Where(x => x.id == i.listingId).ExecuteUpdateAsync(s => s.SetProperty(x => x.quantity, x => x.quantity + i.quantity), ct); o.stockRestoredAt = now; } o.status = "CANCELLED"; }
        g.status = g.paymentDeadline <= now ? "EXPIRED" : "CANCELLED"; g.inventoryReleasedAt = now;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task RequestCancellation(int uid, long orderId, string reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 1000) throw Fail("REASON_INVALID", "Reason is required, maximum 1000 characters.", 400);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct); await RequireBuyer(uid, ct);
        var o = await db.OrderTables.Include(x => x.ShippingInfo).SingleOrDefaultAsync(x => x.id == orderId && x.buyerId == uid, ct) ?? throw Fail("NOT_FOUND", "Order not found.", 404);
        if (o.status != "CONFIRMED" || o.ShippingInfo?.status is "IN_TRANSIT" or "DELIVERED" || await db.CancellationRequests.AnyAsync(x => x.orderId == orderId && x.status == "PENDING", ct)) throw Fail("CANCELLATION_UNAVAILABLE", "Order cannot be requested for cancellation.");
        db.CancellationRequests.Add(new CancellationRequest { orderId = orderId, requestedBy = uid, reason = reason, status = "PENDING" }); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task Review(int uid, long itemId, int rating, string? comment, CancellationToken ct)
    {
        if (rating < 1 || rating > 5 || comment?.Length > 4000) throw Fail("REVIEW_INVALID", "Rating must be 1–5; comment maximum 4000 characters.", 400);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct); await RequireBuyer(uid, ct);
        var i = await db.OrderItems.Include(x => x.order).ThenInclude(x => x.ShippingInfo).SingleOrDefaultAsync(x => x.id == itemId && x.order.buyerId == uid, ct) ?? throw Fail("NOT_FOUND", "Order item not found.", 404);
        if (i.productId == null || i.order.ShippingInfo?.status != "DELIVERED" || await db.Reviews.AnyAsync(x => x.orderItemId == itemId, ct)) throw Fail("REVIEW_UNAVAILABLE", "Only delivered, unreviewed products can be reviewed.");
        db.Reviews.Add(new Review { orderItemId = itemId, productId = i.productId.Value, reviewerId = uid, rating = rating, comment = comment }); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task Return(int uid, long itemId, int quantity, string reason, CancellationToken ct)
    {
        if (quantity < 1 || string.IsNullOrWhiteSpace(reason) || reason.Length > 1000) throw Fail("RETURN_INVALID", "Quantity and reason are required.", 400);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct); await RequireBuyer(uid, ct);
        var i = await db.OrderItems.Include(x => x.order).ThenInclude(x => x.ShippingInfo).SingleOrDefaultAsync(x => x.id == itemId && x.order.buyerId == uid, ct) ?? throw Fail("NOT_FOUND", "Order item not found.", 404);
        var returned = await db.ReturnRequestItems.Where(x => x.orderItemId == itemId && x.returnRequest.status != "REJECTED" && x.returnRequest.status != "CANCELLED").SumAsync(x => (int?)x.quantity, ct) ?? 0;
        if (i.order.ShippingInfo?.status != "DELIVERED" || quantity > i.quantity - returned) throw Fail("RETURN_UNAVAILABLE", "Item is not eligible for this return quantity.");
        db.ReturnRequests.Add(new ReturnRequest { orderId = i.orderId, requestedBy = uid, reasonCode = "BUYER_REQUEST", reasonDetail = reason, status = "REQUESTED", ReturnRequestItems = new[] { new ReturnRequestItem { orderItemId = itemId, quantity = quantity } } }); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task<object> Bid(int uid, int auctionId, decimal maximum, CancellationToken ct)
    {
        if (maximum <= 0 || maximum > 1000000000000m || decimal.Round(maximum, 2) != maximum) throw Fail("BID_INVALID", "Enter a positive maximum bid with at most two decimal places.", 400);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct); await RequireBuyer(uid, ct);
        var a = await db.Auctions.FromSqlInterpolated($"SELECT * FROM dbo.Auction WITH (UPDLOCK,HOLDLOCK) WHERE id={auctionId}").Include(x => x.listing).ThenInclude(x => x.seller).Include(x => x.Bids).SingleOrDefaultAsync(x => x.id == auctionId, ct) ?? throw Fail("NOT_FOUND", "Auction not found.", 404);
        var now = DateTime.UtcNow;
        if (a.status != "ACTIVE" || a.startTime > now || a.endTime <= now || a.listing.status != "ACTIVE" || a.listing.sellerId == uid || a.listing.seller.accountStatus != "ACTIVE") throw Fail("AUCTION_UNAVAILABLE", "Auction is not accepting bids.");
        var previous = a.Bids.Where(x => x.bidderId == uid && x.status != "RETRACTED" && x.status != "CANCELLED").Select(x => (decimal?)x.maximumBid).Max();
        var minimum = a.bidCount == 0 ? a.startingPrice : a.highestBidderId == uid ? a.currentPrice : a.currentPrice + a.bidIncrement;
        if (maximum < minimum || maximum <= previous) throw Fail("BID_TOO_LOW", "Maximum bid must exceed your previous maximum and meet the current minimum.");
        a.Bids.Add(new Bid { bidderId = uid, maximumBid = maximum, effectiveBid = 0, status = "ACTIVE", createdAt = now });
        await db.SaveChangesAsync(ct);
        var bids = a.Bids.Where(x => x.status != "RETRACTED" && x.status != "CANCELLED").GroupBy(x => x.bidderId).Select(g => g.OrderByDescending(x => x.maximumBid).ThenBy(x => x.createdAt).ThenBy(x => x.id).First()).OrderByDescending(x => x.maximumBid).ThenBy(x => x.createdAt).ThenBy(x => x.id).ToArray();
        var winning = bids[0]; var price = bids.Length > 1 ? Math.Min(winning.maximumBid, bids[1].maximumBid + a.bidIncrement) : a.startingPrice;
        var previousLeader = a.highestBidderId;
        a.currentPrice = Math.Max(a.currentPrice, price); a.highestBidderId = winning.bidderId; a.bidCount++;
        foreach (var b in a.Bids.Where(x => x.status != "RETRACTED" && x.status != "CANCELLED")) { b.status = b.id == winning.id ? "WINNING" : "OUTBID"; b.effectiveBid = b.id == winning.id ? a.currentPrice : Math.Min(b.maximumBid, a.currentPrice); }
        if (previousLeader != null && previousLeader != a.highestBidderId) db.Notifications.Add(new Notification { userId = previousLeader.Value, type = "OUTBID", title = "You have been outbid", content = "Another buyer is leading auction #" + a.id, referenceType = "AUCTION", referenceId = a.id.ToString() });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return new { auctionId = a.id, a.currentPrice, a.bidCount, isHighestBidder = a.highestBidderId == uid };
    }
}
