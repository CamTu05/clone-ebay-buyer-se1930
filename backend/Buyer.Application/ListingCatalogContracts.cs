using System.ComponentModel.DataAnnotations;

namespace Buyer.Application.Catalog;

public sealed class ProductListRequest
{
    [StringLength(200)] public string? Q { get; init; }
    [RegularExpression("^(all|any|exact|exact-any-order)$")] public string? Match { get; init; }
    [StringLength(50)] public string? Condition { get; init; }
    [StringLength(150)] public string? Category { get; init; }
    [RegularExpression("^(AUCTION|FIXED_PRICE)$")] public string? Type { get; init; }
    [Range(0, 1000000000000d)] public decimal? Min { get; init; }
    [Range(0, 1000000000000d)] public decimal? Max { get; init; }
    public bool? ReturnsAccepted { get; init; }
    public bool? FreeReturns { get; init; }
    public bool? DealsOnly { get; init; }
    public bool? SaleItems { get; init; }
    [Range(1, 1440)] public int? EndingWithinMinutes { get; init; }
    [Range(0, int.MaxValue)] public int? MinBids { get; init; }
    [Range(0, int.MaxValue)] public int? MaxBids { get; init; }
    [Range(1, int.MaxValue)] public int? MinQuantity { get; init; }
    [Range(1, int.MaxValue)] public int? MaxQuantity { get; init; }
    public bool? FreeShipping { get; init; }
    public bool? LocalPickup { get; init; }
    [RegularExpression("^[A-Za-z]{2}$")] public string? ItemCountry { get; init; }
    [RegularExpression("^[A-Za-z]{2}$")] public string? AvailableTo { get; init; }
    [StringLength(1000)] public string? Sellers { get; init; }
    [RegularExpression("^(include|exclude)$")] public string? SellerMode { get; init; }
    [RegularExpression("^(default|newest|ending-soon|price-low|price-high|name|bids-low|bids-high|quantity-low|quantity-high)$")] public string? Sort { get; init; }
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
