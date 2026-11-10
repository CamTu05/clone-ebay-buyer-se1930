using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.BuyerSchema.Models;

public partial class Coupon
{
    public int id { get; set; }

    public string code { get; set; } = null!;

    public string discountType { get; set; } = null!;

    public decimal discountValue { get; set; }

    public decimal? minOrderValue { get; set; }

    public decimal? maxDiscountAmount { get; set; }

    public DateTime startDate { get; set; }

    public DateTime endDate { get; set; }

    public int? maxUsage { get; set; }

    public int usageCount { get; set; }

    public bool isActive { get; set; }

    public virtual ICollection<CheckoutGroup> CheckoutGroups { get; set; } = new List<CheckoutGroup>();

    public virtual ICollection<CouponUsage> CouponUsages { get; set; } = new List<CouponUsage>();
}
