using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.BuyerSchema.Models;

public partial class CheckoutGroup
{
    public decimal taxAmount { get; set; }
    public long id { get; set; }

    public int? buyerId { get; set; }

    public string? guestEmail { get; set; }

    public int? couponId { get; set; }

    public decimal subtotal { get; set; }

    public decimal discountAmount { get; set; }

    public decimal shippingFee { get; set; }

    public decimal totalAmount { get; set; }

    public string currency { get; set; } = null!;

    public string status { get; set; } = null!;

    public DateTime? paymentDeadline { get; set; }

    public DateTime? inventoryReleasedAt { get; set; }

    public DateTime createdAt { get; set; }

    public DateTime? confirmedAt { get; set; }

    public virtual CouponUsage? CouponUsage { get; set; }

    public virtual ICollection<GuestCheckoutAccess> GuestCheckoutAccesses { get; set; } = new List<GuestCheckoutAccess>();

    public virtual ICollection<OrderTable> OrderTables { get; set; } = new List<OrderTable>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual User? buyer { get; set; }

    public virtual Coupon? coupon { get; set; }
}
