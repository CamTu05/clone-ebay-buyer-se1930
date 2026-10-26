using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class OrderItem
{
    public long id { get; set; }

    public long orderId { get; set; }

    public int listingId { get; set; }

    public int productId { get; set; }

    public int sellerId { get; set; }

    public int quantity { get; set; }

    public decimal unitPrice { get; set; }

    public string productTitle { get; set; } = null!;

    public string? imageUrl { get; set; }

    public DateTime createdAt { get; set; }

    public virtual ICollection<ReturnRequestItem> ReturnRequestItems { get; set; } = new List<ReturnRequestItem>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual Listing listing { get; set; } = null!;

    public virtual OrderTable order { get; set; } = null!;

    public virtual Product product { get; set; } = null!;

    public virtual User seller { get; set; } = null!;
}
