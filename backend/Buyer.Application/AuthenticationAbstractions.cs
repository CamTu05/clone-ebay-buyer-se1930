namespace Buyer.Application.Authentication;

public sealed record AuthUser(
    int Id,
    string Email,
    string Username,
    string? PasswordHash,
    string Role,
    string AccountStatus,
    bool EmailVerified,
    bool PhoneVerified);

public interface IUserRepository
{
    Task<AuthUser?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task RecordSuccessfulLoginAsync(
        int userId,
        DateTime lastLoginAtUtc,
        string? upgradedPasswordHash,
        CancellationToken cancellationToken);
}

public enum PasswordVerificationResult
{
    Failed,
    Success,
    SuccessRehashNeeded
}

public interface IPasswordVerifier
{
    PasswordVerificationResult Verify(AuthUser user, string hashedPassword, string providedPassword);
    void PerformDummyVerification(string providedPassword);
    string Hash(AuthUser user, string password);
}

public interface IClock
{
    DateTime UtcNow { get; }
}

public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);

public interface IAccessTokenGenerator
{
    AccessToken Generate(AuthUser user);
}

public interface IAuthService
{
    Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
