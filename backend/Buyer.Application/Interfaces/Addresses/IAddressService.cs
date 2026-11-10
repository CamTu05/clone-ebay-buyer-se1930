namespace Buyer.Application.Interfaces.Addresses;

public interface IAddressService
{
    IReadOnlyList<CountryResponse> Countries { get; }
    Task<IReadOnlyList<AddressResponse>> ListAsync(int userId, CancellationToken cancellationToken);
    Task<AddressResponse> GetAsync(int userId, int id, CancellationToken cancellationToken);
    Task<AddressResponse> CreateAsync(int userId, CreateAddressRequest request, CancellationToken cancellationToken);
    Task<AddressResponse> UpdateAsync(int userId, int id, UpdateAddressRequest request, CancellationToken cancellationToken);
    Task<AddressResponse> SetDefaultAsync(int userId, int id, CancellationToken cancellationToken);
    Task DeleteAsync(int userId, int id, CancellationToken cancellationToken);
    Task<AddressResponse> SelectForCheckoutAsync(int userId, int id, CancellationToken cancellationToken);
}
