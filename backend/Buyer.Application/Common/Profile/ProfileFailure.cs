using Buyer.Application.DTOs.Profile;

namespace Buyer.Application.Common.Profile;

public enum ProfileFailure
{
    None,
    UserNotFound,
    AccountUnavailable,
    ValidationFailed,
    UsernameTaken,
    UsernameChangeRestricted,
    ConcurrentConflict
}
