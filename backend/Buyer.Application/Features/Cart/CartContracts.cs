namespace Buyer.Application.Features.Cart;

public sealed record CartSellerResponse(int Id, string Username, string? StoreName);

public sealed record CartItemResponse(
    int CartItemId,
    int ListingId,
    int ProductId,
    string Title,
    string? ImageUrl,
    CartSellerResponse Seller,
    decimal UnitPrice,
    int Quantity,
    decimal ItemTotal,
    int AvailableQuantity,
    bool IsAvailable,
    string? UnavailableReason);

public sealed record CartResponse(
    int? CartId,
    IReadOnlyList<CartItemResponse> Items,
    decimal Subtotal,
    string Currency,
    bool CanCheckout);

public sealed record UpdateCartItemQuantityRequest(int Quantity);

public enum CartFailure
{
    None,
    InvalidQuantity,
    ItemNotFound,
    ListingUnavailable,
    QuantityExceedsStock
}

public sealed record CartResult(CartResponse? Value, CartFailure Failure)
{
    public bool IsSuccess => Value is not null;
    public static CartResult Success(CartResponse value) => new(value, CartFailure.None);
    public static CartResult Failed(CartFailure failure) => new(null, failure);
}

public interface ICartRepository
{
    Task<CartResponse> GetAsync(int userId, CancellationToken cancellationToken = default);
    Task<CartResult> UpdateQuantityAsync(int userId, int cartItemId, int quantity,
        CancellationToken cancellationToken = default);
    Task<CartResult> RemoveAsync(int userId, int cartItemId,
        CancellationToken cancellationToken = default);
}

public interface ICartService
{
    Task<CartResponse> GetAsync(int userId, CancellationToken cancellationToken = default);
    Task<CartResult> UpdateQuantityAsync(int userId, int cartItemId, int quantity,
        CancellationToken cancellationToken = default);
    Task<CartResult> RemoveAsync(int userId, int cartItemId,
        CancellationToken cancellationToken = default);
}
