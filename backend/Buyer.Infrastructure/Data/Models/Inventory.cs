using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class Inventory
{
    public int id { get; set; }

    public int listingId { get; set; }

    public int quantity { get; set; }

    public int reservedQuantity { get; set; }

    public DateTime lastUpdated { get; set; }

    public byte[] rowVersion { get; set; } = null!;

    public virtual Listing listing { get; set; } = null!;
}
