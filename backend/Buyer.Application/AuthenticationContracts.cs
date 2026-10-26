using System.ComponentModel.DataAnnotations;

namespace Buyer.Application.Authentication;

public sealed record LoginRequest(
    [property: Required, EmailAddress] string? Email,
    [property: Required] string? Password);

public sealed record AuthenticatedUser(
    int Id,
    string Username,
    string Email,
    string Role,
    bool EmailVerified,
    bool PhoneVerified);

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAt,
    AuthenticatedUser User);

public enum LoginFailure
{
    None,
    InvalidCredentials,
    AccountUnavailable,
    EmailNotVerified
}

public sealed record LoginResult(LoginResponse? Value, LoginFailure Failure)
{
    public bool IsSuccess => Value is not null;

    public static LoginResult Success(LoginResponse value) => new(value, LoginFailure.None);

    public static LoginResult Failed(LoginFailure failure) => new(null, failure);
}
