using Buyer.Application.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Buyer.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting("Login")]
    [ProducesResponseType<ApiEnvelope<LoginResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiEnvelope<object>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiEnvelope<object>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiEnvelope<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiEnvelope<object>>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(ApiEnvelope<LoginResponse>.Succeeded(result.Value!, "Login successful"));
        }

        return result.Failure switch
        {
            LoginFailure.EmailNotVerified => StatusCode(
                StatusCodes.Status403Forbidden,
                ApiEnvelope<object>.Failure("AUTH_EMAIL_NOT_VERIFIED")),
            LoginFailure.AccountUnavailable => StatusCode(
                StatusCodes.Status403Forbidden,
                ApiEnvelope<object>.Failure("AUTH_ACCOUNT_UNAVAILABLE")),
            _ => Unauthorized(ApiEnvelope<object>.Failure(
                "AUTH_INVALID_CREDENTIALS",
                "Invalid email or password"))
        };
    }
}

public sealed record ApiError(string Code, string? Field);

public sealed record ApiEnvelope<T>(bool Success, string Message, T? Data, IReadOnlyList<ApiError>? Errors)
{
    public static ApiEnvelope<T> Succeeded(T data, string message = "Success") =>
        new(true, message, data, null);

    public static ApiEnvelope<T> Failure(string code, string message = "Request failed", string? field = null) =>
        new(false, message, default, [new ApiError(code, field)]);
}
