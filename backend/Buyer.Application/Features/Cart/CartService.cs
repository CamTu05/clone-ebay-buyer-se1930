namespace Buyer.Application.Features.Cart;

public sealed class CartService(ICartRepository repository) : ICartService
{
    public Task<CartResponse> GetAsync(int userId, CancellationToken cancellationToken = default) =>
        repository.GetAsync(userId, cancellationToken);

    public Task<CartResult> UpdateQuantityAsync(int userId, int cartItemId, int quantity,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || cartItemId <= 0)
            return Task.FromResult(CartResult.Failed(CartFailure.ItemNotFound));
        if (quantity <= 0)
            return Task.FromResult(CartResult.Failed(CartFailure.InvalidQuantity));

        return repository.UpdateQuantityAsync(userId, cartItemId, quantity, cancellationToken);
    }

    public Task<CartResult> RemoveAsync(int userId, int cartItemId,
        CancellationToken cancellationToken = default) =>
        userId <= 0 || cartItemId <= 0
            ? Task.FromResult(CartResult.Failed(CartFailure.ItemNotFound))
            : repository.RemoveAsync(userId, cartItemId, cancellationToken);
}
