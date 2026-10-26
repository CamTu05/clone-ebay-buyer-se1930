using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class vw_ActiveListing
{
    public int listingId { get; set; }

    public int productId { get; set; }

    public string title { get; set; } = null!;

    public string? description { get; set; }

    public int categoryId { get; set; }

    public int sellerId { get; set; }

    public string listingType { get; set; } = null!;

    public decimal? price { get; set; }

    public int quantity { get; set; }

    public string status { get; set; } = null!;

    public DateTime? publishedAt { get; set; }

    public int? auctionId { get; set; }

    public decimal? currentPrice { get; set; }

    public DateTime? startTime { get; set; }

    public DateTime? endTime { get; set; }

    public int? bidCount { get; set; }

    public int? highestBidderId { get; set; }
}
