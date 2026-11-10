namespace Buyer.Application.Interfaces.Authentication;

public interface IUserRepository
{
    Task<AuthUser?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<bool> ExistsByNormalizedUsernameAsync(string normalizedUsername, CancellationToken cancellationToken);
    Task<int> CreateAsync(
        string email,
        string username,
        string passwordHash,
        string role,
        string accountStatus,
        DateTime createdAtUtc,
        CancellationToken cancellationToken);
    Task RecordSuccessfulLoginAsync(
        int userId,
        DateTime lastLoginAtUtc,
        string? upgradedPasswordHash,
        CancellationToken cancellationToken);
}
