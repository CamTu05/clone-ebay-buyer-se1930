using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class ShippingOption
{
    public int id { get; set; }

    public int shippingPolicyId { get; set; }

    public string serviceName { get; set; } = null!;

    public string serviceType { get; set; } = null!;

    public decimal? flatRate { get; set; }

    public int? estimatedMinDays { get; set; }

    public int? estimatedMaxDays { get; set; }

    public bool isActive { get; set; }

    public virtual ICollection<OrderTable> OrderTables { get; set; } = new List<OrderTable>();

    public virtual ShippingPolicy shippingPolicy { get; set; } = null!;
}
