using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class CouponUsage
{
    public long id { get; set; }

    public int couponId { get; set; }

    public int? userId { get; set; }

    public long orderId { get; set; }

    public decimal discountAmount { get; set; }

    public DateTime usedAt { get; set; }

    public virtual Coupon coupon { get; set; } = null!;

    public virtual OrderTable order { get; set; } = null!;

    public virtual User? user { get; set; }
}
