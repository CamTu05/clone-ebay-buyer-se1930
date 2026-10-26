using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class ReturnRequest
{
    public long id { get; set; }

    public long orderId { get; set; }

    public int requestedBy { get; set; }

    public string reasonCode { get; set; } = null!;

    public string? reasonDetail { get; set; }

    public string status { get; set; } = null!;

    public DateTime createdAt { get; set; }

    public DateTime? approvedAt { get; set; }

    public DateTime? completedAt { get; set; }

    public virtual ICollection<ReturnRequestItem> ReturnRequestItems { get; set; } = new List<ReturnRequestItem>();

    public virtual OrderTable order { get; set; } = null!;

    public virtual User requestedByNavigation { get; set; } = null!;
}
