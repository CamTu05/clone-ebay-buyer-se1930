using Buyer.Application.DTOs.Profile;
using Buyer.Application.Interfaces.Profile;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Buyer.Infrastructure.BuyerSchema;
public sealed class ProfileRepository(BuyerSchemaContext db) : IProfileRepository
{
    public async Task<ProfileUserSnapshot?> GetByUserIdAsync(int id, CancellationToken ct = default) =>
        await db.Users.AsNoTracking().Where(u => u.id == id).Select(u => new ProfileUserSnapshot(u.id, u.username, u.email, u.phoneNumber, u.avatarURL, u.emailVerified, u.role, u.accountStatus, u.usernameChangedAt, false)).SingleOrDefaultAsync(ct);
    public async Task<BuyerFeedbackRepositoryPage> GetFeedbackAsync(int buyerId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.BuyerFeedbacks.AsNoTracking().Where(feedback => feedback.buyerId == buyerId);
        var totalItems = await query.CountAsync(ct);
        var skip = ((long)page - 1) * pageSize;
        var items = skip >= totalItems
            ? []
            : await query.OrderByDescending(feedback => feedback.createdAt).ThenByDescending(feedback => feedback.id)
                .Skip((int)skip).Take(pageSize)
                .Select(feedback => new BuyerFeedbackItemResponse(
                    feedback.id,
                    feedback.orderId,
                    new BuyerFeedbackSellerResponse(feedback.seller.id, feedback.seller.username, feedback.seller.avatarURL),
                    feedback.order.OrderItems.OrderBy(item => item.id)
                        .Select(item => new BuyerFeedbackPurchaseItemResponse(item.id, item.listingId, item.productTitle, item.imageUrl, item.unitPrice)).ToList(),
                    feedback.order.currency,
                    feedback.order.buyerId == feedback.buyerId,
                    feedback.comment,
                    feedback.createdAt))
                .ToListAsync(ct);
        return new(totalItems, items);
    }

    public Task<bool> IsUsernameTakenAsync(string name, int excludeUserId, CancellationToken ct = default) => db.Users.AnyAsync(u => u.id != excludeUserId && u.username.ToUpper() == name.ToUpper(), ct);
    public async Task<ProfileSaveResult> SaveProfileAsync(ProfileChanges changes, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var u = await db.Users.SingleOrDefaultAsync(u => u.id == changes.UserId, ct);
        if (u is null) return new(ProfileSaveStatus.UserNotFound);
        if (u.role != "BUYER" || u.accountStatus != "ACTIVE") return new(ProfileSaveStatus.AccountUnavailable);
        if (u.username != changes.ExpectedUsername) return new(ProfileSaveStatus.ConcurrentConflict);
        if (changes.NewUsername is not null && await IsUsernameTakenAsync(changes.NewUsername, u.id, ct)) return new(ProfileSaveStatus.UsernameTaken);

        if (changes.NewUsername is not null && changes.NewUsername != u.username)
        {
            if (u.usernameChangedAt > DateTime.UtcNow.AddDays(-30)) return new(ProfileSaveStatus.UsernameChangeRestricted);
            u.username = changes.NewUsername; u.usernameChangedAt = DateTime.UtcNow;
        }
        if (changes.ClearAvatar) u.avatarURL = null; else if (changes.NewAvatarUrl is not null) u.avatarURL = changes.NewAvatarUrl;
        u.updatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return new(ProfileSaveStatus.Updated, await GetByUserIdAsync(u.id, ct));
    }
}
