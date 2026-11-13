namespace Buyer.Infrastructure.BuyerSchema.Models;

public partial class ListingPromotion
{
    public long id { get; set; }
    public int listingId { get; set; }
    public string promotionType { get; set; } = null!;
    public decimal originalPrice { get; set; }
    public decimal salePrice { get; set; }
    public DateTime startsAt { get; set; }
    public DateTime endsAt { get; set; }
    public bool isActive { get; set; }
    public DateTime createdAt { get; set; }
    public virtual Listing listing { get; set; } = null!;
}
