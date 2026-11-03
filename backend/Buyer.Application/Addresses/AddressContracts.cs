namespace Buyer.Application.Addresses;

public sealed record AddressResponse(int Id, string FullName, string Phone, string Street,
    string? AddressLine2, string City, string? State, string? PostalCode, string Country,
    string? CountryCode, bool IsDefault);

public sealed record CreateAddressRequest(string FullName, string Phone, string Street,
    string City, string CountryCode, string? AddressLine2 = null, string? State = null,
    string? PostalCode = null, bool IsDefault = false);

// Empty string clears an optional field; omitted/null fields are left unchanged.
public sealed record UpdateAddressRequest(string? FullName = null, string? Phone = null,
    string? Street = null, string? City = null, string? CountryCode = null,
    string? AddressLine2 = null, string? State = null, string? PostalCode = null, bool? IsDefault = null);

public enum AddressFailure { Invalid, NotFound, Unauthorized, Forbidden, Conflict, Unavailable }

public sealed class AddressException(string code, string message, AddressFailure failure,
    string? field = null) : Exception(message)
{
    public string Code { get; } = code;
    public AddressFailure Failure { get; } = failure;
    public string? Field { get; } = field;
}

public sealed record CountryResponse(string Code, string Name);
