namespace Buyer.Domain.Addresses;

// Independent from the reverse-engineered EF model; represents one saved shipping destination.
public sealed class ShippingAddress
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Street { get; set; } = "";
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = "";
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; } = "";
    public string? CountryCode { get; set; }
    public bool IsDefault { get; set; }
}
