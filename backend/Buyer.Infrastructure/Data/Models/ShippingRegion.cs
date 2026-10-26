using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class ShippingRegion
{
    public int id { get; set; }

    public string code { get; set; } = null!;

    public string name { get; set; } = null!;

    public string regionType { get; set; } = null!;

    public decimal surcharge { get; set; }

    public bool isActive { get; set; }
}
