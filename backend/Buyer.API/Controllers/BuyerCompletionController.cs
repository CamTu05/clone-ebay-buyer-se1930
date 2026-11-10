using System.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Buyer.Application;
using Buyer.Infrastructure.BuyerSchema;
using Buyer.Infrastructure.BuyerSchema.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Buyer.API.Controllers;
[ApiController, Route("api")]
public sealed class BuyerCompletionController(BuyerSchemaContext db, BuyerWorkflowService service, IConfiguration config) : ControllerBase
{
    int Uid => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    object Success(object data) => ApiEnvelope<object>.Succeeded(data);
    [HttpPost("guest/checkout/preview")]
    public async Task<object> GuestPreview(CheckoutRequest r, CancellationToken ct) => Success(await service.Checkout(0, r, true, ct));
    [HttpPost("guest/checkout")]
    public async Task<object> GuestCheckout(CheckoutRequest r, CancellationToken ct) => Success(await service.Checkout(0, r, false, ct));
    async Task GuestAccess(long id, CancellationToken ct)
    {
        var raw = Request.Headers["X-Guest-Token"].ToString();
        if (raw.Length != 64) throw new BuyerRuleException("GUEST_ACCESS_DENIED", "Valid guest access token required.", 401);
        var hash = AccountLifecycleService.HashToken(raw);
        if (!await db.GuestCheckoutAccesses.AnyAsync(x => x.checkoutGroupId == id && x.tokenHash == hash && x.revokedAt == null && x.expiresAt > DateTime.UtcNow, ct)) throw new BuyerRuleException("GUEST_ACCESS_DENIED", "Access denied or link expired.", 401);
    }
    [HttpGet("guest/checkout/{id:long}")]
    public async Task<object> GuestOrder(long id, CancellationToken ct) { await GuestAccess(id, ct); return Success(await db.CheckoutGroups.Where(x => x.id == id && x.buyerId == null).Select(x => new { x.id, x.status, x.totalAmount, x.taxAmount, x.paymentDeadline, orders = x.OrderTables.Select(o => new { o.id, o.status, items = o.OrderItems.Select(i => new { i.productTitle, i.quantity, i.unitPrice }) }) }).SingleAsync(ct)); }
    public sealed record GuestPayment(bool Success);
    [HttpPost("guest/checkout/{id:long}/mock-payment")]
    public async Task<object> GuestPay(long id, GuestPayment r, CancellationToken ct) { await GuestAccess(id, ct); if (!config.GetValue<bool>("Payments:EnableMock")) throw new BuyerRuleException("MOCK_DISABLED", "Mock payments disabled.", 403); return Success(await service.MockPayment(0, id, r.Success, ct)); }
    [HttpPost("guest/checkout/{id:long}/cancel")]
    public async Task<object> GuestCancel(long id, CancellationToken ct) { await GuestAccess(id, ct); await service.CancelPendingCheckout(0, id, ct); return Success(new { success = true }); }
    public sealed record MergeInput([Required, StringLength(100, MinimumLength = 16)] string Key, [Required, MinLength(1), MaxLength(100)] List<CheckoutSelection> Items);
    [Authorize(Roles = "BUYER"), HttpPost("cart/merge")]
    public async Task<object> Merge(MergeInput r, CancellationToken ct)
    {
        if (r.Items.Any(x => x.ListingId <= 0 || x.Quantity < 1 || x.Quantity > 1000) || r.Items.Select(x => x.ListingId).Distinct().Count() != r.Items.Count) throw new BuyerRuleException("ITEMS_INVALID", "Invalid cart items.", 400);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct); await service.RequireBuyer(Uid, ct);
        var scope = "merge:" + Uid; var hash = AccountLifecycleService.HashToken(JsonSerializer.Serialize(r.Items));
        var receipt = await db.OperationReceipts.FromSqlInterpolated($"SELECT * FROM dbo.OperationReceipt WITH(UPDLOCK,HOLDLOCK) WHERE scope={scope} AND operationKey={r.Key}").SingleOrDefaultAsync(ct);
        if (receipt != null) { if (receipt.requestHash != hash) throw new BuyerRuleException("KEY_REUSED", "Merge key reused for another request.", 409); return Success(JsonSerializer.Deserialize<JsonElement>(receipt.responseJson)); }
        var cart = await db.Carts.Include(x => x.CartItems).SingleOrDefaultAsync(x => x.userId == Uid, ct);
        if (cart == null) { cart = new Cart { userId = Uid }; db.Carts.Add(cart); }
        var outcomes = new List<object>();
        foreach (var input in r.Items.OrderBy(x => x.ListingId))
        {
            var l = await db.Listings.SingleOrDefaultAsync(x => x.id == input.ListingId && x.status == "ACTIVE" && x.listingType == "FIXED_PRICE" && x.price != null && x.seller.accountStatus == "ACTIVE" && x.sellerId != Uid && (x.endedAt == null || x.endedAt > DateTime.UtcNow) && (x.publishedAt == null || x.publishedAt <= DateTime.UtcNow), ct);
            var item = cart.CartItems.SingleOrDefault(x => x.listingId == input.ListingId);
            var existing = item?.status == "ACTIVE" ? item.quantity : 0;
            if (l == null || existing + input.Quantity > l.quantity || existing + input.Quantity > 1000) { outcomes.Add(new { input.ListingId, merged = false, reason = "Unavailable or insufficient stock" }); continue; }
            if (item == null) cart.CartItems.Add(new CartItem { listingId = input.ListingId, quantity = input.Quantity, status = "ACTIVE" }); else { item.status = "ACTIVE"; item.quantity = existing + input.Quantity; }
            outcomes.Add(new { input.ListingId, merged = true, reason = (string?)null });
        }
        var result = new { items = outcomes }; db.OperationReceipts.Add(new OperationReceipt { scope = scope, operationKey = r.Key, requestHash = hash, responseJson = JsonSerializer.Serialize(result) });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Success(result);
    }
    [HttpGet("products/{id:int}/reviews")]
    public async Task<object> Reviews(int id, CancellationToken ct) => Success(await db.Reviews.AsNoTracking().Where(x => x.productId == id).OrderByDescending(x => x.id).Take(100).Select(x => new { x.id, x.rating, x.comment, x.createdAt, buyer = x.reviewer.username }).ToListAsync(ct));
    public sealed record FeedbackInput([Required, RegularExpression("^(POSITIVE|NEUTRAL|NEGATIVE)$")] string Type, [StringLength(2000)] string? Comment);
    [Authorize(Roles = "BUYER"), HttpPost("orders/{id:long}/seller-feedback")]
    public async Task<object> Feedback(long id, FeedbackInput r, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct); await service.RequireBuyer(Uid, ct);
        var order = await db.OrderTables.Include(x => x.ShippingInfo).SingleOrDefaultAsync(x => x.id == id && x.buyerId == Uid, ct) ?? throw new BuyerRuleException("NOT_FOUND", "Order not found.", 404);
        if (order.ShippingInfo?.status != "DELIVERED" || await db.SellerFeedbacks.AnyAsync(x => x.orderId == id, ct)) throw new BuyerRuleException("FEEDBACK_UNAVAILABLE", "Feedback requires a delivered order without existing feedback.", 409);
        db.SellerFeedbacks.Add(new SellerFeedback { orderId = id, buyerId = Uid, sellerId = order.sellerId, feedbackType = r.Type, comment = r.Comment }); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Success(new { success = true });
    }
    [HttpGet("sellers/{id:int}/feedback")]
    public async Task<object> FeedbackList(int id, CancellationToken ct) => Success(await db.SellerFeedbacks.AsNoTracking().Where(x => x.sellerId == id).OrderByDescending(x => x.id).Take(100).Select(x => new { x.id, x.feedbackType, x.comment, x.createdAt }).ToListAsync(ct));
    [Authorize(Roles = "BUYER"), HttpGet("me/auctions")]
    public async Task<object> MyAuctions(CancellationToken ct) { await service.RequireBuyer(Uid, ct); return Success(await db.Auctions.AsNoTracking().Where(x => x.Bids.Any(b => b.bidderId == Uid)).OrderByDescending(x => x.id).Take(100).Select(x => new { x.id, x.listingId, x.listing.productId, x.listing.title, x.currentPrice, x.endTime, x.status, x.winningPrice, x.resolvedAt, isLeading = x.highestBidderId == Uid, isWinner = x.winnerId == Uid, ownMaximum = x.Bids.Where(b => b.bidderId == Uid).Max(b => b.maximumBid), checkoutGroupId = x.listing.OrderItems.Where(i => i.order.buyerId == Uid && i.order.status != "CANCELLED").Select(i => (long?)i.order.checkoutGroupId).FirstOrDefault() }).ToListAsync(ct)); }
}
