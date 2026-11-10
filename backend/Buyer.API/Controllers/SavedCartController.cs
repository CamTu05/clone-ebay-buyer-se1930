using System.Data;
using System.Security.Claims;
using Buyer.Application;
using Buyer.Infrastructure.BuyerSchema;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Buyer.API.Controllers;

[ApiController, Authorize(Roles = "BUYER"), Route("api/cart")]
public sealed class SavedCartController(BuyerSchemaContext db, BuyerWorkflowService workflow) : ControllerBase
{
    private int UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id : throw new BuyerRuleException("AUTH_UNAUTHORIZED", "Authentication required.", 401);

    [HttpGet("saved")]
    public async Task<object> Saved(CancellationToken ct)
    {
        await workflow.RequireBuyer(UserId, ct);
        var items = await db.CartItems.AsNoTracking()
            .Where(x => x.cart.userId == UserId && x.status == "SAVED_FOR_LATER")
            .Select(x => new { x.id, x.listingId, x.quantity, x.listing.title, x.listing.price, availableQuantity = x.listing.quantity, listingStatus = x.listing.status })
            .ToListAsync(ct);
        return ApiEnvelope<object>.Succeeded(items);
    }

    [HttpPost("items/{id:int}/save-for-later")]
    public Task<object> Save(int id, CancellationToken ct) => Move(id, false, ct);

    [HttpPost("items/{id:int}/move-to-cart")]
    public Task<object> Restore(int id, CancellationToken ct) => Move(id, true, ct);

    private async Task<object> Move(int id, bool restore, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await workflow.RequireBuyer(UserId, ct);
        var item = await db.CartItems.Include(x => x.listing).ThenInclude(x => x.seller)
            .SingleOrDefaultAsync(x => x.id == id && x.cart.userId == UserId, ct)
            ?? throw new BuyerRuleException("CART_ITEM_NOT_FOUND", "Cart item not found.", 404);
        var listing = item.listing;
        if (restore && (listing.status != "ACTIVE" || listing.listingType != "FIXED_PRICE" || listing.price == null
            || listing.quantity < item.quantity || listing.seller.accountStatus != "ACTIVE"
            || listing.publishedAt > DateTime.UtcNow || listing.endedAt <= DateTime.UtcNow))
            throw new BuyerRuleException("LISTING_UNAVAILABLE", "Item is no longer available in the requested quantity.", 409);
        item.status = restore ? "ACTIVE" : "SAVED_FOR_LATER";
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return ApiEnvelope<object>.Succeeded(new { item.id, item.status });
    }
}
