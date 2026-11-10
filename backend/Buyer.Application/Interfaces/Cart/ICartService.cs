namespace Buyer.Application.Interfaces.Cart;

public interface ICartService
{
    Task<CartResponse> GetAsync(int userId, CancellationToken cancellationToken = default);
    Task<CartResult> UpdateQuantityAsync(int userId, int cartItemId, int quantity,
        CancellationToken cancellationToken = default);
    Task<CartResult> RemoveAsync(int userId, int cartItemId,
        CancellationToken cancellationToken = default);
}
