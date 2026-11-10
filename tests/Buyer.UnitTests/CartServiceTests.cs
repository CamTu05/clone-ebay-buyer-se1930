using Buyer.Application.Features.Cart;

namespace Buyer.UnitTests;

public sealed class CartServiceTests
{
    private static readonly CartResponse Cart = new(1,
        [new(5, 11, 7, "Camera", null, new(2, "seller", null),
            100m, 2, 200m, 5, true, null)], 200m, "VND", true);

    [Fact]
    public async Task GetReturnsRepositoryCart()
    {
        var repository = new FakeCartRepository { Cart = Cart };
        var result = await new CartService(repository).GetAsync(42);
        Assert.Equal(42, repository.LastUserId);
        Assert.Equal(200m, result.Subtotal);
    }

    [Fact]
    public async Task GetSupportsEmptyCart()
    {
        var empty = new CartResponse(null, [], 0m, "VND", false);
        var result = await new CartService(new FakeCartRepository { Cart = empty }).GetAsync(42);
        Assert.Empty(result.Items);
        Assert.Equal(0m, result.Subtotal);
        Assert.False(result.CanCheckout);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateRejectsNonPositiveQuantityWithoutWriting(int quantity)
    {
        var repository = new FakeCartRepository { Cart = Cart };
        var result = await new CartService(repository).UpdateQuantityAsync(42, 5, quantity);
        Assert.Equal(CartFailure.InvalidQuantity, result.Failure);
        Assert.Null(repository.LastQuantity);
    }

    [Fact]
    public async Task UpdatePassesAuthenticatedUserAndQuantity()
    {
        var repository = new FakeCartRepository { Cart = Cart };
        var result = await new CartService(repository).UpdateQuantityAsync(42, 5, 3);
        Assert.True(result.IsSuccess);
        Assert.Equal(42, repository.LastUserId);
        Assert.Equal(5, repository.LastCartItemId);
        Assert.Equal(3, repository.LastQuantity);
    }

    [Fact]
    public async Task StockFailureDoesNotReturnChangedCart()
    {
        var repository = new FakeCartRepository
        {
            Cart = Cart,
            UpdateFailure = CartFailure.QuantityExceedsStock
        };
        var result = await new CartService(repository).UpdateQuantityAsync(42, 5, 99);
        Assert.False(result.IsSuccess);
        Assert.Equal(CartFailure.QuantityExceedsStock, result.Failure);
    }

    [Fact]
    public async Task RemovePassesAuthenticatedUserAndItemId()
    {
        var repository = new FakeCartRepository { Cart = Cart };
        var result = await new CartService(repository).RemoveAsync(42, 5);
        Assert.True(result.IsSuccess);
        Assert.Equal(42, repository.LastUserId);
        Assert.Equal(5, repository.LastCartItemId);
    }

    private sealed class FakeCartRepository : ICartRepository
    {
        public required CartResponse Cart { get; init; }
        public CartFailure UpdateFailure { get; init; }
        public int LastUserId { get; private set; }
        public int LastCartItemId { get; private set; }
        public int? LastQuantity { get; private set; }

        public Task<CartResponse> GetAsync(int userId, CancellationToken cancellationToken = default)
        {
            LastUserId = userId;
            return Task.FromResult(Cart);
        }

        public Task<CartResult> UpdateQuantityAsync(int userId, int cartItemId, int quantity,
            CancellationToken cancellationToken = default)
        {
            LastUserId = userId;
            LastCartItemId = cartItemId;
            LastQuantity = quantity;
            return Task.FromResult(UpdateFailure == CartFailure.None
                ? CartResult.Success(Cart)
                : CartResult.Failed(UpdateFailure));
        }

        public Task<CartResult> RemoveAsync(int userId, int cartItemId,
            CancellationToken cancellationToken = default)
        {
            LastUserId = userId;
            LastCartItemId = cartItemId;
            return Task.FromResult(CartResult.Success(Cart));
        }
    }
}
