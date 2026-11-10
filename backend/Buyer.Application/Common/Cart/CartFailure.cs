namespace Buyer.Application.Common.Cart;

public enum CartFailure
{
    None,
    InvalidQuantity,
    ItemNotFound,
    ListingUnavailable,
    QuantityExceedsStock
}
