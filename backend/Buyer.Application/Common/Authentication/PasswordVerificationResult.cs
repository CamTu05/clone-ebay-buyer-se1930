namespace Buyer.Application.Common.Authentication;

public enum PasswordVerificationResult
{
    Failed,
    Success,
    SuccessRehashNeeded
}
