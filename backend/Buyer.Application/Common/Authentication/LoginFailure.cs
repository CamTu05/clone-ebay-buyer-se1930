using System.ComponentModel.DataAnnotations;

namespace Buyer.Application.Common.Authentication;

public enum LoginFailure
{
    None,
    InvalidCredentials,
    AccountUnavailable,
    EmailNotVerified
}
