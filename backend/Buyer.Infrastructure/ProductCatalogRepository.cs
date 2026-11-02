using Buyer.Application.Catalog;
using Buyer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Buyer.Infrastructure.Catalog;

public sealed class ProductCatalogRepository(CloneEbayDbContext dbContext) : IProductCatalogRepository
{
    public async Task<ProductCatalogPage> GetAvailableProductsAsync(
        int page,
        int pageSize,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var listings = dbContext.Listings.AsNoTracking().Where(listing =>
            listing.status == "ACTIVE" &&
            listing.quantity > 0 &&
            (listing.publishedAt == null || listing.publishedAt <= utcNow) &&
            (listing.endedAt == null || listing.endedAt > utcNow) &&
            ((listing.listingType == "FIXED_PRICE" && listing.price != null) ||
             (listing.listingType == "AUCTION" && listing.Auction != null &&
              listing.Auction.status == "ACTIVE" &&
              listing.Auction.startTime <= utcNow && listing.Auction.endTime > utcNow)));

        var products = dbContext.Products.AsNoTracking()
            .Where(product => listings.Any(listing => listing.productId == product.id));

        var totalItems = await products.CountAsync(cancellationToken);
        var offset = ((long)page - 1) * pageSize;
        if (offset >= totalItems)
        {
            return new ProductCatalogPage([], totalItems);
        }

        var items = await products.OrderBy(product => product.id)
            .Skip((int)offset)
            .Take(pageSize)
            .Select(product => new
            {
                ProductId = product.id,
                Name = product.title,
                ImageUrl = product.ProductImages
                    .OrderByDescending(image => image.isPrimary)
                    .ThenBy(image => image.sortOrder)
                    .ThenBy(image => image.id)
                    .Select(image => image.imageUrl)
                    .FirstOrDefault(),
                Listing = listings.Where(listing => listing.productId == product.id)
                    .OrderBy(listing => listing.listingType == "AUCTION"
                        ? listing.Auction!.currentPrice
                        : listing.price!.Value)
                    .ThenBy(listing => listing.id)
                    .Select(listing => new
                    {
                        ListingId = listing.id,
                        Price = listing.listingType == "AUCTION"
                            ? listing.Auction!.currentPrice
                            : listing.price!.Value,
                        ListingType = listing.listingType
                    })
                    .First()
            })
            .Select(product => new ProductSummaryResponse(
                product.ProductId,
                product.Listing.ListingId,
                product.Name,
                product.Listing.Price,
                product.ImageUrl,
                product.Listing.ListingType))
            .ToListAsync(cancellationToken);

        return new ProductCatalogPage(items, totalItems);
    }
}
