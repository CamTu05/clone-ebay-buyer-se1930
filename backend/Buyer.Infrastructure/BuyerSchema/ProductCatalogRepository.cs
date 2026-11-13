using Buyer.Application.Catalog;
using Buyer.Application;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Buyer.Infrastructure.BuyerSchema.Models;
namespace Buyer.Infrastructure.BuyerSchema;
public sealed class ProductCatalogRepository(BuyerSchemaContext db) : IProductCatalogRepository
{
    public Task<ProductCatalogPage> GetAvailableProductsAsync(int page, int pageSize, DateTime now, CancellationToken ct) => SearchAsync(new() { Page = page, PageSize = pageSize }, now, ct);
    public async Task<ProductCatalogPage> SearchAsync(ProductListRequest r, DateTime now, CancellationToken ct)
    {
        if (r.Min > r.Max) throw new BuyerRuleException("PRICE_RANGE_INVALID", "Minimum price exceeds maximum price.");
        var l = db.Listings.AsNoTracking().Where(x => x.productId != null && x.status == "ACTIVE" && x.quantity > 0 && x.seller.accountStatus == "ACTIVE" && x.category.isActive && (x.publishedAt == null || x.publishedAt <= now) && (x.endedAt == null || x.endedAt > now) && (x.listingType == "FIXED_PRICE" && x.price != null || x.listingType == "AUCTION" && x.Auction != null && x.Auction.status == "ACTIVE" && x.Auction.startTime <= now && x.Auction.endTime > now));
        l = ApplyKeywordMatch(l, r.Q, r.Match);
        if (!string.IsNullOrWhiteSpace(r.Category)) l = l.Where(x => x.category.name == r.Category || x.category.slug == r.Category);
        if (!string.IsNullOrWhiteSpace(r.Condition))
        {
            var conditions = r.Condition.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var includeNew = conditions.Contains("New");
            var includeUsed = conditions.Contains("Used");
            var includeNotSpecified = conditions.Contains("Not specified");
            l = l.Where(listing =>
                includeNew && listing.itemCondition == "New" ||
                includeUsed && listing.itemCondition != null && listing.itemCondition.StartsWith("Used") ||
                includeNotSpecified && (listing.itemCondition == null || listing.itemCondition == ""));
        }
        if (r.Type != null) l = l.Where(x => x.listingType == r.Type);
        if (r.MinBids > r.MaxBids) throw new BuyerRuleException("BID_RANGE_INVALID", "Minimum bid count exceeds maximum bid count.");
        if (r.MinQuantity > r.MaxQuantity) throw new BuyerRuleException("QUANTITY_RANGE_INVALID", "Minimum quantity exceeds maximum quantity.");
        if (r.EndingWithinMinutes is int minutes) { var end = now.AddMinutes(minutes); l = l.Where(x => x.listingType == "AUCTION" && x.Auction != null && x.Auction.endTime <= end); }
        if (r.MinBids is int minBids) l = l.Where(x => x.listingType == "AUCTION" && x.Auction != null && x.Auction.bidCount >= minBids);
        if (r.MaxBids is int maxBids) l = l.Where(x => x.listingType == "AUCTION" && x.Auction != null && x.Auction.bidCount <= maxBids);
        if (r.MinQuantity is int minQuantity) l = l.Where(x => x.quantity >= minQuantity);
        if (r.MaxQuantity is int maxQuantity) l = l.Where(x => x.quantity <= maxQuantity);
        if (r.FreeShipping == true) l = l.Where(x => x.ShippingPolicy != null && x.ShippingPolicy.isActive && x.ShippingPolicy.ShippingOptions.Any(o => o.isActive && o.flatRate == 0));
        if (r.LocalPickup == true) l = l.Where(x => x.ShippingPolicy != null && x.ShippingPolicy.isActive && x.ShippingPolicy.ShippingOptions.Any(o => o.isActive && o.serviceType == "LOCAL_PICKUP"));
        if (r.ReturnsAccepted == true) l = l.Where(x => x.ReturnPolicy != null && x.ReturnPolicy.isActive && x.ReturnPolicy.returnsAccepted);
        if (r.FreeReturns == true) l = l.Where(x => x.ReturnPolicy != null && x.ReturnPolicy.isActive && x.ReturnPolicy.returnsAccepted && x.ReturnPolicy.freeReturns);
        if (r.DealsOnly == true) l = l.Where(x => x.ListingPromotion != null && x.ListingPromotion.isActive && x.ListingPromotion.promotionType == "DEAL" && x.ListingPromotion.startsAt <= now && x.ListingPromotion.endsAt > now);
        if (r.SaleItems == true) l = l.Where(x => x.ListingPromotion != null && x.ListingPromotion.isActive && x.ListingPromotion.promotionType == "SALE" && x.ListingPromotion.startsAt <= now && x.ListingPromotion.endsAt > now);
        if (!string.IsNullOrWhiteSpace(r.ItemCountry)) { var country = r.ItemCountry.ToUpper(); l = l.Where(x => x.itemCountryCode == country); }
        if (!string.IsNullOrWhiteSpace(r.AvailableTo)) { var country = r.AvailableTo.ToUpper(); l = l.Where(x => x.ShippingPolicy != null && x.ShippingPolicy.ShippingPolicyDestinationCountries.Any(d => d.countryCode == country)); }
        if (!string.IsNullOrWhiteSpace(r.Sellers)) { var names = r.Sellers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(x => x.ToUpper()).Distinct().ToArray(); l = r.SellerMode == "exclude" ? l.Where(x => !names.Contains(x.seller.username.ToUpper())) : l.Where(x => names.Contains(x.seller.username.ToUpper())); }
        if (r.Min != null) l = l.Where(x => (x.listingType == "AUCTION" ? x.Auction!.currentPrice : x.ListingPromotion != null && x.ListingPromotion.isActive && x.ListingPromotion.startsAt <= now && x.ListingPromotion.endsAt > now ? x.ListingPromotion.salePrice : x.price) >= r.Min);
        if (r.Max != null) l = l.Where(x => (x.listingType == "AUCTION" ? x.Auction!.currentPrice : x.ListingPromotion != null && x.ListingPromotion.isActive && x.ListingPromotion.startsAt <= now && x.ListingPromotion.endsAt > now ? x.ListingPromotion.salePrice : x.price) <= r.Max);
        var rows = db.Products.AsNoTracking().Where(p => p.category.isActive && l.Any(x => x.productId == p.id)).Select(p => new
        {
            p.id,
            p.title,
            Listing = l.Where(x => x.productId == p.id).OrderBy(x => x.listingType == "AUCTION" ? x.Auction!.currentPrice : x.ListingPromotion != null && x.ListingPromotion.isActive && x.ListingPromotion.startsAt <= now && x.ListingPromotion.endsAt > now ? x.ListingPromotion.salePrice : x.price).ThenBy(x => x.id).Select(x => new { x.id, x.listingType, price = x.listingType == "AUCTION" ? x.Auction!.currentPrice : x.ListingPromotion != null && x.ListingPromotion.isActive && x.ListingPromotion.startsAt <= now && x.ListingPromotion.endsAt > now ? x.ListingPromotion.salePrice : x.price!.Value, x.quantity, bidCount = x.Auction == null ? (int?)null : x.Auction.bidCount, endTime = x.Auction == null ? (DateTime?)null : x.Auction.endTime, publishedAt = x.publishedAt ?? x.createdAt }).First(),
            image = p.ProductImages.OrderByDescending(x => x.isPrimary).ThenBy(x => x.sortOrder).Select(x => x.imageUrl).FirstOrDefault()
        });
        var count = await rows.CountAsync(ct);
        var ordered = r.Sort switch { "price-low" => rows.OrderBy(x => x.Listing.price).ThenBy(x => x.id), "price-high" => rows.OrderByDescending(x => x.Listing.price).ThenBy(x => x.id), "name" => rows.OrderBy(x => x.title).ThenBy(x => x.id), "newest" => rows.OrderByDescending(x => x.Listing.publishedAt).ThenByDescending(x => x.id), "ending-soon" => rows.OrderBy(x => x.Listing.endTime).ThenBy(x => x.id), "bids-low" => rows.OrderBy(x => x.Listing.bidCount).ThenBy(x => x.id), "bids-high" => rows.OrderByDescending(x => x.Listing.bidCount).ThenBy(x => x.id), "quantity-low" => rows.OrderBy(x => x.Listing.quantity).ThenBy(x => x.id), "quantity-high" => rows.OrderByDescending(x => x.Listing.quantity).ThenBy(x => x.id), _ => rows.OrderBy(x => x.id) };
        var skip = ((long)r.Page - 1) * r.PageSize;
        if (skip >= count) return new([], count);
        var data = await ordered.Skip((int)skip).Take(r.PageSize).ToListAsync(ct);
        return new(data.Select(x => new ProductSummaryResponse(x.id, x.Listing.id, x.title, x.Listing.price, x.image, x.Listing.listingType)).ToArray(), count);
    }

    private static IQueryable<Listing> ApplyKeywordMatch(IQueryable<Listing> listings, string? query, string? match)
    {
        if (string.IsNullOrWhiteSpace(query)) return listings;
        var phrase = query.Trim();
        var words = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (match == "exact") return listings.Where(listing => listing.title.Contains(phrase));
        if (match == "any")
        {
            var listing = Expression.Parameter(typeof(Listing), "listing");
            var title = Expression.Property(listing, nameof(Listing.title));
            var contains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
            Expression body = Expression.Constant(false);
            foreach (var word in words) body = Expression.OrElse(body, Expression.Call(title, contains, Expression.Constant(word)));
            return listings.Where(Expression.Lambda<Func<Listing, bool>>(body, listing));
        }
        foreach (var word in words)
        {
            var value = match == "exact-any-order" ? $" {word} " : word;
            listings = match == "exact-any-order"
                ? listings.Where(listing => (" " + listing.title + " ").Contains(value))
                : listings.Where(listing => listing.title.Contains(value));
        }
        return listings;
    }
}
