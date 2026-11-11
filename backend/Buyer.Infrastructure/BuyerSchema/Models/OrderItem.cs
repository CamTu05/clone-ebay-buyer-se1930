using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.BuyerSchema.Models;

public partial class OrderItem
{
    public long id { get; set; }

    public long orderId { get; set; }

    public int listingId { get; set; }

    public int? productId { get; set; }

    public int quantity { get; set; }

    public decimal unitPrice { get; set; }

    public string productTitle { get; set; } = null!;

    public string? imageUrl { get; set; }

    public DateTime createdAt { get; set; }

    public virtual ICollection<ReturnRequestItem> ReturnRequestItems { get; set; } = new List<ReturnRequestItem>();

    public virtual Review? Review { get; set; }

    public virtual Listing listing { get; set; } = null!;

    public virtual OrderTable order { get; set; } = null!;

    public virtual Product? product { get; set; }
}
