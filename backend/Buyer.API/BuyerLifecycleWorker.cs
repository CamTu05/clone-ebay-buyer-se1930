using Buyer.Infrastructure.BuyerSchema;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Buyer.API;

public sealed class BuyerLifecycleWorker(
    IServiceScopeFactory scopes,
    ILogger<BuyerLifecycleWorker> logger,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var interval = Math.Clamp(configuration.GetValue("BuyerLifecycle:IntervalSeconds", 30), 5, 300);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(interval));
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                try { await Tick(ct); }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
                catch (Exception error) { logger.LogError(error, "Buyer lifecycle update failed; will retry"); }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
    }

    private async Task Tick(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BuyerSchemaContext>();
        var expired = await db.CheckoutGroups.AsNoTracking()
            .Where(x => x.status == "PENDING" && x.buyerId != null && x.paymentDeadline < DateTime.UtcNow)
            .OrderBy(x => x.paymentDeadline)
            .Select(x => new { x.id, x.buyerId }).Take(100).ToListAsync(ct);
        foreach (var group in expired)
        {
            using var itemScope = scopes.CreateScope();
            try
            {
                await itemScope.ServiceProvider.GetRequiredService<BuyerWorkflowService>()
                    .ExpirePendingCheckout(group.buyerId!.Value, group.id, ct);
            }
            catch (Buyer.Application.BuyerRuleException)
            {
                // A payment or cancellation may have completed since the initial query.
            }
        }

        var ended = await db.Auctions.AsNoTracking()
            .Where(x => x.status == "ACTIVE" && x.endTime <= DateTime.UtcNow)
            .OrderBy(x => x.endTime).Select(x => x.id).Take(100).ToListAsync(ct);
        foreach (var id in ended)
        {
            using var itemScope = scopes.CreateScope();
            var context = itemScope.ServiceProvider.GetRequiredService<BuyerSchemaContext>();
            await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var auction = await context.Auctions.Include(x => x.listing).Include(x => x.Bids)
                .SingleAsync(x => x.id == id, ct);
            if (auction.status != "ACTIVE") continue;
            auction.status = "ENDED";
            auction.resolvedAt = DateTime.UtcNow;
            auction.winnerId = auction.highestBidderId;
            auction.winningPrice = auction.winnerId != null ? auction.currentPrice : null;
            auction.listing.status = auction.winnerId != null ? "SOLD" : "ENDED";
            auction.listing.endedAt = DateTime.UtcNow;
            foreach (var bid in auction.Bids.Where(x => x.status == "WINNING")) bid.status = "WON";
            await context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
    }
}
