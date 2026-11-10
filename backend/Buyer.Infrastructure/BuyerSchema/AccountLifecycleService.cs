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
    static BuyerRuleException Invalid() => new("TOKEN_INVALID", "This link is invalid, expired or already used.", 400);
    static void Password(string value) { if (value.Length < 10 || value.Length > 128 || !value.Any(char.IsLetter) || !value.Any(char.IsDigit)) throw new BuyerRuleException("PASSWORD_INVALID", "Use 10–128 characters including letters and numbers.", 400); }
    async Task Issue(User user, string type, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var previous = await db.VerificationTokens.Where(x => x.userId == user.id && x.tokenType == type && x.usedAt == null).ToListAsync(ct);
        foreach (var t in previous) t.usedAt = now;
        var raw = NewToken(); db.VerificationTokens.Add(new VerificationToken { user = user, tokenType = type, tokenHash = HashToken(raw), createdAt = now, expiresAt = now.AddMinutes(type == "EMAIL_VERIFY" ? 60 : 20) });
        var path = type == "EMAIL_VERIFY" ? "verify-email" : "reset-password";
        var origin = config["Frontend:PublicUrl"] ?? "http://127.0.0.1:5173";
        db.EmailOutboxes.Add(new EmailOutbox { recipient = user.email, subject = type == "EMAIL_VERIFY" ? "Verify your buyer email" : "Reset your buyer password", body = $"Open {origin.TrimEnd('/')}/{path}?token={raw}" });
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
    public async Task Consume(string token, string? password, CancellationToken ct)
    {
        if (token.Length != 64) throw Invalid(); if (password != null) Password(password);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var hash = HashToken(token); var type = password == null ? "EMAIL_VERIFY" : "PASSWORD_RESET";
        var row = await db.VerificationTokens.Include(x => x.user).SingleOrDefaultAsync(x => x.tokenHash == hash && x.tokenType == type, ct);
        if (row == null || row.usedAt != null || row.expiresAt <= DateTime.UtcNow || row.user.accountStatus != "ACTIVE") throw Invalid();
        row.usedAt = DateTime.UtcNow;
        if (password == null) row.user.emailVerified = true;
        else { row.user.passwordHash = new PasswordHasher<User>().HashPassword(row.user, password); row.user.authVersion++; }
        row.user.updatedAt = DateTime.UtcNow; await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
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
