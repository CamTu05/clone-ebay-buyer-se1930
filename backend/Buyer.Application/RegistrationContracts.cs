using System.ComponentModel.DataAnnotations;

namespace Buyer.Application.Authentication;

public sealed record RegisterRequest(
    [param: Required, EmailAddress, StringLength(255)]
    string? Email,

    [param: Required, MinLength(6), MaxLength(100)]
    string? Password,

    [param: Required, StringLength(100, MinimumLength = 2)]
    string? Username);

public sealed record RegisteredUser(
    int Id,
    string Username,
    string Email,
    string Role);

public sealed record RegisterResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAt,
    RegisteredUser User);

public enum RegisterFailure
{
    None,
    EmailAlreadyExists,
    UsernameAlreadyExists,
    ValidationFailed
}

public sealed record RegisterResult(RegisterResponse? Value, RegisterFailure Failure, string? ErrorMessage = null)
{
    public bool IsSuccess => Value is not null;

    public static RegisterResult Success(RegisterResponse value) => new(value, RegisterFailure.None);

    public static RegisterResult Failed(RegisterFailure failure, string? message = null) => new(null, failure, message);
}

