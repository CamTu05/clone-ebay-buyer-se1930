using System;
using System.Collections.Generic;

namespace Buyer.Infrastructure.Data.Models;

public partial class GiftCardTransaction
{
    public long id { get; set; }

    public long giftCardId { get; set; }

    public long? orderId { get; set; }

    public string transactionType { get; set; } = null!;

    public decimal amount { get; set; }

    public DateTime createdAt { get; set; }

    public virtual GiftCard giftCard { get; set; } = null!;

    public virtual OrderTable? order { get; set; }
}
