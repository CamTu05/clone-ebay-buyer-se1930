using Buyer.Application.Catalog;
using Buyer.Application;
using Microsoft.EntityFrameworkCore;
namespace Buyer.Infrastructure.BuyerSchema;
public sealed class ProductCatalogRepository(BuyerSchemaContext db) : IProductCatalogRepository
{
    public Task<ProductCatalogPage> GetAvailableProductsAsync(int page, int pageSize, DateTime now, CancellationToken ct) => SearchAsync(new() { Page = page, PageSize = pageSize }, now, ct);
    public async Task<ProductCatalogPage> SearchAsync(ProductListRequest r, DateTime now, CancellationToken ct)
    {
        if (r.Min > r.Max) throw new BuyerRuleException("PRICE_RANGE_INVALID", "Minimum price exceeds maximum price.");
        var l = db.Listings.AsNoTracking().Where(x => x.productId != null && x.status == "ACTIVE" && x.quantity > 0 && x.seller.accountStatus == "ACTIVE" && x.category.isActive && (x.publishedAt == null || x.publishedAt <= now) && (x.endedAt == null || x.endedAt > now) && (x.listingType == "FIXED_PRICE" && x.price != null || x.listingType == "AUCTION" && x.Auction != null && x.Auction.status == "ACTIVE" && x.Auction.startTime <= now && x.Auction.endTime > now));
        if (!string.IsNullOrWhiteSpace(r.Q)) foreach (var word in r.Q.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)) l = l.Where(x => x.title.Contains(word));
        if (!string.IsNullOrWhiteSpace(r.Category)) l = l.Where(x => x.category.name == r.Category || x.category.slug == r.Category);
        if (!string.IsNullOrWhiteSpace(r.Condition)) l = l.Where(x => x.itemCondition == r.Condition);
        if (r.Type != null) l = l.Where(x => x.listingType == r.Type);
        if (r.Min != null) l = l.Where(x => (x.listingType == "AUCTION" ? x.Auction!.currentPrice : x.price) >= r.Min);
        if (r.Max != null) l = l.Where(x => (x.listingType == "AUCTION" ? x.Auction!.currentPrice : x.price) <= r.Max);
        var rows = db.Products.AsNoTracking().Where(p => p.category.isActive && l.Any(x => x.productId == p.id)).Select(p => new
        {
            p.id,
            p.title,
            Listing = l.Where(x => x.productId == p.id).OrderBy(x => x.listingType == "AUCTION" ? x.Auction!.currentPrice : x.price).ThenBy(x => x.id).Select(x => new { x.id, x.listingType, price = x.listingType == "AUCTION" ? x.Auction!.currentPrice : x.price!.Value }).First(),
            image = p.ProductImages.OrderByDescending(x => x.isPrimary).ThenBy(x => x.sortOrder).Select(x => x.imageUrl).FirstOrDefault()
        });
        var count = await rows.CountAsync(ct);
        var ordered = r.Sort switch { "price-low" => rows.OrderBy(x => x.Listing.price).ThenBy(x => x.id), "price-high" => rows.OrderByDescending(x => x.Listing.price).ThenBy(x => x.id), "name" => rows.OrderBy(x => x.title).ThenBy(x => x.id), _ => rows.OrderBy(x => x.id) };
        var skip = ((long)r.Page - 1) * r.PageSize;
        if (skip >= count) return new([], count);
        var data = await ordered.Skip((int)skip).Take(r.PageSize).ToListAsync(ct);
        return new(data.Select(x => new ProductSummaryResponse(x.id, x.Listing.id, x.title, x.Listing.price, x.image, x.Listing.listingType)).ToArray(), count);
    }
}
