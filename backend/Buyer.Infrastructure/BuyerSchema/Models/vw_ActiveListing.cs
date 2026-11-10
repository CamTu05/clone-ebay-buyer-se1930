using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.BuyerSchema.Models;

public partial class vw_ActiveListing
{
    public int listingId { get; set; }

    public int? productId { get; set; }

    public int sellerId { get; set; }

    public int categoryId { get; set; }

    public string title { get; set; } = null!;

    public string? description { get; set; }

    public string? itemCondition { get; set; }

    public string listingType { get; set; } = null!;

    public decimal? price { get; set; }

    public int quantity { get; set; }

    public string status { get; set; } = null!;

    public DateTime? publishedAt { get; set; }

    public string? catalogProductTitle { get; set; }

    public string? catalogBrand { get; set; }

    public int? auctionId { get; set; }

    public decimal? currentPrice { get; set; }

    public DateTime? auctionStartTime { get; set; }

    public DateTime? auctionEndTime { get; set; }

    public int? bidCount { get; set; }

    public int? highestBidderId { get; set; }
}
