using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class TaxRegion
{
    public int id { get; set; }

    public string code { get; set; } = null!;

    public string name { get; set; } = null!;

    public decimal taxRate { get; set; }

    public bool taxShipping { get; set; }

    public bool isActive { get; set; }
}
