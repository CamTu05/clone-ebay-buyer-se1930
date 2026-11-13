namespace Buyer.Infrastructure.Data.Models;

public partial class ReturnPolicy
{
    public int id { get; set; }
    public int sellerId { get; set; }
    public string name { get; set; } = null!;
    public bool returnsAccepted { get; set; }
    public bool freeReturns { get; set; }
    public int? returnWindowDays { get; set; }
    public bool isActive { get; set; }
    public DateTime createdAt { get; set; }
    public virtual ICollection<Listing> Listings { get; set; } = new List<Listing>();
    public virtual User seller { get; set; } = null!;
}
