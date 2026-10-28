using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Buyer.Application.Features.Profile;
using Buyer.Application.Features.Profile.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buyer.API.Controllers;

[ApiController]
[Authorize(Roles = "BUYER")]
[Route("api/me")]
public sealed class ProfileController(IProfileService profiles) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiEnvelope<ProfileResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (!TryUserId(out int userId)) return Unauthorized(
            ApiEnvelope<object>.Failure("AUTH_UNAUTHORIZED"));

        var result = await profiles.GetProfileAsync(userId, cancellationToken);
        return Respond(result);
    }

    [HttpPatch]
    [ProducesResponseType(typeof(ApiEnvelope<ProfileResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Patch(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryUserId(out int userId)) return Unauthorized(
            ApiEnvelope<object>.Failure("AUTH_UNAUTHORIZED"));

        var result = await profiles.UpdateProfileAsync(
            userId, request, cancellationToken);
        return Respond(result);
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
            ProfileFailure.UsernameTaken or
            ProfileFailure.UsernameChangeRestricted or
            ProfileFailure.ConcurrentConflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(status, new ApiEnvelope<object>(
            false,
            result.Failure switch
            {
                ProfileFailure.ValidationFailed => "Validation failed",
                ProfileFailure.UsernameChangeRestricted =>
                    "Username can only be changed once every 30 days",
                ProfileFailure.UsernameTaken => "Username is unavailable",
                ProfileFailure.ConcurrentConflict => "Profile changed concurrently",
                _ => "Profile request failed"
            },
            null,
            result.Errors.Select(e => new ApiError(e.Code, e.Field)).ToArray()));
    }
}
