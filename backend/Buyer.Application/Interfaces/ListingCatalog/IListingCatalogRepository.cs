namespace Buyer.Application.Interfaces.ListingCatalog;

public interface IListingCatalogRepository
{
    Task<ListingCatalogPage> GetAvailableListingsAsync(
        ListingCatalogQuery query,
        DateTime utcNow,
        CancellationToken cancellationToken);

    Task<ListingDetailResponse?> GetAvailableListingAsync(
        int listingId,
        DateTime utcNow,
        CancellationToken cancellationToken);
}
