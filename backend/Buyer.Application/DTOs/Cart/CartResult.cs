namespace Buyer.Application.DTOs.Cart;

public sealed record CartResult(CartResponse? Value, CartFailure Failure)
{
    public bool IsSuccess => Value is not null;
    public static CartResult Success(CartResponse value) => new(value, CartFailure.None);
    public static CartResult Failed(CartFailure failure) => new(null, failure);
}
