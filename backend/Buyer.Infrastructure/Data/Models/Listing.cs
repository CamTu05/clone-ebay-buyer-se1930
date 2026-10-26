using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class Listing
{
    public int id { get; set; }

    public int productId { get; set; }

    public int sellerId { get; set; }

    public int? shippingPolicyId { get; set; }

    public string listingType { get; set; } = null!;

    public decimal? price { get; set; }

    public int quantity { get; set; }

    public bool allowBestOffer { get; set; }

    public string status { get; set; } = null!;

    public DateTime createdAt { get; set; }

    public DateTime? publishedAt { get; set; }

    public DateTime? endedAt { get; set; }

    public virtual Auction? Auction { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual Inventory? Inventory { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual PackageInfo? PackageInfo { get; set; }

    public virtual ICollection<Watchlist> Watchlists { get; set; } = new List<Watchlist>();

    public virtual Product product { get; set; } = null!;

    public virtual User seller { get; set; } = null!;

    public virtual ShippingPolicy? shippingPolicy { get; set; }

    public virtual ICollection<Coupon> coupons { get; set; } = new List<Coupon>();
}
