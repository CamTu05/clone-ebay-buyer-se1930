using Buyer.Application.Features.Profile;
using Buyer.Application.Features.Profile.DTOs;

namespace Buyer.UnitTests;

public sealed class ProfileServiceTests
{
    private static readonly ProfileUserSnapshot Buyer = new(
        12, "buyer_old", "buyer@example.com", null,
        "https://example.com/old.png", true, false, "BUYER", "ACTIVE");

    [Fact]
    public async Task GetUsesAuthenticatedUserId()
    {
        var repo = new FakeRepository { User = Buyer };
        var result = await new ProfileService(repo).GetProfileAsync(12);
        Assert.True(result.IsSuccess);
        Assert.Equal(12, repo.LastReadId);
        Assert.Equal("buyer_old", result.Value!.Username);
        Assert.Equal("buyer@example.com", result.Value.Email);
    }

    [Fact]
    public async Task GetRejectsMissingUser()
    {
        var result = await new ProfileService(new FakeRepository())
            .GetProfileAsync(404);
        Assert.Equal(ProfileFailure.UserNotFound, result.Failure);
    }

    [Theory]
    [InlineData("SELLER", "ACTIVE")]
    [InlineData("BUYER", "LOCKED")]
    [InlineData("BUYER", "SUSPENDED")]
    public async Task UpdateRejectsWrongRoleOrState(string role, string status)
    {
        var repo = new FakeRepository { User = Buyer with
            { Role = role, AccountStatus = status } };
        var result = await new ProfileService(repo)
            .UpdateProfileAsync(12, new() { AvatarUrl = "https://example.com/new.png" });
        Assert.Equal(ProfileFailure.AccountUnavailable, result.Failure);
        Assert.Null(repo.LastChanges);
    }

    [Fact]
    public async Task UpdateRejectsInvalidBodyWithoutWriting()
    {
        var repo = new FakeRepository { User = Buyer };
        var result = await new ProfileService(repo)
            .UpdateProfileAsync(12, new() { Username = "??" });
        Assert.Equal(ProfileFailure.ValidationFailed, result.Failure);
        Assert.Null(repo.LastChanges);
    }

    [Fact]
    public async Task UpdateRejectsUsernameEqualToEmailLocalPart()
    {
        var repo = new FakeRepository { User = Buyer with { Email = "buyer_new@example.com" } };
        var result = await new ProfileService(repo)
            .UpdateProfileAsync(12, new() { Username = "buyer_new" });
        Assert.Equal(ProfileFailure.ValidationFailed, result.Failure);
        Assert.Contains(result.Errors, e => e.Code == "PROFILE_USERNAME_EMAIL_LIKE");
        Assert.Null(repo.LastChanges);
    }

    [Fact]
    public async Task SameUsernameCaseInsensitiveIsNoOp()
    {
        var repo = new FakeRepository { User = Buyer };
        var result = await new ProfileService(repo)
            .UpdateProfileAsync(12, new() { Username = "BUYER_OLD" });
        Assert.True(result.IsSuccess);
        Assert.Null(repo.LastChanges);
    }

    [Fact]
    public async Task NewUsernamePassesThroughWithJwtUserId()
    {
        var repo = new FakeRepository { User = Buyer };
        var result = await new ProfileService(repo)
            .UpdateProfileAsync(12, new() { Username = "buyer_new" });
        Assert.True(result.IsSuccess);
        Assert.Equal(12, repo.LastChanges!.UserId);
        Assert.Equal("buyer_old", repo.LastChanges.ExpectedUsername);
        Assert.Equal("buyer_new", repo.LastChanges.NewUsername);
    }

    [Fact]
    public async Task AvatarOnlyDoesNotAskForUsernameChange()
    {
        var repo = new FakeRepository { User = Buyer };
        var result = await new ProfileService(repo)
            .UpdateProfileAsync(12, new() { AvatarUrl = "https://example.com/new.png" });
        Assert.True(result.IsSuccess);
        Assert.Null(repo.LastChanges!.NewUsername);
        Assert.Equal("https://example.com/new.png", repo.LastChanges.NewAvatarUrl);
    }

    [Fact]
    public async Task ClearAvatarDoesNotAskForUsernameChange()
    {
        var repo = new FakeRepository { User = Buyer };
        var result = await new ProfileService(repo)
            .UpdateProfileAsync(12, new() { ClearAvatar = true });
        Assert.True(result.IsSuccess);
        Assert.True(repo.LastChanges!.ClearAvatar);
        Assert.Null(repo.LastChanges.NewUsername);
    }

    [Theory]
    [InlineData(ProfileSaveStatus.UsernameTaken, ProfileFailure.UsernameTaken)]
    [InlineData(ProfileSaveStatus.UsernameChangeRestricted, ProfileFailure.UsernameChangeRestricted)]
    [InlineData(ProfileSaveStatus.ConcurrentConflict, ProfileFailure.ConcurrentConflict)]
    [InlineData(ProfileSaveStatus.AccountUnavailable, ProfileFailure.AccountUnavailable)]
    [InlineData(ProfileSaveStatus.UserNotFound, ProfileFailure.UserNotFound)]
    public async Task MapsRepositoryErrors(
        ProfileSaveStatus status, ProfileFailure expected)
    {
        var repo = new FakeRepository { User = Buyer, SaveStatus = status };
        var result = await new ProfileService(repo)
            .UpdateProfileAsync(12, new() { Username = "buyer_new" });
        Assert.Equal(expected, result.Failure);
    }

    private sealed class FakeRepository : IProfileRepository
    {
        public ProfileUserSnapshot? User { get; init; }
        public ProfileSaveStatus SaveStatus { get; init; } = ProfileSaveStatus.Updated;
        public int LastReadId { get; private set; }
        public ProfileChanges? LastChanges { get; private set; }

        public Task<ProfileUserSnapshot?> GetByUserIdAsync(
            int userId, CancellationToken cancellationToken = default)
        {
            LastReadId = userId;
            return Task.FromResult(User is { } user && user.Id == userId ? user : null);
        }

        public Task<bool> IsUsernameTakenAsync(string username,
            int excludeUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<ProfileSaveResult> SaveProfileAsync(
            ProfileChanges changes, CancellationToken cancellationToken = default)
        {
            LastChanges = changes;
            if (SaveStatus != ProfileSaveStatus.Updated)
                return Task.FromResult(new ProfileSaveResult(SaveStatus));

            var updated = User! with
            {
                Username = changes.NewUsername ?? User.Username,
                AvatarUrl = changes.ClearAvatar ? null : changes.NewAvatarUrl ?? User.AvatarUrl
            };
            return Task.FromResult(new ProfileSaveResult(ProfileSaveStatus.Updated, updated));
        }
    }
}
