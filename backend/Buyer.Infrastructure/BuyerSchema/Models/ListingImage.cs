using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.BuyerSchema.Models;

public partial class ListingImage
{
    public int id { get; set; }

    public int listingId { get; set; }

    public string imageUrl { get; set; } = null!;

    public int sortOrder { get; set; }

    public bool isPrimary { get; set; }

    public virtual Listing listing { get; set; } = null!;
}
