namespace Buyer.Application.Features.Profile;

public sealed record ProfileUserSnapshot(
    int Id,
    string Username,
    string Email,
    string? PhoneNumber,
    string? AvatarUrl,
    bool EmailVerified,
    bool PhoneVerified,
    string Role,
    string AccountStatus,
    DateTime? LastUsernameChangedAtUtc = null);

public sealed record ProfileChanges(
    int UserId,
    string ExpectedUsername,
    string? NewUsername,
    string? NewAvatarUrl,
    bool ClearAvatar);

public enum ProfileSaveStatus
{
    Updated,
    UserNotFound,
    AccountUnavailable,
    UsernameTaken,
    UsernameChangeRestricted,
    ConcurrentConflict
}

public sealed record ProfileSaveResult(
    ProfileSaveStatus Status,
    ProfileUserSnapshot? UpdatedProfile = null);

public interface IProfileRepository
{
    Task<ProfileUserSnapshot?> GetByUserIdAsync(
        int userId, CancellationToken cancellationToken = default);

    // Advisory precheck only. Actual check MUST be inside SaveProfileAsync's transaction.
    Task<bool> IsUsernameTakenAsync(
        string username, int excludeUserId,
        CancellationToken cancellationToken = default);

    Task<ProfileSaveResult> SaveProfileAsync(
        ProfileChanges changes, CancellationToken cancellationToken = default);
}
