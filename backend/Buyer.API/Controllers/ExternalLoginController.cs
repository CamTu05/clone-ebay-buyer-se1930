using System.Data;
using System.Text.Json;
using Buyer.Application;
using Buyer.Application.Interfaces.Authentication;
using Buyer.Infrastructure.BuyerSchema;
using Buyer.Infrastructure.BuyerSchema.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Buyer.API.Controllers;
[ApiController, Route("api/auth")]
public sealed class ExternalLoginController(BuyerSchemaContext db, IAccessTokenGenerator tokens, IConfiguration config) : ControllerBase
{
    bool Enabled => !string.IsNullOrWhiteSpace(config["OAuth:Google:ClientId"]) && !string.IsNullOrWhiteSpace(config["OAuth:Google:ClientSecret"]);
    [HttpGet("providers")]
    public object Providers() => ApiEnvelope<object>.Succeeded(new { google = Enabled });
    [HttpGet("google")]
    public IActionResult Google() => Enabled ? Challenge(new AuthenticationProperties { RedirectUri = "/api/auth/external-complete" }, "Google") : StatusCode(503, ApiEnvelope<object>.Failure("OAUTH_NOT_CONFIGURED", "Google sign-in is not configured."));
    [HttpGet("external-complete")]
    public async Task<IActionResult> Complete(CancellationToken ct)
    {
        if (!Enabled) return NotFound(); var auth = await HttpContext.AuthenticateAsync("External");
        if (!auth.Succeeded) return Unauthorized(); await HttpContext.SignOutAsync("External");
        var subject = auth.Principal?.FindFirst("sub")?.Value; var email = auth.Principal?.FindFirst("email")?.Value?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(email)) return Unauthorized();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var external = await db.ExternalLogins.Include(x => x.user).SingleOrDefaultAsync(x => x.provider == "GOOGLE" && x.providerUserId == subject, ct);
        User user;
        if (external == null)
        {
            if (await db.Users.AnyAsync(x => x.email == email, ct)) throw new BuyerRuleException("LINK_REQUIRED", "This email already has an account. Sign in using your existing password.", 409);
            user = new User { email = email, username = "buyer_" + Guid.NewGuid().ToString("N")[..12], role = "BUYER", accountStatus = "ACTIVE", emailVerified = true };
            db.ExternalLogins.Add(new ExternalLogin { user = user, provider = "GOOGLE", providerUserId = subject }); await db.SaveChangesAsync(ct);
        }
        else user = external.user;
        if (user.role != "BUYER" || user.accountStatus != "ACTIVE" || !user.emailVerified) return Forbid();
        var issued = tokens.Generate(new AuthUser(user.id, user.email, user.username, user.passwordHash, user.role, user.accountStatus, user.emailVerified, false, user.authVersion));
        var response = new LoginResponse(issued.Value, "Bearer", issued.ExpiresAtUtc, new AuthenticatedUser(user.id, user.username, user.email, user.role, true, false));
        var code = AccountLifecycleService.NewToken(); db.OperationReceipts.Add(new OperationReceipt { scope = "external-login", operationKey = AccountLifecycleService.HashToken(code), requestHash = "", responseJson = ReceiptCipher.Protect(JsonSerializer.Serialize(response), config) });
        user.lastLoginAt = DateTime.UtcNow; await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return Redirect((config["Frontend:PublicUrl"] ?? "http://127.0.0.1:5173").TrimEnd('/') + "/external-login#code=" + code);
    }
    public sealed record ExchangeInput(string Code);
    [HttpPost("exchange")]
    public async Task<object> Exchange(ExchangeInput r, CancellationToken ct)
    {
        if (r.Code == null || r.Code.Length != 64) throw new BuyerRuleException("CODE_INVALID", "Invalid sign-in code.", 400);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct); var hash = AccountLifecycleService.HashToken(r.Code);
        var row = await db.OperationReceipts.SingleOrDefaultAsync(x => x.scope == "external-login" && x.operationKey == hash, ct);
        if (row == null || row.createdAt < DateTime.UtcNow.AddMinutes(-1)) throw new BuyerRuleException("CODE_INVALID", "Sign-in code expired or already used.", 400);
        var data = JsonSerializer.Deserialize<LoginResponse>(ReceiptCipher.Unprotect(row.responseJson, config))!; db.OperationReceipts.Remove(row); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return ApiEnvelope<LoginResponse>.Succeeded(data);
    }
}
