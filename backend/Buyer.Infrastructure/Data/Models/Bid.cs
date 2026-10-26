using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class Bid
{
    public long id { get; set; }

    public int auctionId { get; set; }

    public int bidderId { get; set; }

    public decimal maximumBid { get; set; }

    public decimal effectiveBid { get; set; }

    public string status { get; set; } = null!;

    public DateTime createdAt { get; set; }

    public virtual Auction auction { get; set; } = null!;

    public virtual User bidder { get; set; } = null!;
}
