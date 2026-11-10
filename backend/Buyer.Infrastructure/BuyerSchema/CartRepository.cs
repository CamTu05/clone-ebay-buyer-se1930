using Buyer.Application.Interfaces.Authentication;
using Buyer.Application.Interfaces.Cart;
using Microsoft.EntityFrameworkCore;

namespace Buyer.Infrastructure.BuyerSchema;

public sealed class CartRepository(BuyerSchemaContext db, IClock clock) : ICartRepository
{
    public async Task<CartResponse> GetAsync(int userId, CancellationToken cancellationToken = default)
    {
        await new BuyerWorkflowService(db).RequireBuyer(userId, cancellationToken);
        var cartId = await db.Carts.AsNoTracking()
            .Where(cart => cart.userId == userId)
            .Select(cart => (int?)cart.id)
            .SingleOrDefaultAsync(cancellationToken);

        if (cartId is null)
            return Empty();

        var items = await db.CartItems.AsNoTracking()
            .Where(item => item.cartId == cartId.Value && item.status == "ACTIVE")
            .OrderBy(item => item.id)
            .Select(item => new
            {
                item.id,
                item.listingId,
                ProductId = item.listing.productId ?? 0,
                Title = item.listing.title,
                ImageUrl = item.listing.ListingImages
                    .OrderByDescending(image => image.isPrimary)
                    .ThenBy(image => image.sortOrder)
                    .ThenBy(image => image.id)
                    .Select(image => image.imageUrl)
                    .FirstOrDefault(),
                SellerId = item.listing.seller.id,
                SellerUsername = item.listing.seller.username,
                StoreName = (string?)null,
                Price = item.listing.price,
                item.quantity,
                ListingStatus = item.listing.status,
                ListingType = item.listing.listingType,
                SellerStatus = item.listing.seller.accountStatus,
                AvailableQuantity = item.listing.quantity
            })
            .ToListAsync(cancellationToken);

        var responseItems = items.Select(item =>
        {
            var availableQuantity = Math.Max(0, item.AvailableQuantity);
            var reason = UnavailableReason(item.ListingStatus, item.ListingType,
                item.SellerStatus, item.Price, availableQuantity, item.quantity);
            var unitPrice = item.Price ?? 0m;
            return new CartItemResponse(
                item.id, item.listingId, item.ProductId, item.Title, item.ImageUrl,
                new CartSellerResponse(item.SellerId, item.SellerUsername, item.StoreName),
                unitPrice, item.quantity, unitPrice * item.quantity, availableQuantity,
                reason is null, reason);
        }).ToArray();

        return new CartResponse(
            cartId,
            responseItems,
            responseItems.Sum(item => item.ItemTotal),
            "VND",
            responseItems.Length > 0 && responseItems.All(item => item.IsAvailable));
    }

    public async Task<CartResult> UpdateQuantityAsync(int userId, int cartItemId, int quantity,
        CancellationToken cancellationToken = default)
    {
        await new BuyerWorkflowService(db).RequireBuyer(userId, cancellationToken);
        var item = await db.CartItems
            .Include(value => value.cart)

            .Include(value => value.listing).ThenInclude(listing => listing.seller)
            .SingleOrDefaultAsync(value => value.id == cartItemId && value.cart.userId == userId,
                cancellationToken);

        if (item is null)
            return CartResult.Failed(CartFailure.ItemNotFound);

        var available = item.listing.quantity;
        if (item.listing.publishedAt > clock.UtcNow || item.listing.endedAt <= clock.UtcNow)
            return CartResult.Failed(CartFailure.ListingUnavailable);
        if (item.listing.status != "ACTIVE" || item.listing.listingType != "FIXED_PRICE" ||
            item.listing.price is null || item.listing.seller.accountStatus != "ACTIVE")
            return CartResult.Failed(CartFailure.ListingUnavailable);
        if (quantity > available)
            return CartResult.Failed(CartFailure.QuantityExceedsStock);

        item.quantity = quantity;
        item.cart.updatedAt = clock.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return CartResult.Success(await GetAsync(userId, cancellationToken));
    }

    public async Task<CartResult> RemoveAsync(int userId, int cartItemId,
        CancellationToken cancellationToken = default)
    {
        var item = await db.CartItems
            .Include(value => value.cart)
            .SingleOrDefaultAsync(value => value.id == cartItemId && value.cart.userId == userId,
                cancellationToken);

        if (item is null)
            return CartResult.Failed(CartFailure.ItemNotFound);

        item.cart.updatedAt = clock.UtcNow;
        db.CartItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return CartResult.Success(await GetAsync(userId, cancellationToken));
    }

    private static CartResponse Empty() => new(null, [], 0m, "VND", false);

    private static string? UnavailableReason(string listingStatus, string listingType,
        string sellerStatus, decimal? price, int availableQuantity, int requestedQuantity)
    {
        if (listingStatus != "ACTIVE") return "LISTING_UNAVAILABLE";
        if (listingType != "FIXED_PRICE") return "LISTING_TYPE_UNSUPPORTED";
        if (sellerStatus != "ACTIVE") return "SELLER_UNAVAILABLE";
        if (price is null) return "PRICE_UNAVAILABLE";
        if (availableQuantity < requestedQuantity) return "INSUFFICIENT_STOCK";
        return null;
    }
}

