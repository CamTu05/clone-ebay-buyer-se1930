namespace Buyer.Application.Catalog;

public sealed record ProductImageResponse(int Id, string Url, bool IsPrimary);
public sealed record SellerResponse(int Id, string Username, string? AvatarUrl, string? StoreName);
public sealed record ProductListingResponse(int ListingId, string ListingType, decimal? Price, SellerResponse Seller);
public sealed record ProductDetailResponse(int ProductId, string Name, string? Description,
    string? Brand, string? Condition, IReadOnlyList<ProductImageResponse> Images,
    IReadOnlyList<ProductListingResponse> Listings);
public sealed record ProductSummaryResponse(int ProductId, string Name, string? ImageUrl);
public sealed record ProductPageResponse(IReadOnlyList<ProductSummaryResponse> Items, int Page,
    int PageSize, int TotalItems, int TotalPages);
