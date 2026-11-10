using Buyer.Infrastructure.BuyerSchema;
using Microsoft.EntityFrameworkCore;
using System.Data;
namespace Buyer.API;
public sealed class BuyerLifecycleWorker(IServiceScopeFactory scopes, ILogger<BuyerLifecycleWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct) { using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30)); while (await timer.WaitForNextTickAsync(ct)) { try { await Tick(ct); } catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; } catch (Exception e) { logger.LogError(e, "Buyer lifecycle update failed; will retry"); } } }
    async Task Tick(CancellationToken ct)
    {
        using var scope = scopes.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<BuyerSchemaContext>();
        var expired = await db.CheckoutGroups.AsNoTracking().Where(x => x.status == "PENDING" && x.buyerId != null && x.paymentDeadline < DateTime.UtcNow).Select(x => new { x.id, x.buyerId }).Take(100).ToListAsync(ct);
        foreach (var g in expired) { using var s = scopes.CreateScope(); try { await s.ServiceProvider.GetRequiredService<BuyerWorkflowService>().ExpirePendingCheckout(g.buyerId!.Value, g.id, ct); } catch (Buyer.Application.BuyerRuleException) { } }
        var ended = await db.Auctions.AsNoTracking().Where(x => x.status == "ACTIVE" && x.endTime <= DateTime.UtcNow).Select(x => x.id).Take(100).ToListAsync(ct);
        foreach (var id in ended) { using var s = scopes.CreateScope(); var context = s.ServiceProvider.GetRequiredService<BuyerSchemaContext>(); await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct); var a = await context.Auctions.Include(x => x.listing).Include(x => x.Bids).SingleAsync(x => x.id == id, ct); if (a.status != "ACTIVE") continue; a.status = "ENDED"; a.resolvedAt = DateTime.UtcNow; a.winnerId = a.highestBidderId; a.winningPrice = a.winnerId != null ? a.currentPrice : null; a.listing.status = a.winnerId != null ? "SOLD" : "ENDED"; a.listing.endedAt = DateTime.UtcNow; foreach (var b in a.Bids.Where(x => x.status == "WINNING")) b.status = "WON"; await context.SaveChangesAsync(ct); await tx.CommitAsync(ct); }
    }
}
