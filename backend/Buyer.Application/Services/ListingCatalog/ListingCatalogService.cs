using Buyer.Application.Interfaces.Authentication;

namespace Buyer.Application.Services.ListingCatalog;

public sealed class ListingCatalogService(IListingCatalogRepository repository, IClock clock)
    : IListingCatalogService
{
    public async Task<ListingPageResponse> GetListingsAsync(
        ListingListRequest request,
        CancellationToken cancellationToken = default)
    {
        var search = Normalize(request.Search);
        var sort = ParseSort(Normalize(request.Sort), search is not null);
        var query = new ListingCatalogQuery(
            search,
            search is null ? null : $"%{EscapeLikePattern(search)}%",
            request.CategoryId,
            request.MinPrice,
            request.MaxPrice,
            Normalize(request.Condition),
            Normalize(request.Brand),
            Normalize(request.ListingType)?.ToUpperInvariant(),
            request.SellerId,
            request.FreeShipping,
            request.MinBidCount,
            request.MaxBidCount,
            sort,
            request.Page,
            request.PageSize);

        var result = await repository.GetAvailableListingsAsync(query, clock.UtcNow, cancellationToken);

        return new ListingPageResponse(
            result.Items,
            request.Page,
            request.PageSize,
            result.TotalItems,
            (int)Math.Ceiling((double)result.TotalItems / request.PageSize));
    }

    public Task<ListingDetailResponse?> GetListingAsync(
        int listingId,
        CancellationToken cancellationToken = default) =>
        repository.GetAvailableListingAsync(listingId, clock.UtcNow, cancellationToken);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ListingCatalogSort ParseSort(string? sort, bool hasSearch) => sort?.ToLowerInvariant() switch
    {
        "relevance" => ListingCatalogSort.Relevance,
        "price_asc" => ListingCatalogSort.PriceAscending,
        "price_desc" => ListingCatalogSort.PriceDescending,
        "ending_soon" => ListingCatalogSort.EndingSoon,
        "newest" => ListingCatalogSort.Newest,
        _ => hasSearch ? ListingCatalogSort.Relevance : ListingCatalogSort.Newest
    };

    internal static string EscapeLikePattern(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal)
        .Replace("[", "\\[", StringComparison.Ordinal);
}
