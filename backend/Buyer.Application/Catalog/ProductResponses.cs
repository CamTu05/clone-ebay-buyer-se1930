namespace Buyer.Application.Catalog;

public sealed record ProductImageResponse(int Id, string Url, bool IsPrimary);
public sealed record SellerResponse(int Id, string Username, string? AvatarUrl, string? StoreName);
public sealed record ProductListingResponse(int ListingId, string ListingType, decimal? Price, SellerResponse Seller, int? AuctionId = null, string? Condition = null);
public sealed record UrgencyBadgeResponse(
    bool Show,
    string? Text,
    int InCartCount,
    int SoldCount,
    string? BadgeType);
[method: System.Text.Json.Serialization.JsonConstructor]
public sealed record ProductDetailResponse(int ProductId, string Name, string? Description,
    string? Brand, string? Condition, IReadOnlyList<ProductImageResponse> Images,
    IReadOnlyList<ProductListingResponse> Listings,
    UrgencyBadgeResponse? UrgencyBadge = null)
{
    public ProductDetailResponse(int productId, string name, string? description,
        string? brand, string? condition, IReadOnlyList<ProductImageResponse> images,
        IReadOnlyList<ProductListingResponse> listings)
        : this(productId, name, description, brand, condition, images, listings, null)
    {
    }
}
