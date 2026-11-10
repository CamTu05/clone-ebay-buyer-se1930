namespace Buyer.Application.Common.Authentication;

public enum RegisterFailure
{
    None,
    EmailAlreadyExists,
    UsernameAlreadyExists,
    ValidationFailed
}
