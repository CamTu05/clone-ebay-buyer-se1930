namespace Buyer.Application.DTOs.Cart;

public sealed record CartItemResponse(
    int CartItemId,
    int ListingId,
    int? ProductId,
    string Title,
    string? ImageUrl,
    CartSellerResponse Seller,
    decimal UnitPrice,
    int Quantity,
    decimal ItemTotal,
    int AvailableQuantity,
    bool IsAvailable,
    string? UnavailableReason);
