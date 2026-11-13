using Buyer.Application.Catalog;
using Buyer.Infrastructure.Data;
using Buyer.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Buyer.Infrastructure.Catalog;

public sealed class ProductCatalogRepository(CloneEbayDbContext dbContext) : IProductCatalogRepository
{
    public Task<ProductCatalogPage> GetAvailableProductsAsync(
        int page,
        int pageSize,
        DateTime utcNow,
        CancellationToken cancellationToken) => SearchAsync(
            new ProductListRequest { Page = page, PageSize = pageSize }, utcNow, cancellationToken);

    public async Task<ProductCatalogPage> SearchAsync(
        ProductListRequest request,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var listings = dbContext.Listings.AsNoTracking().Where(listing =>
            listing.status == "ACTIVE" &&
            listing.seller.accountStatus == "ACTIVE" &&
            listing.quantity > 0 &&
            (listing.publishedAt == null || listing.publishedAt <= utcNow) &&
            (listing.endedAt == null || listing.endedAt > utcNow) &&
            ((listing.listingType == "FIXED_PRICE" && listing.price != null) ||
             (listing.listingType == "AUCTION" && listing.Auction != null &&
              listing.Auction.status == "ACTIVE" &&
               listing.Auction.startTime <= utcNow && listing.Auction.endTime > utcNow)));

        listings = ApplyKeywordMatch(listings, request.Q, request.Match);
        if (!string.IsNullOrWhiteSpace(request.Category)) listings = listings.Where(listing => listing.category.name == request.Category || listing.category.slug == request.Category);
        if (!string.IsNullOrWhiteSpace(request.Condition))
        {
            var conditions = request.Condition.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var includeNew = conditions.Contains("New");
            var includeUsed = conditions.Contains("Used");
            var includeNotSpecified = conditions.Contains("Not specified");
            listings = listings.Where(listing =>
                includeNew && listing.itemCondition == "New" ||
                includeUsed && listing.itemCondition != null && listing.itemCondition.StartsWith("Used") ||
                includeNotSpecified && (listing.itemCondition == null || listing.itemCondition == ""));
        }
        if (request.Type is not null) listings = listings.Where(listing => listing.listingType == request.Type);
        if (request.MinBids > request.MaxBids) throw new InvalidOperationException("Minimum bid count exceeds maximum bid count.");
        if (request.MinQuantity > request.MaxQuantity) throw new InvalidOperationException("Minimum quantity exceeds maximum quantity.");
        if (request.EndingWithinMinutes is int minutes) { var end = utcNow.AddMinutes(minutes); listings = listings.Where(x => x.listingType == "AUCTION" && x.Auction != null && x.Auction.endTime <= end); }
        if (request.MinBids is int minBids) listings = listings.Where(x => x.Auction != null && x.Auction.bidCount >= minBids);
        if (request.MaxBids is int maxBids) listings = listings.Where(x => x.Auction != null && x.Auction.bidCount <= maxBids);
        if (request.MinQuantity is int minQuantity) listings = listings.Where(x => x.quantity >= minQuantity);
        if (request.MaxQuantity is int maxQuantity) listings = listings.Where(x => x.quantity <= maxQuantity);
        if (request.FreeShipping == true) listings = listings.Where(x => x.ShippingPolicy != null && x.ShippingPolicy.ShippingOptions.Any(o => o.isActive && o.flatRate == 0));
        if (request.LocalPickup == true) listings = listings.Where(x => x.ShippingPolicy != null && x.ShippingPolicy.ShippingOptions.Any(o => o.isActive && o.serviceType == "LOCAL_PICKUP"));
        if (request.ReturnsAccepted == true) listings = listings.Where(x => x.ReturnPolicy != null && x.ReturnPolicy.isActive && x.ReturnPolicy.returnsAccepted);
        if (request.FreeReturns == true) listings = listings.Where(x => x.ReturnPolicy != null && x.ReturnPolicy.isActive && x.ReturnPolicy.freeReturns);
        if (request.DealsOnly == true) listings = listings.Where(x => x.ListingPromotion != null && x.ListingPromotion.promotionType == "DEAL" && x.ListingPromotion.startsAt <= utcNow && x.ListingPromotion.endsAt > utcNow);
        if (request.SaleItems == true) listings = listings.Where(x => x.ListingPromotion != null && x.ListingPromotion.promotionType == "SALE" && x.ListingPromotion.startsAt <= utcNow && x.ListingPromotion.endsAt > utcNow);
        if (!string.IsNullOrWhiteSpace(request.ItemCountry)) { var country = request.ItemCountry.ToUpper(); listings = listings.Where(x => x.itemCountryCode == country); }
        if (!string.IsNullOrWhiteSpace(request.AvailableTo)) { var country = request.AvailableTo.ToUpper(); listings = listings.Where(x => x.ShippingPolicy != null && x.ShippingPolicy.ShippingPolicyDestinationCountries.Any(d => d.countryCode == country)); }
        if (!string.IsNullOrWhiteSpace(request.Sellers)) { var names = request.Sellers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(x => x.ToUpper()).Distinct().ToArray(); listings = request.SellerMode == "exclude" ? listings.Where(x => !names.Contains(x.seller.username.ToUpper())) : listings.Where(x => names.Contains(x.seller.username.ToUpper())); }
        if (request.Min is not null) listings = listings.Where(x => (x.listingType == "AUCTION" ? x.Auction!.currentPrice : x.ListingPromotion != null && x.ListingPromotion.startsAt <= utcNow && x.ListingPromotion.endsAt > utcNow ? x.ListingPromotion.salePrice : x.price) >= request.Min);
        if (request.Max is not null) listings = listings.Where(x => (x.listingType == "AUCTION" ? x.Auction!.currentPrice : x.ListingPromotion != null && x.ListingPromotion.startsAt <= utcNow && x.ListingPromotion.endsAt > utcNow ? x.ListingPromotion.salePrice : x.price) <= request.Max);

        var products = dbContext.Products.AsNoTracking()
            .Where(product => product.category.isActive)
            .Where(product => listings.Any(listing => listing.productId == product.id));

        var totalItems = await products.CountAsync(cancellationToken);
        var offset = ((long)request.Page - 1) * request.PageSize;
        if (offset >= totalItems)
        {
            return new ProductCatalogPage([], totalItems);
        }

        var ordered = request.Sort switch
        {
            "name" => products.OrderBy(product => product.title).ThenBy(product => product.id),
            _ => products.OrderBy(product => product.id)
        };
        var items = await ordered.Skip((int)offset)
            .Take(request.PageSize)
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
