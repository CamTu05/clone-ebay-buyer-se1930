namespace Buyer.Application.Common.Profile;

public enum ProfileSaveStatus
{
    Updated,
    UserNotFound,
    AccountUnavailable,
    UsernameTaken,
    ConcurrentConflict
}
