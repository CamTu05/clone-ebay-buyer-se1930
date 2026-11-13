using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.BuyerSchema.Models;

public partial class ShippingPolicy
{
    public int id { get; set; }

    public int sellerId { get; set; }

    public string name { get; set; } = null!;

    public bool isActive { get; set; }

    public DateTime createdAt { get; set; }

    public virtual ICollection<Listing> Listings { get; set; } = new List<Listing>();

    public virtual ICollection<ShippingOption> ShippingOptions { get; set; } = new List<ShippingOption>();

    public virtual ICollection<ShippingPolicyDestinationCountry> ShippingPolicyDestinationCountries { get; set; } = new List<ShippingPolicyDestinationCountry>();

    public virtual User seller { get; set; } = null!;
}
