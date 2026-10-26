using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class CheckoutGroup
{
    public long id { get; set; }

    public int? buyerId { get; set; }

    public string? guestEmail { get; set; }

    public decimal subtotal { get; set; }

    public decimal discountAmount { get; set; }

    public decimal shippingFee { get; set; }

    public decimal taxAmount { get; set; }

    public decimal importCharge { get; set; }

    public decimal totalAmount { get; set; }

    public string currency { get; set; } = null!;

    public DateTime createdAt { get; set; }

    public virtual ICollection<OrderTable> OrderTables { get; set; } = new List<OrderTable>();

    public virtual User? buyer { get; set; }
}
