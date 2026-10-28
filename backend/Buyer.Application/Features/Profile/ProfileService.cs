using Buyer.Application.Features.Profile.DTOs;

namespace Buyer.Application.Features.Profile;

public sealed class ProfileService(IProfileRepository repository) : IProfileService
{
    public async Task<ProfileServiceResult> GetProfileAsync(
        int authenticatedUserId, CancellationToken cancellationToken = default)
    {
        if (authenticatedUserId <= 0) return NotFound();

        var user = await repository.GetByUserIdAsync(
            authenticatedUserId, cancellationToken);
        if (user is null) return NotFound();
        if (!CanAccess(user)) return Unavailable();
        return ProfileServiceResult.Success(ToResponse(user));
    }

    public async Task<ProfileServiceResult> UpdateProfileAsync(
        int authenticatedUserId, UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = UpdateProfileValidator.Validate(request);
        if (errors.Count != 0)
            return ProfileServiceResult.Failed(ProfileFailure.ValidationFailed,
                errors.ToArray());

        if (authenticatedUserId <= 0) return NotFound();

        var current = await repository.GetByUserIdAsync(
            authenticatedUserId, cancellationToken);
        if (current is null) return NotFound();
        if (!CanAccess(current)) return Unavailable();

        // Username is case-insensitive. Case-only edits are no-ops.
        bool changeUsername = request.Username is not null &&
            !string.Equals(current.Username, request.Username,
                StringComparison.OrdinalIgnoreCase);
        bool changeAvatar = request.ClearAvatar
            ? current.AvatarUrl is not null
            : request.AvatarUrl is not null &&
              !string.Equals(current.AvatarUrl, request.AvatarUrl,
                  StringComparison.Ordinal);

        if (changeUsername)
        {
            errors = UpdateProfileValidator.ValidateForAccount(
                request.Username, current.Email);
            if (errors.Count != 0)
                return ProfileServiceResult.Failed(ProfileFailure.ValidationFailed,
                    errors.ToArray());
        }

        if (!changeUsername && !changeAvatar)
            return ProfileServiceResult.Success(ToResponse(current));

        var changes = new ProfileChanges(
            UserId: authenticatedUserId,       // Always from authenticated JWT
            ExpectedUsername: current.Username,
            NewUsername: changeUsername ? request.Username : null,
            NewAvatarUrl: request.ClearAvatar ? null : request.AvatarUrl,
            ClearAvatar: request.ClearAvatar);

        var saved = await repository.SaveProfileAsync(changes, cancellationToken);
        return saved.Status switch
        {
            ProfileSaveStatus.Updated when saved.UpdatedProfile is not null =>
                ProfileServiceResult.Success(ToResponse(saved.UpdatedProfile)),
            ProfileSaveStatus.UserNotFound => NotFound(),
            ProfileSaveStatus.AccountUnavailable => Unavailable(),
            ProfileSaveStatus.UsernameTaken => ProfileServiceResult.Failed(
                ProfileFailure.UsernameTaken,
                new ProfileFieldError("PROFILE_USERNAME_TAKEN", "username")),
            ProfileSaveStatus.UsernameChangeRestricted => ProfileServiceResult.Failed(
                ProfileFailure.UsernameChangeRestricted,
                new ProfileFieldError("PROFILE_USERNAME_CHANGE_RESTRICTED", "username")),
            _ => ProfileServiceResult.Failed(ProfileFailure.ConcurrentConflict,
                new ProfileFieldError("PROFILE_CONCURRENT_CONFLICT", null))
        };
    }

    private static bool CanAccess(ProfileUserSnapshot user) =>
        user.Role == "BUYER" && user.AccountStatus == "ACTIVE";

    private static ProfileResponse ToResponse(ProfileUserSnapshot user) => new(
        user.Id, user.Username, user.Email, user.PhoneNumber,
        user.AvatarUrl, user.EmailVerified, user.PhoneVerified);

    private static ProfileServiceResult NotFound() => ProfileServiceResult.Failed(
        ProfileFailure.UserNotFound, new ProfileFieldError("PROFILE_NOT_FOUND", null));

    private static ProfileServiceResult Unavailable() => ProfileServiceResult.Failed(
        ProfileFailure.AccountUnavailable,
        new ProfileFieldError("PROFILE_ACCOUNT_UNAVAILABLE", null));
}
