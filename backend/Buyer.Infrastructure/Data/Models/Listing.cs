using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class Listing
{
    public int id { get; set; }

    public int? productId { get; set; }

    public int sellerId { get; set; }

    public int categoryId { get; set; }

    public int? shippingPolicyId { get; set; }

    public string title { get; set; } = null!;

    public string? description { get; set; }

    public string? itemCondition { get; set; }

    public string listingType { get; set; } = null!;

    public decimal? price { get; set; }

    public int quantity { get; set; }

    public string status { get; set; } = null!;

    public DateTime createdAt { get; set; }

    public DateTime? publishedAt { get; set; }

    public DateTime? endedAt { get; set; }

    public byte[] rowVersion { get; set; } = null!;

    public virtual Auction? Auction { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<ListingImage> ListingImages { get; set; } = new List<ListingImage>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ShippingPolicy? ShippingPolicy { get; set; }

    public virtual ICollection<Watchlist> Watchlists { get; set; } = new List<Watchlist>();

    public virtual Category category { get; set; } = null!;

    public virtual Product? product { get; set; }

    public virtual User seller { get; set; } = null!;
}
