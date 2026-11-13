namespace Buyer.Infrastructure.Data.Models;

public partial class ShippingPolicyDestinationCountry
{
    public int shippingPolicyId { get; set; }
    public string countryCode { get; set; } = null!;
    public virtual ShippingPolicy shippingPolicy { get; set; } = null!;
}
