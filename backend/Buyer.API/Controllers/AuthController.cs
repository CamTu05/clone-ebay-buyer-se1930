using Buyer.Application.Interfaces.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Buyer.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<ApiEnvelope<RegisterResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiEnvelope<object>>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiEnvelope<object>>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status201Created,
                ApiEnvelope<RegisterResponse>.Succeeded(result.Value!, "Registration successful"));
        }

        return result.Failure switch
        {
            RegisterFailure.EmailAlreadyExists => Conflict(
                ApiEnvelope<object>.Failure("AUTH_EMAIL_EXISTS", result.ErrorMessage ?? "Email already exists")),
            RegisterFailure.UsernameAlreadyExists => Conflict(
                ApiEnvelope<object>.Failure("AUTH_USERNAME_EXISTS", result.ErrorMessage ?? "Username already taken")),
            _ => BadRequest(
                ApiEnvelope<object>.Failure("AUTH_VALIDATION_FAILED", result.ErrorMessage ?? "Validation failed"))
        };
    }

    [HttpPost("login")]
    [EnableRateLimiting("Login")]
    [ProducesResponseType<ApiEnvelope<LoginResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiEnvelope<object>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiEnvelope<object>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiEnvelope<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiEnvelope<object>>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
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
