using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Buyer.Application.Interfaces.Email;
using Buyer.Application.Interfaces.Profile;
using Buyer.Application.DTOs.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buyer.API.Controllers;

[ApiController]
[Authorize(Roles = "BUYER")]
[Route("api/me")]
public sealed class ProfileController(
    IProfileService profiles,
    IEmailVerificationService emailVerification) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiEnvelope<ProfileResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        if (!TryUserId(out int userId)) return Unauthorized(
            ApiEnvelope<object>.Failure("AUTH_UNAUTHORIZED"));

        var result = await profiles.GetProfileAsync(userId, cancellationToken);
        return Respond(result);
    }

    [HttpPatch]
    [ProducesResponseType(typeof(ApiEnvelope<ProfileResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PatchAsync(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryUserId(out int userId)) return Unauthorized(
            ApiEnvelope<object>.Failure("AUTH_UNAUTHORIZED"));

        var result = await profiles.UpdateProfileAsync(
            userId, request, cancellationToken);
        return Respond(result);
    }

    [HttpPost("email-verification/send")]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendEmailVerification(CancellationToken cancellationToken)
    {
        if (!TryUserId(out int userId)) return Unauthorized(
            ApiEnvelope<object>.Failure("AUTH_UNAUTHORIZED"));

        var result = await emailVerification.SendVerificationCodeAsync(userId, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(ApiEnvelope<object>.Succeeded(new { sent = true }, result.Message));
        }

        return BadRequest(ApiEnvelope<object>.Failure(result.ErrorCode ?? "VERIFICATION_SEND_FAILED", result.Message));
    }

    [HttpPost("email-verification/verify")]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailCodeRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryUserId(out int userId)) return Unauthorized(
            ApiEnvelope<object>.Failure("AUTH_UNAUTHORIZED"));

        var result = await emailVerification.VerifyCodeAsync(userId, request.Code, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(ApiEnvelope<object>.Succeeded(new { verified = true }, result.Message));
        }

        return BadRequest(ApiEnvelope<object>.Failure(result.ErrorCode ?? "VERIFICATION_FAILED", result.Message));
    }

    private bool TryUserId(out int userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(claim, out userId) && userId > 0;
    }

    private IActionResult Respond(ProfileServiceResult result)
    {
        if (result.IsSuccess)
            return Ok(ApiEnvelope<ProfileResponse>.Succeeded(result.Value!));

        var status = result.Failure switch
        {
            ProfileFailure.ValidationFailed => StatusCodes.Status400BadRequest,
            ProfileFailure.UserNotFound => StatusCodes.Status404NotFound,
             ProfileFailure.AccountUnavailable => StatusCodes.Status403Forbidden,
             ProfileFailure.UsernameTaken or ProfileFailure.UsernameChangeRestricted or
             ProfileFailure.ConcurrentConflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(status, new ApiEnvelope<object>(
            false,
            result.Failure switch
            {
                 ProfileFailure.ValidationFailed => "Validation failed",
                 ProfileFailure.UsernameTaken => "Username is unavailable",
                ProfileFailure.ConcurrentConflict => "Profile changed concurrently",
                ProfileFailure.UsernameChangeRestricted => "Username can only be changed once every 30 days",
                _ => "Profile request failed"
            },
            null,
            result.Errors.Select(e => new ApiError(e.Code, e.Field)).ToArray()));
    }
}
