namespace Buyer.Application.DTOs.Cart;

public sealed record CartResponse(
    int? CartId,
    IReadOnlyList<CartItemResponse> Items,
    decimal Subtotal,
    string Currency,
    bool CanCheckout);
