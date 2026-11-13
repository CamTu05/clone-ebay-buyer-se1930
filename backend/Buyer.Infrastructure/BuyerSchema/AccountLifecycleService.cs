using System.Data;
using System.Security.Cryptography;
using System.Text;
using Buyer.Application;
using Buyer.Infrastructure.BuyerSchema.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Buyer.Infrastructure.BuyerSchema;

public sealed class AccountLifecycleService(BuyerSchemaContext db, Microsoft.Extensions.Configuration.IConfiguration config)
{
    public static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    public static string NewToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    static BuyerRuleException Invalid() => new("TOKEN_INVALID", "This code or link is invalid, expired or already used.", 400);
    static void Password(string value) { if (value.Length < 10 || value.Length > 128 || !value.Any(char.IsLetter) || !value.Any(char.IsDigit)) throw new BuyerRuleException("PASSWORD_INVALID", "Use 10–128 characters including letters and numbers.", 400); }

    async Task Issue(User user, string type, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var previous = await db.VerificationTokens.Where(x => x.userId == user.id && x.tokenType == type && x.usedAt == null).ToListAsync(ct);
        foreach (var t in previous) t.usedAt = now;

        string raw;
        string subject;
        string body;

        if (type == "EMAIL_VERIFY")
        {
            // 6-digit OTP code for email verification
            raw = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            subject = "eBay Clone - Mã xác thực email của bạn";
            body = $@"
<div style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; max-width: 520px; margin: 0 auto; padding: 24px; border: 1px solid #e5e5e5; border-radius: 12px; background: #ffffff;"">
    <div style=""text-align: center; margin-bottom: 24px;"">
        <span style=""font-size: 32px; font-weight: 800; letter-spacing: -1px;"">
            <span style=""color: #e53238;"">e</span><span style=""color: #0064d2;"">b</span><span style=""color: #f5af02;"">a</span><span style=""color: #86b817;"">y</span>
        </span>
    </div>
    <h2 style=""color: #191919; font-size: 20px; font-weight: 700; margin-bottom: 12px; text-align: center;"">Xác thực địa chỉ Email của bạn</h2>
    <p style=""color: #555; font-size: 14px; line-height: 1.5; margin-bottom: 20px;"">
        Xin chào <strong>{user.username}</strong>,<br>
        Mã xác thực tài khoản Buyer tại <strong>eBay Clone</strong> của bạn là:
    </p>
    <div style=""background: #f4f7fa; border-radius: 8px; padding: 20px; text-align: center; margin-bottom: 20px;"">
        <p style=""font-size: 12px; color: #777; margin: 0 0 8px 0; text-transform: uppercase; letter-spacing: 1px;"">Mã xác thực của bạn (OTP)</p>
        <div style=""font-size: 36px; font-weight: 800; letter-spacing: 8px; color: #0064d2;"">{raw}</div>
        <p style=""font-size: 12px; color: #999; margin: 8px 0 0 0;"">Mã có hiệu lực trong vòng 15 phút.</p>
    </div>
    <p style=""color: #777; font-size: 13px; line-height: 1.4;"">
        Vui lòng nhập mã gồm 6 chữ số này trên trang đăng ký để hoàn tất kích hoạt tài khoản. Tuyệt đối không chia sẻ mã này với bất kỳ ai.
    </p>
    <hr style=""border: none; border-top: 1px solid #eee; margin: 24px 0;"" />
    <p style=""text-align: center; color: #aaa; font-size: 11px; margin: 0;"">
        © 2026 eBay Clone. PRN232 Project.
    </p>
</div>";
        }
        else
        {
            raw = NewToken();
            var origin = config["Frontend:PublicUrl"] ?? "http://127.0.0.1:5173";
            subject = "Reset your buyer password";
            body = $"Open {origin.TrimEnd('/')}/reset-password?token={raw}";
        }

        db.VerificationTokens.Add(new VerificationToken
        {
            user = user,
            tokenType = type,
            tokenHash = type == "EMAIL_VERIFY" ? raw : HashToken(raw),
            createdAt = now,
            expiresAt = now.AddMinutes(type == "EMAIL_VERIFY" ? 15 : 20)
        });

        db.EmailOutboxes.Add(new EmailOutbox
        {
            recipient = user.email,
            subject = subject,
            body = body
        });
    }

