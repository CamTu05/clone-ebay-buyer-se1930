using Buyer.Application.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Buyer.Infrastructure.Data;

public sealed class ProductRepository(CloneEbayDbContext db) : IProductRepository
{
    // Only public listings from active sellers are exposed; private account fields never enter DTOs.
    public async Task<ProductDetailResponse?> GetDetailAsync(int productId, CancellationToken cancellationToken)
    {
        return await db.Products.AsNoTracking()
            .Where(p => p.id == productId && p.category.isActive
                && p.Listings.Any(l => l.status == "ACTIVE" && l.seller.accountStatus == "ACTIVE"))
            .Select(p => new ProductDetailResponse(p.id, p.title, p.description, p.brand, p.itemCondition,
                p.ProductImages.OrderByDescending(i => i.isPrimary).ThenBy(i => i.sortOrder).ThenBy(i => i.id)
                    .Select(i => new ProductImageResponse(i.id, i.imageUrl, i.isPrimary)).ToList(),
                p.Listings.Where(l => l.status == "ACTIVE" && l.seller.accountStatus == "ACTIVE")
                    .OrderBy(l => l.id)
                    .Select(l => new ProductListingResponse(l.id, l.listingType,
                        l.listingType == "AUCTION" ? (l.Auction == null ? null : l.Auction.currentPrice) : l.price,
                        new SellerResponse(l.seller.id, l.seller.username, l.seller.avatarURL,
                            l.seller.Store == null ? null : l.seller.Store.storeName))).ToList()))
            .SingleOrDefaultAsync(cancellationToken);
    }

}
