using System.ComponentModel.DataAnnotations;

namespace Buyer.Application.DTOs.ListingCatalog;

public sealed record ListingPageResponse(
    IReadOnlyList<ListingSummaryResponse> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);
