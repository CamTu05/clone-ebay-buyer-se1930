namespace Buyer.Application.Services.Authentication;

public sealed class AuthService(
    IUserRepository users,
    IPasswordVerifier passwords,
    IAccessTokenGenerator tokens,
    IClock clock) : IAuthService
{
    public async Task<RegisterResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Password)
            || string.IsNullOrWhiteSpace(request.Username))
        {
            return RegisterResult.Failed(RegisterFailure.ValidationFailed, "Email, password, and username are required.");
        }

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        if (await users.ExistsByNormalizedEmailAsync(normalizedEmail, cancellationToken))
        {
            return RegisterResult.Failed(RegisterFailure.EmailAlreadyExists, "An account with this email already exists.");
        }

        var normalizedUsername = request.Username.Trim().ToUpperInvariant();
        if (await users.ExistsByNormalizedUsernameAsync(normalizedUsername, cancellationToken))
        {
            return RegisterResult.Failed(RegisterFailure.UsernameAlreadyExists, "This username is already taken.");
        }

        var dummyUser = new AuthUser(0, request.Email.Trim().ToLowerInvariant(), request.Username.Trim(), null, "BUYER", "ACTIVE", false);
        var passwordHash = passwords.Hash(dummyUser, request.Password);

        var userId = await users.CreateAsync(
            request.Email.Trim().ToLowerInvariant(),
            request.Username.Trim(),
            passwordHash,
            "BUYER",
            "ACTIVE",
            clock.UtcNow,
            cancellationToken);

        var createdUser = new AuthUser(
            userId,
            request.Email.Trim().ToLowerInvariant(),
            request.Username.Trim(),
            passwordHash,
            "BUYER",
            "ACTIVE",
            false);

        var token = tokens.Generate(createdUser);

        return RegisterResult.Success(new RegisterResponse(
            token.Value,
            "Bearer",
            token.ExpiresAtUtc,
            new RegisteredUser(
                userId,
                request.Username.Trim(),
                request.Email.Trim().ToLowerInvariant(),
                "BUYER")));
    }

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
                user.EmailVerified)));
    }
}
