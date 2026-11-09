using Buyer.Domain.Addresses;

namespace Buyer.Application.Interfaces.Addresses;

public interface IAddressRepository
{
    Task<IReadOnlyList<ShippingAddress>> ListAsync(int userId, CancellationToken cancellationToken);

    // Lock this user's address book and commit all changes atomically. Newly saved IDs are
    // assigned to the same objects before returning. Exceptions roll back the entire operation.
    Task<T> MutateAsync<T>(int userId, Func<IList<ShippingAddress>, T> change,
        CancellationToken cancellationToken);
}
