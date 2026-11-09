using System.ComponentModel.DataAnnotations;

namespace Buyer.Application.DTOs.Authentication;

public sealed record LoginResult(LoginResponse? Value, LoginFailure Failure)
{
    public bool IsSuccess => Value is not null;

    public static LoginResult Success(LoginResponse value) => new(value, LoginFailure.None);

    public static LoginResult Failed(LoginFailure failure) => new(null, failure);
}
