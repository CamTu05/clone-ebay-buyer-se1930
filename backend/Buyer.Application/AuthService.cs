namespace Buyer.Application.Authentication;

public sealed class AuthService(
    IUserRepository users,
    IPasswordVerifier passwords,
    IAccessTokenGenerator tokens,
    IClock clock) : IAuthService
{
    public async Task<LoginResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return LoginResult.Failed(LoginFailure.InvalidCredentials);
        }

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await users.FindByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
        {
            passwords.PerformDummyVerification(request.Password);
            return LoginResult.Failed(LoginFailure.InvalidCredentials);
        }

        var verification = passwords.Verify(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return LoginResult.Failed(LoginFailure.InvalidCredentials);
        }

        if (!string.Equals(user.AccountStatus, "ACTIVE", StringComparison.Ordinal))
        {
            return LoginResult.Failed(LoginFailure.AccountUnavailable);
        }

        if (!string.Equals(user.Role, "BUYER", StringComparison.Ordinal))
        {
            return LoginResult.Failed(LoginFailure.AccountUnavailable);
        }

        if (!user.EmailVerified)
        {
            return LoginResult.Failed(LoginFailure.EmailNotVerified);
        }

        var upgradedPasswordHash = verification == PasswordVerificationResult.SuccessRehashNeeded
            ? passwords.Hash(user, request.Password)
            : null;
        await users.RecordSuccessfulLoginAsync(
            user.Id,
            clock.UtcNow,
            upgradedPasswordHash,
            cancellationToken);

        var token = tokens.Generate(user);
        return LoginResult.Success(new LoginResponse(
            token.Value,
            "Bearer",
            token.ExpiresAtUtc,
            new AuthenticatedUser(
                user.Id,
                user.Username,
                user.Email,
                user.Role,
                user.EmailVerified,
                user.PhoneVerified)));
    }
}
