using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class Review
{
    public long id { get; set; }

    public long orderItemId { get; set; }

    public int productId { get; set; }

    public int reviewerId { get; set; }

    public int rating { get; set; }

    public string? comment { get; set; }

    public DateTime createdAt { get; set; }

    public virtual OrderItem orderItem { get; set; } = null!;

    public virtual Product product { get; set; } = null!;

    public virtual User reviewer { get; set; } = null!;
}
