namespace Buyer.Application.DTOs.Email;

public sealed record EmailVerificationResult(bool IsSuccess, string Message, string? ErrorCode = null)
{
    public static EmailVerificationResult Succeeded(string message) => new(true, message);

    public static EmailVerificationResult Failed(string errorCode, string message) => new(false, message, errorCode);
}