    public async Task Register(string email, string username, string password, CancellationToken ct)
    {
        Password(password); email = email.Trim().ToLowerInvariant(); username = username.Trim();
        if (username.Length < 3 || username.Length > 100) throw new BuyerRuleException("USERNAME_INVALID", "Username must contain 3–100 characters.", 400);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (await db.Users.AnyAsync(x => x.email == email || x.username == username, ct)) throw new BuyerRuleException("ACCOUNT_EXISTS", "Email or username is already registered.", 409);
        var u = new User { email = email, username = username, role = "BUYER", accountStatus = "ACTIVE", emailVerified = false };
        u.passwordHash = new PasswordHasher<User>().HashPassword(u, password); db.Users.Add(u); await db.SaveChangesAsync(ct);
        await Issue(u, "EMAIL_VERIFY", ct); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }

    public async Task Request(string email, bool reset, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var user = await db.Users.SingleOrDefaultAsync(x => x.email == email.Trim() && x.role == "BUYER" && x.accountStatus == "ACTIVE", ct);
        if (user != null && (reset || !user.emailVerified)) { await Issue(user, reset ? "PASSWORD_RESET" : "EMAIL_VERIFY", ct); await db.SaveChangesAsync(ct); }
        await tx.CommitAsync(ct);
    }

    public async Task Consume(string token, string? email, string? password, CancellationToken ct)
    {
        token = token?.Trim() ?? string.Empty;
        if (password != null) Password(password);
        if (string.IsNullOrEmpty(token)) throw Invalid();

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var type = password == null ? "EMAIL_VERIFY" : "PASSWORD_RESET";

        VerificationToken? row = null;
        if (type == "EMAIL_VERIFY")
        {
            var query = db.VerificationTokens.Include(x => x.user)
                .Where(x => x.tokenType == "EMAIL_VERIFY" && x.usedAt == null);

            if (!string.IsNullOrWhiteSpace(email))
            {
                var cleanEmail = email.Trim().ToLowerInvariant();
                query = query.Where(x => x.user.email == cleanEmail);
            }

            var candidates = await query.OrderByDescending(x => x.id).ToListAsync(ct);
            var hash = token.Length == 64 ? HashToken(token) : null;
            row = candidates.FirstOrDefault(x => x.tokenHash == token || (hash != null && x.tokenHash == hash));
        }
        else
        {
            if (token.Length != 64) throw Invalid();
            var hash = HashToken(token);
            row = await db.VerificationTokens.Include(x => x.user)
                .SingleOrDefaultAsync(x => x.tokenHash == hash && x.tokenType == type, ct);
        }

        if (row == null || row.usedAt != null || row.user.accountStatus != "ACTIVE")
            throw new BuyerRuleException("TOKEN_INVALID", "Mã xác thực không hợp lệ hoặc đã được sử dụng.", 400);

        if (row.expiresAt <= DateTime.UtcNow)
            throw new BuyerRuleException("TOKEN_EXPIRED", "Mã xác thực đã hết hạn. Vui lòng yêu cầu mã mới.", 400);

        row.usedAt = DateTime.UtcNow;
        if (password == null)
        {
            row.user.emailVerified = true;
        }
        else
        {
            row.user.passwordHash = new PasswordHasher<User>().HashPassword(row.user, password);
            row.user.authVersion++;
        }
        row.user.updatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task Change(int uid, string current, string password, CancellationToken ct)
    {
        Password(password); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var user = await db.Users.SingleAsync(x => x.id == uid, ct);
        if (user.passwordHash == null || new PasswordHasher<User>().VerifyHashedPassword(user, user.passwordHash, current) == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed) throw new BuyerRuleException("PASSWORD_INVALID", "Current password is incorrect.", 400);
        user.passwordHash = new PasswordHasher<User>().HashPassword(user, password); user.authVersion++; user.updatedAt = DateTime.UtcNow;
        foreach (var token in await db.VerificationTokens.Where(x => x.userId == uid && x.usedAt == null && x.tokenType == "PASSWORD_RESET").ToListAsync(ct)) token.usedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
}
