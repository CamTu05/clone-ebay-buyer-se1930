using Buyer.Application.Interfaces.ListingCatalog;
using Buyer.Infrastructure.Repositories.Addresses;
using Buyer.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Buyer.Infrastructure.Repositories.ListingCatalog;

public sealed class ListingCatalogRepository(CloneEbayDbContext dbContext) : IListingCatalogRepository
{
    public Task<ListingCatalogPage> GetAvailableListingsAsync(
        int page,
        int pageSize,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        GetAvailableListingsAsync(new ListingCatalogQuery(
            null, null, null, null, null, null, null, null, null, null,
            null, null, ListingCatalogSort.Newest, page, pageSize), utcNow, cancellationToken);

    public async Task<ListingCatalogPage> GetAvailableListingsAsync(
        ListingCatalogQuery query,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var candidates = AvailableListings(utcNow).Select(listing => new
        {
            Listing = listing,
            EffectivePrice = listing.listingType == "AUCTION"
                ? listing.Auction!.currentPrice
                : listing.price!.Value,
            TitleMatch = query.SearchPattern != null &&
                EF.Functions.Like(listing.title, query.SearchPattern, "\\"),
            DescriptionMatch = query.SearchPattern != null && listing.description != null &&
                EF.Functions.Like(listing.description, query.SearchPattern, "\\")
        });

        if (query.SearchPattern is not null)
            candidates = candidates.Where(item => item.TitleMatch || item.DescriptionMatch);
        if (query.CategoryId.HasValue)
            candidates = candidates.Where(item => item.Listing.categoryId == query.CategoryId);
        if (query.MinPrice.HasValue)
            candidates = candidates.Where(item => item.EffectivePrice >= query.MinPrice);
        if (query.MaxPrice.HasValue)
            candidates = candidates.Where(item => item.EffectivePrice <= query.MaxPrice);
        if (query.Condition is not null)
            candidates = candidates.Where(item => item.Listing.itemCondition == query.Condition);
        if (query.Brand is not null)
            candidates = candidates.Where(item => item.Listing.product != null && item.Listing.product.brand == query.Brand);
        if (query.ListingType is not null)
            candidates = candidates.Where(item => item.Listing.listingType == query.ListingType);
        if (query.SellerId.HasValue)
            candidates = candidates.Where(item => item.Listing.sellerId == query.SellerId);
        if (query.FreeShipping == true)
            candidates = candidates.Where(item => item.Listing.ShippingPolicy != null &&
                item.Listing.ShippingPolicy.isActive &&
                item.Listing.ShippingPolicy.ShippingOptions.Any(option => option.isActive && option.flatRate == 0));
        if (query.MinBidCount.HasValue)
            candidates = candidates.Where(item => item.Listing.listingType == "AUCTION" &&
                item.Listing.Auction!.bidCount >= query.MinBidCount);
        if (query.MaxBidCount.HasValue)
            candidates = candidates.Where(item => item.Listing.listingType == "AUCTION" &&
                item.Listing.Auction!.bidCount <= query.MaxBidCount);
        if (query.Sort == ListingCatalogSort.EndingSoon)
            candidates = candidates.Where(item => item.Listing.listingType == "AUCTION");

        var totalItems = await candidates.CountAsync(cancellationToken);
        var offset = ((long)query.Page - 1) * query.PageSize;
        if (offset >= totalItems)
            return new ListingCatalogPage([], totalItems);

        var ordered = query.Sort switch
        {
            ListingCatalogSort.Relevance => candidates
                .OrderByDescending(item => item.TitleMatch)
                .ThenByDescending(item => item.DescriptionMatch)
                .ThenByDescending(item => item.Listing.publishedAt ?? item.Listing.createdAt)
                .ThenByDescending(item => item.Listing.id),
            ListingCatalogSort.PriceAscending => candidates.OrderBy(item => item.EffectivePrice).ThenBy(item => item.Listing.id),
            ListingCatalogSort.PriceDescending => candidates.OrderByDescending(item => item.EffectivePrice).ThenByDescending(item => item.Listing.id),
            ListingCatalogSort.EndingSoon => candidates.OrderBy(item => item.Listing.Auction!.endTime).ThenBy(item => item.Listing.id),
            _ => candidates.OrderByDescending(item => item.Listing.publishedAt ?? item.Listing.createdAt).ThenByDescending(item => item.Listing.id)
        };

        var items = await ordered
            .Skip((int)offset)
            .Take(query.PageSize)
            .Select(item => new ListingSummaryResponse(
                item.Listing.id,
                item.Listing.productId,
                item.Listing.title,
                item.Listing.ListingImages
                    .OrderByDescending(image => image.isPrimary)
                    .ThenBy(image => image.sortOrder)
                    .ThenBy(image => image.id)
                    .Select(image => image.imageUrl)
                    .FirstOrDefault() ?? (item.Listing.product == null ? null : item.Listing.product.ProductImages
                        .OrderByDescending(image => image.isPrimary)
                        .ThenBy(image => image.sortOrder)
                        .ThenBy(image => image.id)
                        .Select(image => image.imageUrl)
                        .FirstOrDefault()),
                item.Listing.product == null ? null : item.Listing.product.brand,
                item.Listing.itemCondition,
                item.Listing.sellerId,
                item.Listing.listingType,
                item.EffectivePrice,
                item.Listing.ShippingPolicy != null && item.Listing.ShippingPolicy.isActive &&
                    item.Listing.ShippingPolicy.ShippingOptions.Any(option => option.isActive && option.flatRate == 0),
                item.Listing.listingType == "AUCTION" ? item.Listing.Auction!.currentPrice : null,
                item.Listing.listingType == "AUCTION" ? item.Listing.Auction!.bidCount : null,
                item.Listing.listingType == "AUCTION" ? item.Listing.Auction!.endTime : null,
                item.Listing.publishedAt))
            .ToListAsync(cancellationToken);

        return new ListingCatalogPage(items, totalItems);
    }

    public Task<ListingDetailResponse?> GetAvailableListingAsync(
        int listingId,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        AvailableListings(utcNow)
            .Where(listing => listing.id == listingId)
            .Select(listing => new ListingDetailResponse(
                listing.id,
                listing.productId,
                listing.title,
                listing.description,
                listing.product == null ? null : listing.product.brand,
                listing.itemCondition,
                new CategorySummaryResponse(listing.category.id, listing.category.name, listing.category.slug),
                listing.ListingImages.Any()
                    ? listing.ListingImages
                        .OrderByDescending(image => image.isPrimary)
                        .ThenBy(image => image.sortOrder)
                        .ThenBy(image => image.id)
                        .Select(image => new ProductImageResponse(image.id, image.imageUrl, image.isPrimary, image.sortOrder))
                        .ToList()
                    : listing.product == null
                        ? new List<ProductImageResponse>()
                        : listing.product.ProductImages
                            .OrderByDescending(image => image.isPrimary)
                            .ThenBy(image => image.sortOrder)
                            .ThenBy(image => image.id)
                            .Select(image => new ProductImageResponse(image.id, image.imageUrl, image.isPrimary, image.sortOrder))
                            .ToList(),
                listing.sellerId,
                listing.seller.username,
                listing.listingType,
                listing.listingType == "AUCTION" ? listing.Auction!.currentPrice : listing.price!.Value,
                listing.listingType == "FIXED_PRICE" ? listing.price : null,
                listing.quantity,
                new ShippingSummaryResponse(
                    null,
                    listing.ShippingPolicy == null || !listing.ShippingPolicy.isActive
                        ? new List<ShippingOptionResponse>()
                        : listing.ShippingPolicy.ShippingOptions
                            .Where(option => option.isActive)
                            .OrderBy(option => option.id)
                            .Select(option => new ShippingOptionResponse(
                                option.id, option.serviceName, option.serviceType, option.flatRate,
                                option.estimatedMinDays, option.estimatedMaxDays))
                            .ToList()),
                listing.listingType == "AUCTION"
                    ? new AuctionDetailResponse(
                        listing.Auction!.id, listing.Auction.startingPrice, listing.Auction.currentPrice,
                        listing.Auction.bidIncrement, listing.Auction.bidCount, listing.Auction.startTime,
                        listing.Auction.endTime, listing.Auction.status)
                    : null,
                listing.publishedAt))
            .SingleOrDefaultAsync(cancellationToken);

    private IQueryable<Listing> AvailableListings(DateTime utcNow) =>
        dbContext.Listings.AsNoTracking().Where(listing =>
            listing.status == "ACTIVE" &&
            listing.quantity > 0 &&
            (listing.publishedAt == null || listing.publishedAt <= utcNow) &&
            (listing.endedAt == null || listing.endedAt > utcNow) &&
            ((listing.listingType == "FIXED_PRICE" && listing.price != null) ||
             (listing.listingType == "AUCTION" && listing.Auction != null &&
              listing.Auction.status == "ACTIVE" &&
              listing.Auction.startTime <= utcNow && listing.Auction.endTime > utcNow)));
}
