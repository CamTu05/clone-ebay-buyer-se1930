namespace Buyer.Application.DTOs.Profile;

public sealed record BuyerFeedbackSellerResponse(
    int Id,
    string Username,
    string? AvatarUrl);

public sealed record BuyerFeedbackPurchaseItemResponse(
    long OrderItemId,
    int ListingId,
    string Title,
    string? ImageUrl,
    decimal UnitPrice);

public sealed record BuyerFeedbackItemResponse(
    long Id,
    long OrderId,
    BuyerFeedbackSellerResponse Seller,
    IReadOnlyList<BuyerFeedbackPurchaseItemResponse> PurchaseItems,
    string Currency,
    bool VerifiedPurchase,
    string Comment,
    DateTime CreatedAt);

public sealed record BuyerFeedbackPageResponse(
    int PositiveFeedbackCount,
    IReadOnlyList<BuyerFeedbackItemResponse> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);
