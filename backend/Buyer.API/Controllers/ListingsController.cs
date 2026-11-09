using Buyer.Application.Interfaces.ListingCatalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buyer.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/listings")]
public sealed class ListingsController(IListingCatalogService catalogService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ApiEnvelope<ListingPageResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiEnvelope<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiEnvelope<object>>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ApiEnvelope<object>>(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType<ApiEnvelope<object>>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetListingsAsync(
        [FromQuery] ListingListRequest request,
        CancellationToken cancellationToken)
    {
        var result = await catalogService.GetListingsAsync(request, cancellationToken);
        return Ok(ApiEnvelope<ListingPageResponse>.Succeeded(result));
    }

    [HttpGet("{listingId}")]
    [ProducesResponseType<ApiEnvelope<ListingDetailResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiEnvelope<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiEnvelope<object>>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiEnvelope<object>>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ApiEnvelope<object>>(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType<ApiEnvelope<object>>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetListingAsync(string listingId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(listingId, out var id) || id <= 0)
            return BadRequest(ApiEnvelope<object>.Failure(
                "LISTING_ID_INVALID", "Listing ID must be a positive integer.", "listingId"));

        var listing = await catalogService.GetListingAsync(id, cancellationToken);
        return listing is null
            ? NotFound(ApiEnvelope<object>.Failure("LISTING_NOT_FOUND", "Listing not found."))
            : Ok(ApiEnvelope<ListingDetailResponse>.Succeeded(listing));
    }
}
