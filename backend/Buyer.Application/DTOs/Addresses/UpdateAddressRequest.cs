namespace Buyer.Application.DTOs.Addresses;

public sealed record UpdateAddressRequest(string? FullName = null, string? Phone = null,
    string? Street = null, string? City = null, string? CountryCode = null,
    string? AddressLine2 = null, string? State = null, string? PostalCode = null, bool? IsDefault = null);
