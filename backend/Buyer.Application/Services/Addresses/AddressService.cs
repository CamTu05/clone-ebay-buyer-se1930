using System.Globalization;
using Buyer.Domain.Addresses;

namespace Buyer.Application.Services.Addresses;

public sealed class AddressService(IAddressRepository repository) : IAddressService
{
    private static IReadOnlyList<CountryResponse> AllCountries { get; } = CultureInfo
        .GetCultures(CultureTypes.SpecificCultures).Select(c => new RegionInfo(c.Name))
        .Where(r => r.TwoLetterISORegionName.Length == 2)
        .DistinctBy(r => r.TwoLetterISORegionName)
        .Select(r => new CountryResponse(r.TwoLetterISORegionName, r.EnglishName))
        .OrderBy(c => c.Name).ToArray();

    public IReadOnlyList<CountryResponse> Countries => AllCountries;

    public async Task<IReadOnlyList<AddressResponse>> ListAsync(int userId, CancellationToken ct)
        => (await repository.ListAsync(userId, ct)).OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.Id).Select(Map).ToArray();

    public async Task<AddressResponse> GetAsync(int userId, int id, CancellationToken ct)
        => Map(Find((await repository.ListAsync(userId, ct)).ToList(), id));

    public async Task<AddressResponse> CreateAsync(int userId, CreateAddressRequest request, CancellationToken ct)
    {
        var address = new ShippingAddress
        {
            FullName = request.FullName,
            Phone = request.Phone,
            Street = request.Street,
            City = request.City,
            CountryCode = request.CountryCode,
            AddressLine2 = request.AddressLine2,
            State = request.State,
            PostalCode = request.PostalCode
        };
        Validate(address);
        var saved = await repository.MutateAsync(userId, book =>
        {
            book.Add(address);
            if (request.IsDefault || !book.Any(a => a.IsDefault)) MakeDefault(book, address);
            return address;
        }, ct);
        return Map(saved);
    }

    public async Task<AddressResponse> UpdateAsync(int userId, int id, UpdateAddressRequest patch, CancellationToken ct)
    {
        var saved = await repository.MutateAsync(userId, book =>
        {
            var address = Find(book, id);
            address.FullName = patch.FullName ?? address.FullName;
            address.Phone = patch.Phone ?? address.Phone;
            address.Street = patch.Street ?? address.Street;
            address.City = patch.City ?? address.City;
            address.CountryCode = patch.CountryCode ?? address.CountryCode;
            address.AddressLine2 = patch.AddressLine2 ?? address.AddressLine2;
            address.State = patch.State ?? address.State;
            address.PostalCode = patch.PostalCode ?? address.PostalCode;
            Validate(address, patch.CountryCode is not null);
            if (patch.IsDefault == false && address.IsDefault)
                throw new AddressException("ADDRESS_DEFAULT_REQUIRED", "Choose another default address first.", AddressFailure.Conflict, "isDefault");
            if (patch.IsDefault == true) MakeDefault(book, address);
            return address;
        }, ct);
        return Map(saved);
    }

    public async Task<AddressResponse> SetDefaultAsync(int userId, int id, CancellationToken ct)
        => Map(await repository.MutateAsync(userId, book =>
        {
            var address = Find(book, id);
            MakeDefault(book, address);
            return address;
        }, ct));

    public async Task DeleteAsync(int userId, int id, CancellationToken ct)
    {
        await repository.MutateAsync(userId, book =>
        {
            var address = Find(book, id);
            book.Remove(address);
            if (address.IsDefault && book.Count > 0) MakeDefault(book, book.MinBy(a => a.Id)!);
            return true;
        }, ct);
    }

    // Checkout must resolve the owned address again at order creation and snapshot these fields.
    public Task<AddressResponse> SelectForCheckoutAsync(int userId, int id, CancellationToken ct)
        => GetAsync(userId, id, ct);

    private static ShippingAddress Find(IList<ShippingAddress> book, int id)
        => book.FirstOrDefault(a => a.Id == id)
            ?? throw new AddressException("ADDRESS_NOT_FOUND", "Address not found.", AddressFailure.NotFound);

    private static void MakeDefault(IList<ShippingAddress> book, ShippingAddress chosen)
    {
        foreach (var address in book) address.IsDefault = ReferenceEquals(address, chosen);
    }

    private static string Required(string? value, int max, string field)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text) || text.Length > max || text.Any(char.IsControl))
            throw new AddressException("ADDRESS_INVALID", $"{field} is required, must be at most {max} characters and contain no control characters.", AddressFailure.Invalid, field);
        return text;
    }

    private static string? Optional(string? value, int max, string field)
        => string.IsNullOrWhiteSpace(value) ? null : Required(value, max, field);

    private static void Validate(ShippingAddress a, bool validateCountry = true)
    {
        a.FullName = Required(a.FullName, 150, "fullName");
        a.Phone = Required(a.Phone, 30, "phone");
        if (!a.Phone.Any(char.IsDigit))
            throw new AddressException("ADDRESS_INVALID", "Phone must contain a telephone number.", AddressFailure.Invalid, "phone");
        a.Street = Required(a.Street, 255, "street");
        a.City = Required(a.City, 100, "city");
        a.AddressLine2 = Optional(a.AddressLine2, 255, "addressLine2");
        a.State = Optional(a.State, 100, "state");
        a.PostalCode = Optional(a.PostalCode, 30, "postalCode");
        // Existing SQL rows may have no countryCode; unrelated edits must preserve their country.
        if (validateCountry || !string.IsNullOrWhiteSpace(a.CountryCode))
        {
            var code = Required(a.CountryCode, 2, "countryCode").ToUpperInvariant();
            var country = AllCountries.FirstOrDefault(c => c.Code == code)
                ?? throw new AddressException("ADDRESS_INVALID", "Select a valid country.", AddressFailure.Invalid, "countryCode");
            a.CountryCode = country.Code;
            a.Country = country.Name;
        }
    }

    private static AddressResponse Map(ShippingAddress a) => new(a.Id, a.FullName, a.Phone,
        a.Street, a.AddressLine2, a.City, a.State, a.PostalCode, a.Country, a.CountryCode, a.IsDefault);
}
