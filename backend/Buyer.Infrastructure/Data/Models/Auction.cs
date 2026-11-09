using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class Auction
{
    public int id { get; set; }

    public int listingId { get; set; }

    public decimal startingPrice { get; set; }

    public decimal currentPrice { get; set; }

    public decimal bidIncrement { get; set; }

    public DateTime startTime { get; set; }

    public DateTime endTime { get; set; }

    public int? highestBidderId { get; set; }

    public int? winnerId { get; set; }

    public decimal? winningPrice { get; set; }

    public int bidCount { get; set; }

    public string status { get; set; } = null!;

    public DateTime? resolvedAt { get; set; }

    public byte[] rowVersion { get; set; } = null!;

    public virtual ICollection<Bid> Bids { get; set; } = new List<Bid>();

    public virtual User? highestBidder { get; set; }

    public virtual Listing listing { get; set; } = null!;

    public virtual User? winner { get; set; }
}
