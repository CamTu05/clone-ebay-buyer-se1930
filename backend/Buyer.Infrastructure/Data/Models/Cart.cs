using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class Cart
{
    public int id { get; set; }

    public int userId { get; set; }

    public DateTime createdAt { get; set; }

    public DateTime updatedAt { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual User user { get; set; } = null!;
}
