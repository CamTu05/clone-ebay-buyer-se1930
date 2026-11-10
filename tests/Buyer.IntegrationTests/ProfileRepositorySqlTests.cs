using Buyer.Application.Interfaces.Authentication;
using Buyer.Application.Interfaces.Profile;
using Buyer.Infrastructure.Repositories.Addresses;
using Buyer.Infrastructure.Data.Models;
using Buyer.Infrastructure.Repositories.Profile;
using Microsoft.EntityFrameworkCore;

namespace Buyer.IntegrationTests;

public sealed class ProfileRepositorySqlTests
{
    [Fact]
    public async Task SqlAllowsRepeatedChangesAndReleasedOldUsername()
    {
        string? connection = Environment.GetEnvironmentVariable(
            "PROFILE_TEST_SQL_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection)) return;

        var clock = new MutableClock(new DateTime(2026, 10, 27, 10, 0, 0,
            DateTimeKind.Utc));
        (int aliceId, int bobId) = await SeedTwoBuyers(connection);
        try
        {
            await using var db = CreateDb(connection);
            var repo = new ProfileRepository(db, clock);
            var alice = (await repo.GetByUserIdAsync(aliceId))!;
            string firstName = $"n{aliceId}xyz001";
            string secondName = $"n{aliceId}xyz002";

            var first = await repo.SaveProfileAsync(new ProfileChanges(
                aliceId, alice.Username, firstName, null, false));
            Assert.Equal(ProfileSaveStatus.Updated, first.Status);
            var second = await repo.SaveProfileAsync(new ProfileChanges(
                aliceId, firstName, secondName, null, false));
            Assert.Equal(ProfileSaveStatus.Updated, second.Status);

            var bob = (await repo.GetByUserIdAsync(bobId))!;
            var released = await repo.SaveProfileAsync(new ProfileChanges(
                bobId, bob.Username, alice.Username, null, false));
            Assert.Equal(ProfileSaveStatus.Updated, released.Status);
        }
        finally
        {
            await Cleanup(connection, aliceId, bobId);
        }
    }

    [Fact]
    public async Task SqlSerializesTwoConcurrentChangesForSameAccount()
    {
        string? connection = Environment.GetEnvironmentVariable(
            "PROFILE_TEST_SQL_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection)) return;

        var clock = new MutableClock(DateTime.UtcNow);
        (int aliceId, int bobId) = await SeedTwoBuyers(connection);
        try
        {
            await using var firstDb = CreateDb(connection);
            await using var secondDb = CreateDb(connection);
            var current = await new ProfileRepository(firstDb, clock)
                .GetByUserIdAsync(aliceId);
            var firstRepository = new ProfileRepository(firstDb, clock);
            var secondRepository = new ProfileRepository(secondDb, clock);

            var outcomes = await Task.WhenAll(
                firstRepository.SaveProfileAsync(new ProfileChanges(
                    aliceId, current!.Username, $"c{aliceId}xyz001", null, false)),
                secondRepository.SaveProfileAsync(new ProfileChanges(
                    aliceId, current.Username, $"c{aliceId}xyz002", null, false)));
            Assert.Single(outcomes, result => result.Status == ProfileSaveStatus.Updated);
            Assert.Single(outcomes, result => result.Status != ProfileSaveStatus.Updated);
        }
        finally
        {
            await Cleanup(connection, aliceId, bobId);
        }
    }

    [Fact]
    public async Task SqlRejectsCurrentUsernameCaseInsensitively()
    {
        string? connection = Environment.GetEnvironmentVariable(
            "PROFILE_TEST_SQL_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection)) return;

        var clock = new MutableClock(DateTime.UtcNow);
        (int aliceId, int bobId) = await SeedTwoBuyers(connection);
        try
        {
            await using var db = CreateDb(connection);
            var repo = new ProfileRepository(db, clock);
            var alice = (await repo.GetByUserIdAsync(aliceId))!;
            var bob = (await repo.GetByUserIdAsync(bobId))!;

            var result = await repo.SaveProfileAsync(new ProfileChanges(
                bobId, bob.Username, $" {alice.Username.ToUpperInvariant()} ", null, false));
            Assert.Equal(ProfileSaveStatus.UsernameTaken, result.Status);
        }
        finally
        {
            await Cleanup(connection, aliceId, bobId);
        }
    }

    private static CloneEbayDbContext CreateDb(string connection) => new(
        new DbContextOptionsBuilder<CloneEbayDbContext>()
            .UseSqlServer(connection)
            .Options);

    private static async Task<(int aliceId, int bobId)> SeedTwoBuyers(string connection)
    {
        await using var db = CreateDb(connection);
        string nonce = Guid.NewGuid().ToString("N")[..12];
        DateTime now = DateTime.UtcNow;
        var alice = NewBuyer("a" + nonce, now);
        var bob = NewBuyer("b" + nonce, now);
        db.Users.AddRange(alice, bob);
        await db.SaveChangesAsync();
        return (alice.id, bob.id);
    }

    private static User NewBuyer(string username, DateTime now) => new()
    {
        username = username,
        email = username + "@example.test",
        role = "BUYER",
        accountStatus = "ACTIVE",
        emailVerified = true,
        createdAt = now,
        updatedAt = now
    };

    private static async Task Cleanup(string connection, int aliceId, int bobId)
    {
        await using var db = CreateDb(connection);
        await db.Users
            .Where(user => user.id == aliceId || user.id == bobId)
            .ExecuteDeleteAsync();
    }

    private sealed class MutableClock(DateTime now) : IClock
    {
        public DateTime UtcNow { get; set; } = now;
    }
}
