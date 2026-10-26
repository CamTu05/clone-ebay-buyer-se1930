using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class Address
{
    public int id { get; set; }

    public int userId { get; set; }

    public string fullName { get; set; } = null!;

    public string phone { get; set; } = null!;

    public string street { get; set; } = null!;

    public string? addressLine2 { get; set; }

    public string city { get; set; } = null!;

    public string? state { get; set; }

    public string? postalCode { get; set; }

    public string country { get; set; } = null!;

    public string? countryCode { get; set; }

    public bool isDefault { get; set; }

    public DateTime createdAt { get; set; }

    public DateTime updatedAt { get; set; }

    public virtual ICollection<OrderTable> OrderTables { get; set; } = new List<OrderTable>();

    public virtual User user { get; set; } = null!;
}
