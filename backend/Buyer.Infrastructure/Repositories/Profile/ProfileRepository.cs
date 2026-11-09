using System.Data;
using Buyer.Application.Interfaces.Authentication;
using Buyer.Application.Interfaces.Profile;
using Buyer.Infrastructure.Repositories.Addresses;
using Buyer.Infrastructure.Data.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Buyer.Infrastructure.Repositories.Profile;

public sealed class ProfileRepository(
    CloneEbayDbContext dbContext, IClock clock) : IProfileRepository
{
    private const string UsernameAllocationLockSql = """
        DECLARE @result int;
        EXEC @result = sys.sp_getapplock
            @Resource = N'Buyer.UsernameAllocation.v1',
            @LockMode = N'Exclusive',
            @LockOwner = N'Transaction',
            @LockTimeout = 10000;
        IF @result < 0
            THROW 51001, 'Username allocation lock unavailable', 1;
        """;

    public Task<ProfileUserSnapshot?> GetByUserIdAsync(
        int userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return Task.FromResult<ProfileUserSnapshot?>(null);

        return dbContext.Users.AsNoTracking()
            .Where(user => user.id == userId)
            .Select(user => new ProfileUserSnapshot(
                user.id, user.username, user.email, user.phoneNumber,
                user.avatarURL, user.emailVerified,
                user.role, user.accountStatus))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> IsUsernameTakenAsync(
        string username, int excludeUserId,
        CancellationToken cancellationToken = default) =>
        string.IsNullOrWhiteSpace(username)
            ? Task.FromResult(true)
            : UsernameUnavailableAsync(username, excludeUserId, cancellationToken);

    public async Task<ProfileSaveResult> SaveProfileAsync(
        ProfileChanges changes, CancellationToken cancellationToken = default)
    {
        if (changes.UserId <= 0)
            return new(ProfileSaveStatus.UserNotFound);

        try
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            if (changes.NewUsername is not null)
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    UsernameAllocationLockSql, cancellationToken);
            }

            var user = await dbContext.Users.FromSqlInterpolated($"""
                    SELECT * FROM dbo.[User] WITH (UPDLOCK, HOLDLOCK)
                    WHERE [id] = {changes.UserId}
                    """)
                .SingleOrDefaultAsync(cancellationToken);

            if (user is null) return new(ProfileSaveStatus.UserNotFound);
            if (user.role != "BUYER" || user.accountStatus != "ACTIVE")
                return new(ProfileSaveStatus.AccountUnavailable);

            if (!string.Equals(user.username, changes.ExpectedUsername,
                    StringComparison.Ordinal))
                return new(ProfileSaveStatus.ConcurrentConflict);

            var nowUtc = clock.UtcNow;
            bool changeUsername = changes.NewUsername is not null &&
                !string.Equals(user.username, changes.NewUsername,
                    StringComparison.OrdinalIgnoreCase);
            bool changeAvatar = changes.ClearAvatar
                ? user.avatarURL is not null
                : changes.NewAvatarUrl is not null &&
                  !string.Equals(user.avatarURL, changes.NewAvatarUrl,
                      StringComparison.Ordinal);

            if (changeUsername)
            {
                var newUsername = changes.NewUsername!;
                var validate = UpdateProfileValidator.Validate(
                    new() { Username = newUsername });
                if (validate.Count != 0)
                    return new(ProfileSaveStatus.ConcurrentConflict);

                if (await UsernameUnavailableAsync(
                        newUsername, user.id, cancellationToken))
                    return new(ProfileSaveStatus.UsernameTaken);

                user.username = newUsername.Trim();
            }

            if (changes.ClearAvatar)
                user.avatarURL = null;
            else if (changes.NewAvatarUrl is not null)
                user.avatarURL = changes.NewAvatarUrl;

            if (changeUsername || changeAvatar)
            {
                user.updatedAt = nowUtc;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return new(ProfileSaveStatus.Updated, ToSnapshot(user));
        }
        catch (DbUpdateException exception) when
            (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return new(ProfileSaveStatus.UsernameTaken);
        }
        catch (SqlException exception) when
            (exception.Number is 1205 or 1222 or 51001)
        {
            return new(ProfileSaveStatus.ConcurrentConflict);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(ProfileSaveStatus.ConcurrentConflict);
        }
    }

    private Task<bool> UsernameUnavailableAsync(
        string username, int excludeUserId, CancellationToken cancellationToken)
    {
        string normalizedName = username.Trim().ToUpper();
        return dbContext.Users.AsNoTracking().AnyAsync(
            user => user.id != excludeUserId &&
                    user.username.Trim().ToUpper() == normalizedName,
            cancellationToken);
    }

    private static ProfileUserSnapshot ToSnapshot(User user) => new(
        user.id, user.username, user.email, user.phoneNumber,
        user.avatarURL, user.emailVerified,
        user.role, user.accountStatus);
}
