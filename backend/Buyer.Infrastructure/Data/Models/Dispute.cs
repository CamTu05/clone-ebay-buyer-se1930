using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class Dispute
{
    public long id { get; set; }

    public long orderId { get; set; }

    public int raisedBy { get; set; }

    public string description { get; set; } = null!;

    public string status { get; set; } = null!;

    public string? resolution { get; set; }

    public DateTime createdAt { get; set; }

    public DateTime? resolvedAt { get; set; }

    public virtual OrderTable order { get; set; } = null!;

    public virtual User raisedByNavigation { get; set; } = null!;
}
