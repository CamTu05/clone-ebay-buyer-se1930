using Buyer.Application.DTOs.Email;
using Buyer.Application.Interfaces.Email;
using Buyer.Infrastructure.Data;
using Buyer.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Buyer.Infrastructure.Email;

public sealed class EmailVerificationService(
    CloneEbayDbContext dbContext,
    IEmailSender emailSender,
    ILogger<EmailVerificationService> logger) : IEmailVerificationService
{
    public async Task<EmailVerificationResult> SendVerificationCodeAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.id == userId, cancellationToken);
        if (user is null)
        {
            return EmailVerificationResult.Failed("USER_NOT_FOUND", "User account not found.");
        }

        // Generate 6-digit random code
        var code = Random.Shared.Next(100000, 999999).ToString();
        var now = DateTime.UtcNow;

        // Invalidate previous unused verification tokens
        var existingTokens = await dbContext.VerificationTokens
            .Where(t => t.userId == userId && t.tokenType == "EMAIL_VERIFY" && t.usedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var t in existingTokens)
        {
            t.usedAt = now;
        }

        // Create new verification token (valid for 10 minutes)
        var token = new VerificationToken
        {
            userId = userId,
            tokenType = "EMAIL_VERIFY",
            tokenHash = code,
            createdAt = now,
            expiresAt = now.AddMinutes(10),
            usedAt = null
        };

        dbContext.VerificationTokens.Add(token);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Always log OTP to console for easy debugging/demo
        logger.LogInformation("=================================================");
        logger.LogInformation("🔑 OTP EMAIL VERIFICATION for {Email}: [{Code}]", user.email, code);
        logger.LogInformation("=================================================");

        // Send email via SMTP
        var htmlBody = $@"
<div style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; max-width: 520px; margin: 0 auto; padding: 24px; border: 1px solid #e5e5e5; border-radius: 12px; background: #ffffff;"">
    <div style=""text-align: center; margin-bottom: 24px;"">
        <span style=""font-size: 32px; font-weight: 800; letter-spacing: -1px;"">
            <span style=""color: #e53238;"">e</span><span style=""color: #0064d2;"">b</span><span style=""color: #f5af02;"">a</span><span style=""color: #86b817;"">y</span>
        </span>
    </div>
    <h2 style=""color: #191919; font-size: 20px; font-weight: 700; margin-bottom: 12px; text-align: center;"">Xác thực địa chỉ Email của bạn</h2>
    <p style=""color: #555; font-size: 14px; line-height: 1.5; margin-bottom: 20px;"">
        Xin chào <strong>{user.username}</strong>,<br>
        Bạn vừa yêu cầu mã xác thực email cho tài khoản Buyer tại <strong>eBay Clone</strong>.
    </p>
    <div style=""background: #f4f7fa; border-radius: 8px; padding: 20px; text-align: center; margin-bottom: 20px;"">
        <p style=""font-size: 12px; color: #777; margin: 0 0 8px 0; text-transform: uppercase; letter-spacing: 1px;"">Mã xác thực của bạn (OTP)</p>
        <div style=""font-size: 36px; font-weight: 800; letter-spacing: 8px; color: #0064d2;"">{code}</div>
        <p style=""font-size: 12px; color: #999; margin: 8px 0 0 0;"">Mã có hiệu lực trong vòng 10 phút.</p>
    </div>
    <p style=""color: #777; font-size: 13px; line-height: 1.4;"">
        Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email. Tuyệt đối không chia sẻ mã này với bất kỳ ai.
    </p>
    <hr style=""border: none; border-top: 1px solid #eee; margin: 24px 0;"" />
    <p style=""text-align: center; color: #aaa; font-size: 11px; margin: 0;"">
        © 2026 eBay Clone. PRN232 Project.
    </p>
</div>";

        try
        {
            await emailSender.SendEmailAsync(user.email, "eBay Clone - Mã xác thực email của bạn", htmlBody, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not send email via SMTP, but OTP [{Code}] was generated and logged.", code);
        }

        return EmailVerificationResult.Succeeded("Mã xác thực đã được gửi đến email của bạn.");
    }

    public async Task<EmailVerificationResult> VerifyCodeAsync(int userId, string code, CancellationToken cancellationToken = default)
    {
        var cleanCode = code?.Trim();
        if (string.IsNullOrWhiteSpace(cleanCode) || cleanCode.Length != 6)
        {
            return EmailVerificationResult.Failed("INVALID_CODE", "Mã xác thực phải gồm 6 chữ số.");
        }

        var now = DateTime.UtcNow;

        var token = await dbContext.VerificationTokens
            .Where(t => t.userId == userId && t.tokenType == "EMAIL_VERIFY" && t.usedAt == null)
            .OrderByDescending(t => t.createdAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (token is null)
        {
            return EmailVerificationResult.Failed("CODE_NOT_FOUND", "Không tìm thấy yêu cầu xác thực hoặc mã đã được sử dụng.");
        }

        if (token.expiresAt < now)
        {
            return EmailVerificationResult.Failed("CODE_EXPIRED", "Mã xác thực đã hết hạn. Vui lòng yêu cầu mã mới.");
        }

        if (!string.Equals(token.tokenHash, cleanCode, StringComparison.Ordinal))
        {
            return EmailVerificationResult.Failed("CODE_MISMATCH", "Mã xác thực không chính xác.");
        }

        // Mark token as used
        token.usedAt = now;

        // Update user
        var user = await dbContext.Users.SingleAsync(u => u.id == userId, cancellationToken);
        user.emailVerified = true;
        user.updatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Email verified successfully for user {UserId} ({Email})", userId, user.email);

        return EmailVerificationResult.Succeeded("Xác thực email thành công!");
    }
}
