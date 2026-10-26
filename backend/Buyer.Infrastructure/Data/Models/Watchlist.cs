using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class Watchlist
{
    public long id { get; set; }

    public int userId { get; set; }

    public int listingId { get; set; }

    public DateTime createdAt { get; set; }

    public virtual Listing listing { get; set; } = null!;

    public virtual User user { get; set; } = null!;
}
