using System.Text.Json;
using Buyer.Infrastructure.BuyerSchema;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Buyer.API.Controllers;
[ApiController, Route("api/auctions")]
public sealed class AuctionStreamController(BuyerSchemaContext db) : ControllerBase
{
    [HttpGet("{id:int}/events")]
    public async Task Events(int id, CancellationToken ct)
    {
        Response.ContentType = "text/event-stream"; Response.Headers.CacheControl = "no-cache"; Response.Headers["X-Accel-Buffering"] = "no";
        string? previous = null; using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        try
        {
            do
            {
                var snapshot = await db.Auctions.AsNoTracking().Where(x => x.id == id && x.listing.status != "DRAFT").Select(x => new { x.id, x.currentPrice, x.bidCount, x.bidIncrement, x.endTime, x.status }).SingleOrDefaultAsync(ct);
                if (snapshot == null) { await Response.WriteAsync("event: unavailable\ndata: {}\n\n", ct); break; }
                var json = JsonSerializer.Serialize(snapshot);
                if (json != previous) { await Response.WriteAsync("data: " + json + "\n\n", ct); previous = json; }
                else await Response.WriteAsync(": heartbeat\n\n", ct);
                await Response.Body.FlushAsync(ct);
                if (snapshot.status is "ENDED" or "CANCELLED") break;
            } while (await timer.WaitForNextTickAsync(ct));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
    }
}
