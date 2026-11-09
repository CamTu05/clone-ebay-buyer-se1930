namespace Buyer.Application.Interfaces.Profile;

public interface IProfileService
{
    Task<ProfileServiceResult> GetProfileAsync(int authenticatedUserId, CancellationToken cancellationToken = default);
    Task<ProfileServiceResult> UpdateProfileAsync(int authenticatedUserId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
}
