using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class PackageInfo
{
    public int id { get; set; }

    public int listingId { get; set; }

    public decimal weightKg { get; set; }

    public decimal? lengthCm { get; set; }

    public decimal? widthCm { get; set; }

    public decimal? heightCm { get; set; }

    public virtual Listing listing { get; set; } = null!;
}
