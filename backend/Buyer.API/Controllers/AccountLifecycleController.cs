using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Buyer.Infrastructure.BuyerSchema;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Buyer.API.Controllers;
[ApiController, Route("api/auth"), EnableRateLimiting("Login")]
public sealed class AccountLifecycleController(AccountLifecycleService service, BuyerSchemaContext db, IWebHostEnvironment env) : ControllerBase
{
    public sealed record Registration([Required, EmailAddress, StringLength(255)] string Email, [Required, StringLength(100)] string Username, [Required, StringLength(128, MinimumLength = 10)] string Password);
    public sealed record EmailInput([Required, EmailAddress, StringLength(255)] string Email);
    public sealed record TokenInput([Required, StringLength(64)] string Token);
    public sealed record VerifyEmailInput([StringLength(64)] string? Token, [StringLength(64)] string? Code, [EmailAddress, StringLength(255)] string? Email);
    public sealed record ResetInput([Required, StringLength(64)] string Token, [Required, StringLength(128, MinimumLength = 10)] string Password);
    public sealed record ChangeInput([Required, StringLength(128)] string CurrentPassword, [Required, StringLength(128, MinimumLength = 10)] string Password);
    object OkData() => ApiEnvelope<object>.Succeeded(new { message = "Request processed. Check your email for the next step." });
    [HttpPost("register")]
    public async Task<object> Register(Registration r, CancellationToken ct) { await service.Register(r.Email, r.Username, r.Password, ct); return OkData(); }
    [HttpPost("resend-verification")]
    public async Task<object> Resend(EmailInput r, CancellationToken ct) { await service.Request(r.Email, false, ct); return OkData(); }
    [HttpPost("forgot-password")]
    public async Task<object> Forgot(EmailInput r, CancellationToken ct) { await service.Request(r.Email, true, ct); return OkData(); }
    [HttpPost("verify-email")]
    public async Task<object> Verify(VerifyEmailInput r, CancellationToken ct)
    {
        var code = r.Code ?? r.Token;
        if (string.IsNullOrWhiteSpace(code)) throw new Buyer.Application.BuyerRuleException("TOKEN_INVALID", "Vui lòng nhập mã xác thực gồm 6 chữ số.", 400);
        await service.Consume(code, r.Email, null, ct);
        return ApiEnvelope<object>.Succeeded(new { message = "Email verified successfully." });
    }
    [HttpPost("reset-password")]
    public async Task<object> Reset(ResetInput r, CancellationToken ct) { await service.Consume(r.Token, null, r.Password, ct); return OkData(); }
    [Authorize(Roles = "BUYER"), HttpPost("change-password")]
    public async Task<object> Change(ChangeInput r, CancellationToken ct) { await service.Change(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), r.CurrentPassword, r.Password, ct); return OkData(); }
    [Authorize(Roles = "BUYER"), HttpPost("logout")]
    public async Task<object> Logout(CancellationToken ct) { var uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); await db.Users.Where(x => x.id == uid).ExecuteUpdateAsync(s => s.SetProperty(x => x.authVersion, x => x.authVersion + 1), ct); return OkData(); }
    [HttpGet("dev-mailbox")]
    public async Task<IActionResult> Mailbox(CancellationToken ct)
    {
        if (!env.IsDevelopment() || HttpContext.Connection.RemoteIpAddress is not { } ip || !System.Net.IPAddress.IsLoopback(ip)) return NotFound();
        return Ok(ApiEnvelope<object>.Succeeded(await db.EmailOutboxes.AsNoTracking().OrderByDescending(x => x.id).Take(30).Select(x => new { x.id, x.recipient, x.subject, x.body, x.createdAt, x.sentAt }).ToListAsync(ct)));
    }
}
