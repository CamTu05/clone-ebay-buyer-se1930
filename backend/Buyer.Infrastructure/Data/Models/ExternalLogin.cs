using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class ExternalLogin
{
    public int id { get; set; }

    public int userId { get; set; }

    public string provider { get; set; } = null!;

    public string providerUserId { get; set; } = null!;

    public DateTime createdAt { get; set; }

    public virtual User user { get; set; } = null!;
}
