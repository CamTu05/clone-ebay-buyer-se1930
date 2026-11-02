using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Buyer.Application.Features.Cart;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buyer.API.Controllers;

[ApiController]
[Authorize(Roles = "BUYER")]
[Route("api/cart")]
public sealed class CartController(ICartService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiEnvelope<CartResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
            return Unauthorized(ApiEnvelope<object>.Failure("AUTH_UNAUTHORIZED"));
        return Ok(ApiEnvelope<CartResponse>.Succeeded(
            await service.GetAsync(userId, cancellationToken)));
    }

    [HttpPatch("items/{cartItemId:int}")]
    [ProducesResponseType(typeof(ApiEnvelope<CartResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateQuantity(int cartItemId,
        UpdateCartItemQuantityRequest request, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
            return Unauthorized(ApiEnvelope<object>.Failure("AUTH_UNAUTHORIZED"));
        return Respond(await service.UpdateQuantityAsync(
            userId, cartItemId, request.Quantity, cancellationToken));
    }

    [HttpDelete("items/{cartItemId:int}")]
    [ProducesResponseType(typeof(ApiEnvelope<CartResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Remove(int cartItemId, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
            return Unauthorized(ApiEnvelope<object>.Failure("AUTH_UNAUTHORIZED"));
        return Respond(await service.RemoveAsync(userId, cartItemId, cancellationToken));
    }

    private IActionResult Respond(CartResult result)
    {
        if (result.IsSuccess)
            return Ok(ApiEnvelope<CartResponse>.Succeeded(result.Value!));

        var (status, code, message, field) = result.Failure switch
        {
            CartFailure.InvalidQuantity => (400, "CART_QUANTITY_INVALID",
                "Quantity must be a positive integer.", "quantity"),
            CartFailure.ItemNotFound => (404, "CART_ITEM_NOT_FOUND",
                "Cart item not found.", (string?)null),
            CartFailure.ListingUnavailable => (409, "CART_LISTING_UNAVAILABLE",
                "This listing is no longer available.", (string?)null),
            CartFailure.QuantityExceedsStock => (409, "CART_QUANTITY_EXCEEDS_STOCK",
                "Requested quantity exceeds available stock.", "quantity"),
            _ => (400, "CART_REQUEST_INVALID", "Cart request is invalid.", (string?)null)
        };
        return StatusCode(status, ApiEnvelope<object>.Failure(code, message, field));
    }

    private bool TryUserId(out int userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(claim, out userId) && userId > 0;
    }
}
