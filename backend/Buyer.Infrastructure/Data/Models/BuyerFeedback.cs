namespace Buyer.Infrastructure.Data.Models;

public partial class BuyerFeedback
{
    public long id { get; set; }

    public long orderId { get; set; }

    public int buyerId { get; set; }

    public int sellerId { get; set; }

    public string comment { get; set; } = null!;

    public DateTime createdAt { get; set; }

    public virtual User buyer { get; set; } = null!;

    public virtual OrderTable order { get; set; } = null!;

    public virtual User seller { get; set; } = null!;
}
