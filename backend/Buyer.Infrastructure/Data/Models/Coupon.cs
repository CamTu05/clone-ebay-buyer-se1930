using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class Coupon
{
    public int id { get; set; }

    public int? sellerId { get; set; }

    public string code { get; set; } = null!;

    public string discountType { get; set; } = null!;

    public decimal discountValue { get; set; }

    public decimal? minOrderValue { get; set; }

    public decimal? maxDiscountAmount { get; set; }

    public DateTime startDate { get; set; }

    public DateTime endDate { get; set; }

    public int? maxUsage { get; set; }

    public int usageCount { get; set; }

    public int? perUserLimit { get; set; }

    public bool isActive { get; set; }

    public virtual ICollection<CouponUsage> CouponUsages { get; set; } = new List<CouponUsage>();

    public virtual ICollection<OrderTable> OrderTables { get; set; } = new List<OrderTable>();

    public virtual User? seller { get; set; }

    public virtual ICollection<Listing> listings { get; set; } = new List<Listing>();
}
