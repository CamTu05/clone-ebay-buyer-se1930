namespace Buyer.Application.Interfaces.Profile;

public interface IProfileRepository
{
    Task<ProfileUserSnapshot?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<BuyerFeedbackRepositoryPage> GetFeedbackAsync(int buyerId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> IsUsernameTakenAsync(string username, int excludeUserId, CancellationToken cancellationToken = default);
    Task<ProfileSaveResult> SaveProfileAsync(ProfileChanges changes, CancellationToken cancellationToken = default);
}
