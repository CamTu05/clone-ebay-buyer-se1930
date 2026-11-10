using Buyer.Application.Authentication;

namespace Buyer.UnitTests;

public sealed class AuthServiceTests
{
    private static readonly AuthUser ActiveBuyer = new(
        1, "buyer@example.com", "buyer", "hash", "BUYER", "ACTIVE", true, false);

    [Fact]
    public async Task LoginReturnsTokenForValidCredentials()
    {
        var repository = new FakeUserRepository(ActiveBuyer);
        var service = CreateService(repository, PasswordVerificationResult.Success);

        var result = await service.LoginAsync(new LoginRequest(" Buyer@Example.com ", "password"));

        Assert.True(result.IsSuccess);
        Assert.Equal("token", result.Value!.AccessToken);
        Assert.Equal("BUYER@EXAMPLE.COM", repository.ReceivedEmail);
    }

    [Theory]
    [InlineData(false, PasswordVerificationResult.Success)]
    [InlineData(true, PasswordVerificationResult.Failed)]
    public async Task LoginRejectsUnknownUserOrWrongPassword(
        bool hasUser,
        PasswordVerificationResult verification)
    {
        var service = CreateService(new FakeUserRepository(hasUser ? ActiveBuyer : null), verification);

        var result = await service.LoginAsync(new LoginRequest("buyer@example.com", "password"));

        Assert.Equal(LoginFailure.InvalidCredentials, result.Failure);
    }

    [Theory]
    [InlineData("LOCKED")]
    [InlineData("SUSPENDED")]
    public async Task LoginRejectsUnavailableAccount(string status)
    {
        var service = CreateService(
            new FakeUserRepository(ActiveBuyer with { AccountStatus = status }),
            PasswordVerificationResult.Success);

        var result = await service.LoginAsync(new LoginRequest("buyer@example.com", "password"));

        Assert.Equal(LoginFailure.AccountUnavailable, result.Failure);
    }

    [Fact]
    public async Task LoginRejectsNonBuyer()
    {
        var service = CreateService(
            new FakeUserRepository(ActiveBuyer with { Role = "SELLER" }),
            PasswordVerificationResult.Success);

        var result = await service.LoginAsync(new LoginRequest("buyer@example.com", "password"));

        Assert.Equal(LoginFailure.AccountUnavailable, result.Failure);
    }

    [Fact]
    public async Task LoginRejectsUnverifiedEmail()
    {
        var service = CreateService(
            new FakeUserRepository(ActiveBuyer with { EmailVerified = false }),
            PasswordVerificationResult.Success);

        var result = await service.LoginAsync(new LoginRequest("buyer@example.com", "password"));

        Assert.Equal(LoginFailure.EmailNotVerified, result.Failure);
    }

    [Fact]
    public async Task LoginPersistsRehashedPassword()
    {
        var repository = new FakeUserRepository(ActiveBuyer);
        var service = CreateService(repository, PasswordVerificationResult.SuccessRehashNeeded);

        await service.LoginAsync(new LoginRequest("buyer@example.com", "password"));

        Assert.Equal("new-hash", repository.UpdatedHash);
    }

    private static AuthService CreateService(
        FakeUserRepository repository,
        PasswordVerificationResult verification) =>
        new(repository, new FakePasswordVerifier(verification), new FakeTokenGenerator(), new FakeClock());

    private sealed class FakeUserRepository(AuthUser? user) : IUserRepository
    {
        public string? ReceivedEmail { get; private set; }
        public string? UpdatedHash { get; private set; }
        public DateTime? LastLoginAtUtc { get; private set; }
        public int SaveCalls { get; private set; }

        public Task<AuthUser?> FindByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken)
        {
            ReceivedEmail = normalizedEmail;
            return Task.FromResult(user);
        }

        public Task RecordSuccessfulLoginAsync(
            int userId,
            DateTime lastLoginAtUtc,
            string? upgradedPasswordHash,
            CancellationToken cancellationToken)
        {
            UpdatedHash = upgradedPasswordHash;
            LastLoginAtUtc = lastLoginAtUtc;
            SaveCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordVerifier(PasswordVerificationResult result) : IPasswordVerifier
    {
        public PasswordVerificationResult Verify(
            AuthUser user,
            string hashedPassword,
            string providedPassword) => result;

        public void PerformDummyVerification(string providedPassword)
        {
        }

        public string Hash(AuthUser user, string password) => "new-hash";
    }

    private sealed class FakeClock : IClock
    {
        public DateTime UtcNow => new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeTokenGenerator : IAccessTokenGenerator
    {
        public AccessToken Generate(AuthUser user) => new("token", DateTime.UtcNow.AddMinutes(15));
    }
}
