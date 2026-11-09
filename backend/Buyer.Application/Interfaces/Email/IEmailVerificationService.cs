namespace Buyer.Application.Interfaces.Email;

public interface IEmailVerificationService
{
    Task<EmailVerificationResult> SendVerificationCodeAsync(int userId, CancellationToken cancellationToken = default);
    Task<EmailVerificationResult> VerifyCodeAsync(int userId, string code, CancellationToken cancellationToken = default);
}
