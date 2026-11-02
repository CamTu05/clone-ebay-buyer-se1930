using Buyer.Application.Features.Profile.DTOs;

namespace Buyer.Application.Features.Profile;

public enum ProfileFailure
{
    None,
    UserNotFound,
    AccountUnavailable,
    ValidationFailed,
    UsernameTaken,
    UsernameChangeRestricted,
    ConcurrentConflict
}

public sealed record ProfileFieldError(string Code, string? Field);

public sealed record ProfileServiceResult(
    ProfileResponse? Value,
    ProfileFailure Failure,
    IReadOnlyList<ProfileFieldError> Errors)
{
    public bool IsSuccess => Failure == ProfileFailure.None && Value is not null;

    public static ProfileServiceResult Success(ProfileResponse value) =>
        new(value, ProfileFailure.None, Array.Empty<ProfileFieldError>());

    public static ProfileServiceResult Failed(
        ProfileFailure failure, params ProfileFieldError[] errors) =>
        new(null, failure, errors);
}

public interface IProfileService
{
    Task<ProfileServiceResult> GetProfileAsync(
        int authenticatedUserId, CancellationToken cancellationToken = default);

    Task<ProfileServiceResult> UpdateProfileAsync(
        int authenticatedUserId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default);
}
