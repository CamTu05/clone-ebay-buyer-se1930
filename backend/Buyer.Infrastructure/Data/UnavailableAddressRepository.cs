using Buyer.Application.Addresses;
using Buyer.Domain.Addresses;

namespace Buyer.Infrastructure.Data;

public sealed class UnavailableAddressRepository : IAddressRepository
{
    private static AddressException Unavailable() => new("ADDRESS_UNAVAILABLE",
        "The address book is temporarily unavailable.", AddressFailure.Unavailable);
    public Task<IReadOnlyList<ShippingAddress>> ListAsync(int userId, CancellationToken cancellationToken)
        => throw Unavailable();
    public Task<T> MutateAsync<T>(int userId, Func<IList<ShippingAddress>, T> change, CancellationToken cancellationToken)
        => throw Unavailable();
}
