namespace Buyer.Application.Interfaces.ListingCatalog;

public interface IListingCatalogService
{
    Task<ListingPageResponse> GetListingsAsync(
        ListingListRequest request,
        CancellationToken cancellationToken = default);

    Task<ListingDetailResponse?> GetListingAsync(
        int listingId,
        CancellationToken cancellationToken = default);
}
