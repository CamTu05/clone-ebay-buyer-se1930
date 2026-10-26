using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class ProductImage
{
    public int id { get; set; }

    public int productId { get; set; }

    public string imageUrl { get; set; } = null!;

    public int sortOrder { get; set; }

    public bool isPrimary { get; set; }

    public virtual Product product { get; set; } = null!;
}
