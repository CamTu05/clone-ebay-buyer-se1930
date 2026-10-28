using Buyer.Application.Authentication;
using Buyer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Buyer.Infrastructure.Authentication;

public sealed class UserRepository(CloneEbayDbContext dbContext) : IUserRepository
{
    public async Task<AuthUser?> FindByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Where(user => user.email.Trim().ToUpper() == normalizedEmail)
            .Select(user => new AuthUser(
                user.id,
                user.email,
                user.username,
                user.passwordHash,
                user.role,
                user.accountStatus,
                user.emailVerified,
                user.phoneVerified))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task RecordSuccessfulLoginAsync(
        int userId,
        DateTime lastLoginAtUtc,
        string? upgradedPasswordHash,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleAsync(user => user.id == userId, cancellationToken);
        user.lastLoginAt = lastLoginAtUtc;
        if (upgradedPasswordHash is not null)
        {
            user.passwordHash = upgradedPasswordHash;
            user.updatedAt = lastLoginAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
