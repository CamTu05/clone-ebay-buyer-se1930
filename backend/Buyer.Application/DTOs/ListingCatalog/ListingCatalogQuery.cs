namespace Buyer.Application.DTOs.ListingCatalog;

public sealed record ListingCatalogQuery(
    string? Search,
    string? SearchPattern,
    int? CategoryId,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? Condition,
    string? Brand,
    string? ListingType,
    int? SellerId,
    bool? FreeShipping,
    bool? AllowBestOffer,
    int? MinBidCount,
    int? MaxBidCount,
    ListingCatalogSort Sort,
    int Page,
    int PageSize);
