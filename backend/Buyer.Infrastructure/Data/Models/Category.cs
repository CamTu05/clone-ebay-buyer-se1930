using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class Category
{
    public int id { get; set; }

    public int? parentCategoryId { get; set; }

    public string name { get; set; } = null!;

    public string slug { get; set; } = null!;

    public bool isActive { get; set; }

    public virtual ICollection<Category> InverseparentCategory { get; set; } = new List<Category>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual Category? parentCategory { get; set; }
}
