using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class VerificationToken
{
    public int id { get; set; }

    public int userId { get; set; }

    public string tokenType { get; set; } = null!;

    public string tokenHash { get; set; } = null!;

    public DateTime expiresAt { get; set; }

    public DateTime? usedAt { get; set; }

    public DateTime createdAt { get; set; }

    public virtual User user { get; set; } = null!;
}
