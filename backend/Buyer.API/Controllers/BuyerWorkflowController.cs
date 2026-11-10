using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Buyer.Infrastructure.BuyerSchema;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Buyer.API.Controllers;

[ApiController, Authorize(Roles = "BUYER"), Route("api")]
public sealed class BuyerWorkflowController(BuyerWorkflowService service, BuyerSchemaContext db, IConfiguration configuration) : ControllerBase
{
    int Uid => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : throw new Buyer.Application.BuyerRuleException("AUTH_UNAUTHORIZED", "Authentication required.", 401);
    object Success(object? data = null) => ApiEnvelope<object>.Succeeded(data ?? new { success = true });
    public sealed record CartInput([Range(1, int.MaxValue)] int ListingId, [Range(1, 1000)] int Quantity);
    public sealed record PaymentInput(bool Success);
    public sealed record ReasonInput([Required, StringLength(1000)] string Reason);
    public sealed record ReviewInput([Range(1, 5)] int Rating, [StringLength(4000)] string? Comment);
    public sealed record ReturnInput([Range(1, 1000)] int Quantity, [Required, StringLength(1000)] string Reason);
    public sealed record BidInput(decimal MaximumBid);
    [HttpPost("cart/items")]
    public async Task<object> AddCart(CartInput r, CancellationToken ct) { await service.AddCart(Uid, r.ListingId, r.Quantity, ct); return Success(); }
    [HttpGet("watchlist")]
    public async Task<object> Watchlist(CancellationToken ct) => Success(await service.Watchlist(Uid, ct));
    [HttpPut("watchlist/{listingId:int}")]
    public async Task<object> Watch(int listingId, CancellationToken ct) { await service.SaveWatch(Uid, listingId, true, ct); return Success(); }
    [HttpDelete("watchlist/{listingId:int}")]
    public async Task<object> Unwatch(int listingId, CancellationToken ct) { await service.SaveWatch(Uid, listingId, false, ct); return Success(); }
    [HttpGet("orders")]
    public async Task<object> Orders(CancellationToken ct) => Success(await service.Orders(Uid, ct));
    [HttpGet("notifications")]
    public async Task<object> Notifications(CancellationToken ct) => Success(await service.Notifications(Uid, ct));
    [HttpPatch("notifications/{id:long}/read")]
    public async Task<object> Read(long id, CancellationToken ct) { await service.ReadNotification(Uid, id, ct); return Success(); }
    [HttpPost("checkout/preview")]
    public async Task<object> Preview(CheckoutRequest r, CancellationToken ct) => Success(await service.Checkout(Uid, r, true, ct));
    [HttpPost("checkout")]
    public async Task<object> Checkout(CheckoutRequest r, CancellationToken ct) => Success(await service.Checkout(Uid, r, false, ct));
    [HttpPost("checkout/{id:long}/mock-payment")]
    public async Task<object> Pay(long id, PaymentInput r, CancellationToken ct) { if (!configuration.GetValue<bool>("Payments:EnableMock")) throw new Buyer.Application.BuyerRuleException("MOCK_DISABLED", "Mock payments are disabled.", 403); return Success(await service.MockPayment(Uid, id, r.Success, ct)); }
    [HttpPost("checkout/{id:long}/cancel")]
    public async Task<object> Cancel(long id, CancellationToken ct) { await service.CancelPendingCheckout(Uid, id, ct); return Success(); }
    [HttpPost("orders/{id:long}/cancellation-requests")]
    public async Task<object> CancelRequest(long id, ReasonInput r, CancellationToken ct) { await service.RequestCancellation(Uid, id, r.Reason, ct); return Success(); }
    [HttpPost("order-items/{id:long}/reviews")]
    public async Task<object> Review(long id, ReviewInput r, CancellationToken ct) { await service.Review(Uid, id, r.Rating, r.Comment, ct); return Success(); }
    [HttpPost("order-items/{id:long}/returns")]
    public async Task<object> Return(long id, ReturnInput r, CancellationToken ct) { await service.Return(Uid, id, r.Quantity, r.Reason, ct); return Success(); }
    [HttpPost("auctions/{id:int}/bids")]
    public async Task<object> Bid(int id, BidInput r, CancellationToken ct) => Success(await service.Bid(Uid, id, r.MaximumBid, ct));
    [AllowAnonymous, HttpGet("categories")]
    public async Task<object> Categories(CancellationToken ct) => Success(await db.Categories.AsNoTracking().Where(x => x.isActive).Select(x => new { x.id, x.name, x.slug, x.parentCategoryId }).ToListAsync(ct));
    [AllowAnonymous, HttpGet("auctions/{id:int}")]
    public async Task<object> Auction(int id, CancellationToken ct) => Success(await db.Auctions.AsNoTracking().Where(x => x.id == id && x.listing.status != "DRAFT").Select(x => new { x.id, x.listingId, x.startingPrice, x.currentPrice, x.bidIncrement, x.startTime, x.endTime, x.bidCount, x.status }).SingleOrDefaultAsync(ct) ?? throw new Buyer.Application.BuyerRuleException("NOT_FOUND", "Auction not found.", 404));
}
