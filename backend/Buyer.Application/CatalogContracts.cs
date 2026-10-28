using System.ComponentModel.DataAnnotations;

namespace Buyer.Application.Catalog;

public sealed class ProductListRequest
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed record ProductSummaryResponse(
    int ProductId,
    int ListingId,
    string Name,
    decimal Price,
    string? ImageUrl,
    string ListingType);

public sealed record ProductPageResponse(
    IReadOnlyList<ProductSummaryResponse> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);
