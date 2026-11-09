using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class ShippingInfo
{
    public long id { get; set; }

    public long orderId { get; set; }

    public string? carrier { get; set; }

    public string? trackingNumber { get; set; }

    public string status { get; set; } = null!;

    public DateTime? shippedAt { get; set; }

    public DateTime? estimatedArrival { get; set; }

    public DateTime? deliveredAt { get; set; }

    public DateTime updatedAt { get; set; }

    public virtual OrderTable order { get; set; } = null!;
}
