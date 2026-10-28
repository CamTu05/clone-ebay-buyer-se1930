using Buyer.Application.Authentication;
using Buyer.Application.Features.Profile;
using Buyer.Infrastructure.Data;
using Buyer.Infrastructure.Data.Models;
using Buyer.Infrastructure.Profiles;
using Microsoft.EntityFrameworkCore;

namespace Buyer.IntegrationTests;

// Opt-in SQL Server tests. Set PROFILE_TEST_SQL_CONNECTION to a DEDICATED
// database already created from schema V2 and migrated through
// AddUsernameChangeTracking. Never point at the shared/project DB.
public sealed class ProfileRepositorySqlTests
{
    [Fact]
    public async Task SqlEnforcesCooldownReservationAndExactBoundary()
    {
        string? connection = Environment.GetEnvironmentVariable(
            "PROFILE_TEST_SQL_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection)) return; // opt-in

        var clock = new MutableClock(new DateTime(2026, 10, 27, 10, 0, 0,
            DateTimeKind.Utc));
        (int aliceId, int bobId) = await SeedTwoBuyers(connection);
        try
        {
            await using var ctx = CreateDb(connection);
            var repo = new ProfileRepository(ctx, clock);
            var before = await repo.GetByUserIdAsync(aliceId);
            Assert.NotNull(before);
            string newName1 = $"n{aliceId}xyz001";
            string newName2 = $"n{aliceId}xyz002";
            var first = await repo.SaveProfileAsync(new ProfileChanges(
                aliceId, before!.Username, newName1, null, false));
            Assert.Equal(ProfileSaveStatus.Updated, first.Status);

            var cooldown = await repo.SaveProfileAsync(new ProfileChanges(
                aliceId, newName1, newName2, null, false));
            Assert.Equal(ProfileSaveStatus.UsernameChangeRestricted, cooldown.Status);

            var bob = await repo.GetByUserIdAsync(bobId);
            var reserved = await repo.SaveProfileAsync(new ProfileChanges(
                bobId, bob!.Username, before.Username, null, false));
            Assert.Equal(ProfileSaveStatus.UsernameTaken, reserved.Status);

            clock.UtcNow = clock.UtcNow.AddDays(30);
            var atBoundary = await repo.SaveProfileAsync(new ProfileChanges(
                aliceId, newName1, newName2, null, false));
            Assert.Equal(ProfileSaveStatus.Updated, atBoundary.Status);

            var history = await ctx.UsernameChangeHistories.AsNoTracking()
                .Where(x => x.userId == aliceId).ToListAsync();
            Assert.Equal(2, history.Count);
            Assert.All(history, x => Assert.Equal(
                x.changedAtUtc.AddDays(30), x.oldUsernameReservedUntilUtc));
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
        if (string.IsNullOrWhiteSpace(connection)) return; // opt-in

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

            var firstTask = firstRepository.SaveProfileAsync(new ProfileChanges(
                aliceId, current!.Username, $"c{aliceId}xyz001", null, false));
            var secondTask = secondRepository.SaveProfileAsync(new ProfileChanges(
                aliceId, current.Username, $"c{aliceId}xyz002", null, false));
            var outcomes = await Task.WhenAll(firstTask, secondTask);
            Assert.Single(outcomes, r => r.Status == ProfileSaveStatus.Updated);
            Assert.Single(outcomes, r => r.Status != ProfileSaveStatus.Updated);

            await using var verifyDb = CreateDb(connection);
            Assert.Equal(1, await verifyDb.UsernameChangeHistories.CountAsync(
                x => x.userId == aliceId));
        }
        finally
        {
            await Cleanup(connection, aliceId, bobId);
        }
    }

    [Fact]
    public async Task SqlRejectsActiveUsernameAndAllowsAvatarDuringCooldown()
    {
        string? connection = Environment.GetEnvironmentVariable(
            "PROFILE_TEST_SQL_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection)) return; // opt-in

        var clock = new MutableClock(DateTime.UtcNow);
        (int aliceId, int bobId) = await SeedTwoBuyers(connection);
        try
        {
            await using var db = CreateDb(connection);
            var repo = new ProfileRepository(db, clock);
            var alice = (await repo.GetByUserIdAsync(aliceId))!;
            var bob = (await repo.GetByUserIdAsync(bobId))!;

            var activeName = await repo.SaveProfileAsync(new ProfileChanges(
                bobId, bob.Username, alice.Username.ToUpperInvariant(), null, false));
            Assert.Equal(ProfileSaveStatus.UsernameTaken, activeName.Status);

            var renamed = await repo.SaveProfileAsync(new ProfileChanges(
                aliceId, alice.Username, $"z{aliceId}newname", null, false));
            Assert.Equal(ProfileSaveStatus.Updated, renamed.Status);

            var avatarOnly = await repo.SaveProfileAsync(new ProfileChanges(
                aliceId, renamed.UpdatedProfile!.Username, null,
                "https://example.com/avatar.png", false));
            Assert.Equal(ProfileSaveStatus.Updated, avatarOnly.Status);
            Assert.Equal("https://example.com/avatar.png",
                avatarOnly.UpdatedProfile!.AvatarUrl);
            Assert.Equal(1, await db.UsernameChangeHistories.CountAsync(
                x => x.userId == aliceId));
        }
        finally
        {
            await Cleanup(connection, aliceId, bobId);
        }
    }

    [Fact]
    public async Task SqlRollsBackUsernameWhenAnotherPendingInsertFails()
    {
        string? connection = Environment.GetEnvironmentVariable(
            "PROFILE_TEST_SQL_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection)) return; // opt-in

        var clock = new MutableClock(DateTime.UtcNow);
        (int aliceId, int bobId) = await SeedTwoBuyers(connection);
        try
        {
            await using (var db = CreateDb(connection))
            {
                var repo = new ProfileRepository(db, clock);
                var alice = (await repo.GetByUserIdAsync(aliceId))!;
                // Intentionally violate the FK inside the same SaveChanges.
                db.UsernameChangeHistories.Add(new UsernameChangeHistory
                {
                    userId = -999999,
                    oldUsername = "test_invalid_old",
                    newUsername = "test_invalid_new",
                    changedAtUtc = clock.UtcNow,
                    oldUsernameReservedUntilUtc = clock.UtcNow.AddDays(30)
                });
                await Assert.ThrowsAnyAsync<Exception>(() =>
                    repo.SaveProfileAsync(new ProfileChanges(
                        aliceId, alice.Username, $"r{aliceId}newname", null, false)));
            }

            await using var verify = CreateDb(connection);
            Assert.Equal(0, await verify.UsernameChangeHistories.CountAsync(
                x => x.userId == aliceId));
            Assert.Null((await verify.Users.AsNoTracking()
                .SingleAsync(x => x.id == aliceId)).lastUsernameChangedAt);
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
        phoneVerified = false,
        createdAt = now,
        updatedAt = now
    };

    private static async Task Cleanup(string connection, int aliceId, int bobId)
    {
        await using var db = CreateDb(connection);
        await db.UsernameChangeHistories
            .Where(x => x.userId == aliceId || x.userId == bobId)
            .ExecuteDeleteAsync();
        await db.Users
            .Where(x => x.id == aliceId || x.id == bobId)
            .ExecuteDeleteAsync();
    }

    private sealed class MutableClock(DateTime now) : IClock
    {
        public DateTime UtcNow { get; set; } = now;
    }
}
