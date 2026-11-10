using System.ComponentModel.DataAnnotations;

namespace Buyer.Application.Catalog;

public sealed class ProductListRequest
{
    [StringLength(200)] public string? Q { get; init; }
    [StringLength(50)] public string? Condition { get; init; }
    [StringLength(150)] public string? Category { get; init; }
    [RegularExpression("^(AUCTION|FIXED_PRICE)$")] public string? Type { get; init; }
    [Range(0, 1000000000000d)] public decimal? Min { get; init; }
    [Range(0, 1000000000000d)] public decimal? Max { get; init; }
    [RegularExpression("^(default|price-low|price-high|name)$")] public string? Sort { get; init; }
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
