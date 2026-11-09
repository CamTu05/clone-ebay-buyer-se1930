namespace Buyer.Application.DTOs.Addresses;

public sealed record CreateAddressRequest(string FullName, string Phone, string Street,
    string City, string CountryCode, string? AddressLine2 = null, string? State = null,
    string? PostalCode = null, bool IsDefault = false);

// Empty string clears an optional field; omitted/null fields are left unchanged.
