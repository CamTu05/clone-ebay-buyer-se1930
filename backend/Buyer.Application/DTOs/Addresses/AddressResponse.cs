namespace Buyer.Application.DTOs.Addresses;

public sealed record AddressResponse(int Id, string FullName, string Phone, string Street,
    string? AddressLine2, string City, string? State, string? PostalCode, string Country,
    string? CountryCode, bool IsDefault);
