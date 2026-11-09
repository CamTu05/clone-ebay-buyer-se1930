namespace Buyer.Application.DTOs.ListingCatalog;

public sealed record ListingCatalogPage(IReadOnlyList<ListingSummaryResponse> Items, int TotalItems);
