namespace Buyer.Application.Common.Profile;

public enum ProfileSaveStatus
{
    Updated,
    UsernameChangeRestricted,
    UserNotFound,
    AccountUnavailable,
    UsernameTaken,
    ConcurrentConflict
}
