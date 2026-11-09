using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class Payment
{
    public long id { get; set; }

    public long checkoutGroupId { get; set; }

    public decimal amount { get; set; }

    public string method { get; set; } = null!;

    public string? provider { get; set; }

    public string? transactionId { get; set; }

    public string status { get; set; } = null!;

    public string? failureReason { get; set; }

    public DateTime createdAt { get; set; }

    public DateTime? paidAt { get; set; }

    public virtual CheckoutGroup checkoutGroup { get; set; } = null!;
}
