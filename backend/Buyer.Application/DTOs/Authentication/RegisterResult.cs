namespace Buyer.Application.DTOs.Authentication;

public sealed record RegisterResult(RegisterResponse? Value, RegisterFailure Failure, string? ErrorMessage = null)
{
    public bool IsSuccess => Value is not null;

    public static RegisterResult Success(RegisterResponse value) => new(value, RegisterFailure.None);

    public static RegisterResult Failed(RegisterFailure failure, string? message = null) => new(null, failure, message);
}
