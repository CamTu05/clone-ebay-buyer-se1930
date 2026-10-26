using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class Store
{
    public int id { get; set; }

    public int sellerId { get; set; }

    public string storeName { get; set; } = null!;

    public string? description { get; set; }

    public string? bannerImageURL { get; set; }

    public DateTime createdAt { get; set; }

    public DateTime updatedAt { get; set; }

    public virtual User seller { get; set; } = null!;
}
