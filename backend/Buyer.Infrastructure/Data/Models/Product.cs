using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class Product
{
    public int id { get; set; }

    public string title { get; set; } = null!;

    public string? description { get; set; }

    public int categoryId { get; set; }

    public string? brand { get; set; }

    public string? itemCondition { get; set; }

    public DateTime createdAt { get; set; }

    public DateTime updatedAt { get; set; }

    public virtual ICollection<Listing> Listings { get; set; } = new List<Listing>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual Category category { get; set; } = null!;
}
