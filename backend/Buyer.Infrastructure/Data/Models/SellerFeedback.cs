using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class SellerFeedback
{
    public long id { get; set; }

    public long orderId { get; set; }

    public int sellerId { get; set; }

    public int buyerId { get; set; }

    public string feedbackType { get; set; } = null!;

    public string? comment { get; set; }

    public DateTime createdAt { get; set; }

    public virtual User buyer { get; set; } = null!;

    public virtual OrderTable order { get; set; } = null!;

    public virtual User seller { get; set; } = null!;
}
