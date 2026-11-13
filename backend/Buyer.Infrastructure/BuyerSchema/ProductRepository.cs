using Buyer.Application.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Buyer.Infrastructure.BuyerSchema;

public sealed class ProductRepository(
    BuyerSchemaContext db,
    IOptionsSnapshot<UrgencyBadgeOptions> urgencyOptions) : IProductRepository
{
    // Only public listings from active sellers are exposed; private account fields never enter DTOs.
    public async Task<ProductDetailResponse?> GetDetailAsync(int productId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var product = await db.Products.AsNoTracking()
            .Where(p => p.id == productId && p.category.isActive && p.Listings.Any(l => l.status == "ACTIVE" && l.quantity > 0 && l.seller.accountStatus == "ACTIVE"
                && (l.publishedAt == null || l.publishedAt <= now) && (l.endedAt == null || l.endedAt > now)
                && (l.listingType == "FIXED_PRICE" && l.price != null || l.listingType == "AUCTION" && l.Auction != null && l.Auction.status == "ACTIVE" && l.Auction.startTime <= now && l.Auction.endTime > now)))
            .Select(p => new ProductDetailResponse(p.id, p.title, p.description, p.brand, p.Listings.Select(l => l.itemCondition).FirstOrDefault(),
                p.ProductImages.OrderByDescending(i => i.isPrimary).ThenBy(i => i.sortOrder).ThenBy(i => i.id)
                    .Select(i => new Buyer.Application.Catalog.ProductImageResponse(i.id, i.imageUrl, i.isPrimary)).ToList(),
                p.Listings.Where(l => l.status == "ACTIVE" && l.quantity > 0 && l.seller.accountStatus == "ACTIVE"
                        && (l.publishedAt == null || l.publishedAt <= now) && (l.endedAt == null || l.endedAt > now)
                        && (l.listingType == "FIXED_PRICE" && l.price != null || l.listingType == "AUCTION" && l.Auction != null && l.Auction.status == "ACTIVE" && l.Auction.startTime <= now && l.Auction.endTime > now))
                    .OrderBy(l => l.id)
                    .Select(l => new ProductListingResponse(l.id, l.listingType,
                        l.listingType == "AUCTION" ? l.Auction!.currentPrice : l.price,
                        new SellerResponse(l.seller.id, l.seller.username, l.seller.avatarURL,
                            null), l.Auction == null ? null : l.Auction.id, l.itemCondition)).ToList()))
            .SingleOrDefaultAsync(cancellationToken);

        if (product is null) return null;

        var options = urgencyOptions.Value;
        if (!options.Enabled) return product;

        var listingIds = product.Listings.Select(l => l.ListingId).ToList();

        var inCartCount = listingIds.Count > 0
            ? await db.CartItems.AsNoTracking()
                .Where(ci => listingIds.Contains(ci.listingId) && ci.status == "ACTIVE")
                .Select(ci => ci.cartId)
                .Distinct()
                .CountAsync(cancellationToken)
            : 0;

        var validStatuses = options.ValidOrderStatuses ?? ["COMPLETED", "CONFIRMED"];
        var soldCount = await db.OrderItems.AsNoTracking()
            .Where(oi => (oi.productId == productId || (listingIds.Count > 0 && listingIds.Contains(oi.listingId)))
                         && validStatuses.Contains(oi.order.status))
            .SumAsync(oi => (int?)oi.quantity, cancellationToken) ?? 0;

        var isCartEligible = inCartCount >= options.MinCartThreshold;
        var isSoldEligible = soldCount >= options.MinSoldThreshold;

        UrgencyBadgeResponse? badge = null;
        if (isCartEligible && isSoldEligible)
        {
            if (options.ShowBothIfEligible)
            {
                badge = new UrgencyBadgeResponse(
                    Show: true,
                    Text: $"In {inCartCount} carts · {soldCount} sold",
                    InCartCount: inCartCount,
                    SoldCount: soldCount,
                    BadgeType: "BOTH");
            }
            else if (string.Equals(options.PriorityMode, "SOLD_FIRST", StringComparison.OrdinalIgnoreCase))
            {
                badge = new UrgencyBadgeResponse(
                    Show: true,
                    Text: $"{soldCount} sold",
                    InCartCount: inCartCount,
                    SoldCount: soldCount,
                    BadgeType: "SOLD");
            }
            else
            {
                badge = new UrgencyBadgeResponse(
                    Show: true,
                    Text: $"In {inCartCount} carts",
                    InCartCount: inCartCount,
                    SoldCount: soldCount,
                    BadgeType: "CART");
            }
        }
        else if (isCartEligible)
        {
            badge = new UrgencyBadgeResponse(
                Show: true,
                Text: $"In {inCartCount} carts",
                InCartCount: inCartCount,
                SoldCount: soldCount,
                BadgeType: "CART");
        }
        else if (isSoldEligible)
        {
            badge = new UrgencyBadgeResponse(
                Show: true,
                Text: $"{soldCount} sold",
                InCartCount: inCartCount,
                SoldCount: soldCount,
                BadgeType: "SOLD");
        }

        return product with { UrgencyBadge = badge };
    }
}

