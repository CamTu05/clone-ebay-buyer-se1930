using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class CartItem
{
    public int id { get; set; }

    public int cartId { get; set; }

    public int listingId { get; set; }

    public int quantity { get; set; }

    public DateTime addedAt { get; set; }

    public virtual Cart cart { get; set; } = null!;

    public virtual Listing listing { get; set; } = null!;
}
