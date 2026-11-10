using Buyer.Application.DTOs.Profile;

namespace Buyer.Application.DTOs.Profile;

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
