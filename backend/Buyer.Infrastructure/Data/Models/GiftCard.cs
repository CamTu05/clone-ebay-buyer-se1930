using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class GiftCard
{
    public long id { get; set; }

    public string codeHash { get; set; } = null!;

    public decimal initialBalance { get; set; }

    public decimal currentBalance { get; set; }

    public int? linkedUserId { get; set; }

    public string status { get; set; } = null!;

    public DateTime? expiresAt { get; set; }

    public DateTime createdAt { get; set; }

    public virtual ICollection<GiftCardTransaction> GiftCardTransactions { get; set; } = new List<GiftCardTransaction>();

    public virtual User? linkedUser { get; set; }
}
