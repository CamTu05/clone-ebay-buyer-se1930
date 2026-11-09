using Buyer.Application.Interfaces.Authentication;
using Buyer.Infrastructure.Data;
using Buyer.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Buyer.Infrastructure.Repositories.Authentication;

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
                 user.emailVerified))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.email.Trim().ToUpper() == normalizedEmail, cancellationToken);
    }

    public async Task<bool> ExistsByNormalizedUsernameAsync(
        string normalizedUsername,
        CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.username.Trim().ToUpper() == normalizedUsername, cancellationToken);
    }

    public async Task<int> CreateAsync(
        string email,
        string username,
        string passwordHash,
        string role,
        string accountStatus,
        DateTime createdAtUtc,
        CancellationToken cancellationToken)
    {
        var user = new User
        {
            email = email,
            username = username,
            passwordHash = passwordHash,
            role = role,
            accountStatus = accountStatus,
            createdAt = createdAtUtc,
            updatedAt = createdAtUtc,
            emailVerified = false
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return user.id;
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
