using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class SellerFeedbackSummary
{
    public int sellerId { get; set; }

    public decimal? averageRating { get; set; }

    public int totalReviews { get; set; }

    public decimal? positiveRate { get; set; }

    public DateTime updatedAt { get; set; }

    public virtual User seller { get; set; } = null!;
}
