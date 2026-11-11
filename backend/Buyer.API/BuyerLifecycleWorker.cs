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
            .Where(x => x.status == "PENDING" && x.paymentDeadline < DateTime.UtcNow)
            .OrderBy(x => x.paymentDeadline)
            .Select(x => new { x.id, x.buyerId }).Take(100).ToListAsync(ct);
        foreach (var group in expired)
        {
            using var itemScope = scopes.CreateScope();
            try
            {
                await itemScope.ServiceProvider.GetRequiredService<BuyerWorkflowService>()
                    .ExpirePendingCheckout(group.buyerId ?? 0, group.id, ct);
            }
            catch (Buyer.Application.BuyerRuleException)
            {
                // A payment or cancellation may have completed since the initial query.
            }
        }

        await db.Auctions.Where(x => x.status == "SCHEDULED" && x.startTime <= DateTime.UtcNow && x.endTime > DateTime.UtcNow && x.listing.status == "ACTIVE").ExecuteUpdateAsync(s => s.SetProperty(x => x.status, "ACTIVE"), ct);
        var shipments = await db.ShippingInfos.AsNoTracking().Where(x => x.order.buyerId != null).OrderByDescending(x => x.id).Take(1000).Select(x => new { x.id, x.orderId, x.status, x.trackingNumber, x.order.buyerId }).ToListAsync(ct);
        foreach (var shipment in shipments)
        {
            using var noticeScope = scopes.CreateScope(); var noticeDb = noticeScope.ServiceProvider.GetRequiredService<BuyerSchemaContext>();
            await using var noticeTx = await noticeDb.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var key = AccountLifecycleService.HashToken(shipment.id + ":" + shipment.status + ":" + shipment.trackingNumber);
            var exists = await noticeDb.OperationReceipts.FromSqlInterpolated($"SELECT * FROM dbo.OperationReceipt WITH(UPDLOCK,HOLDLOCK) WHERE scope=N'shipping-notice' AND operationKey={key}").AnyAsync(ct);
            if (exists) continue;
            noticeDb.OperationReceipts.Add(new OperationReceipt { scope = "shipping-notice", operationKey = key, requestHash = "", responseJson = "{}" });
            noticeDb.Notifications.Add(new Buyer.Infrastructure.BuyerSchema.Models.Notification { userId = shipment.buyerId!.Value, type = "SHIPPING", title = "Shipping update", content = "Order #" + shipment.orderId + ": " + shipment.status + " " + shipment.trackingNumber, referenceType = "ORDER", referenceId = shipment.orderId.ToString() });
            await noticeDb.SaveChangesAsync(ct); await noticeTx.CommitAsync(ct);
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
            if (auction.winnerId != null) context.Notifications.Add(new Buyer.Infrastructure.BuyerSchema.Models.Notification { userId = auction.winnerId.Value, type = "AUCTION_WON", title = "You won an auction", content = "Complete checkout within 48 hours for auction #" + auction.id, referenceType = "AUCTION", referenceId = auction.id.ToString() });
            await context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
    }
}
