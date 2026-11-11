using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.BuyerSchema.Models;

public partial class CancellationRequest
{
    public long id { get; set; }

    public long orderId { get; set; }

    public int requestedBy { get; set; }

    public string reason { get; set; } = null!;

    public string status { get; set; } = null!;

    public DateTime createdAt { get; set; }

    public DateTime? resolvedAt { get; set; }

    public virtual OrderTable order { get; set; } = null!;

    public virtual User requestedByNavigation { get; set; } = null!;
}
