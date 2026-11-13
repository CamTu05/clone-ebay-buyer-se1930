using Buyer.Infrastructure.BuyerSchema;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
using System.Data;
namespace Buyer.API;
public sealed class EmailOutboxWorker(IServiceScopeFactory scopes, IConfiguration config, ILogger<EmailOutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                if (string.IsNullOrWhiteSpace(config["Smtp:Host"])) continue;
                try
                {
                    using var scope = scopes.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<BuyerSchemaContext>();
                    await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
                    var row = await db.EmailOutboxes.FromSqlRaw("SELECT TOP(1) * FROM dbo.EmailOutbox WITH(UPDLOCK,READPAST,ROWLOCK) WHERE sentAt IS NULL AND attempts < 5 ORDER BY id").FirstOrDefaultAsync(ct);
                    if (row == null) continue;
                    row.attempts++;
                    using var client = new SmtpClient(config["Smtp:Host"], config.GetValue("Smtp:Port", 587)) { EnableSsl = config.GetValue("Smtp:EnableSsl", true) };
                    if (config["Smtp:Username"] is { Length: > 0 } username) client.Credentials = new NetworkCredential(username, config["Smtp:Password"]);
                    using var message = new MailMessage(config["Smtp:From"] ?? "buyer@example.test", row.recipient, row.subject, row.body)
                    {
                        IsBodyHtml = row.body.TrimStart().StartsWith('<')
                    };
                    try { await client.SendMailAsync(message, ct); row.sentAt = DateTime.UtcNow; } catch (Exception e) when (e is SmtpException or InvalidOperationException) { logger.LogWarning("Email attempt failed for outbox message {Id}", row.id); }
                    await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
                catch (Exception e) { logger.LogError(e, "Outbox processing failed"); }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
    }
}
