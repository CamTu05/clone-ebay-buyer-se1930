using System.Security.Claims;
using Buyer.Application.Interfaces.Addresses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Buyer.API.Filters;

namespace Buyer.API.Controllers;

[ApiController]
[Authorize]
[Route("api/addresses")]
[ServiceFilter(typeof(AddressExceptionFilter))]
[ProducesResponseType<ApiEnvelope<object>>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ApiEnvelope<object>>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ApiEnvelope<object>>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ApiEnvelope<object>>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ApiEnvelope<object>>(StatusCodes.Status409Conflict)]
public sealed class AddressesController(IAddressService service) : ControllerBase
{
    // Identity comes exclusively from the validated JWT, never request body/query userId.
    private int UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub"), out var id) && id > 0 ? id
        : throw new AddressException("AUTH_UNAUTHORIZED", "Invalid account identity.", AddressFailure.Unauthorized);

    [HttpGet]
    [ProducesResponseType<ApiEnvelope<IReadOnlyList<AddressResponse>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(CancellationToken ct)
        => Ok(ApiEnvelope<IReadOnlyList<AddressResponse>>.Succeeded(await service.ListAsync(UserId, ct)));

    [HttpGet("countries")]
    [ProducesResponseType<ApiEnvelope<IReadOnlyList<CountryResponse>>>(StatusCodes.Status200OK)]
    public IActionResult Countries() => Ok(ApiEnvelope<IReadOnlyList<CountryResponse>>.Succeeded(service.Countries));

    [HttpGet("{addressId}")]
    [ProducesResponseType<ApiEnvelope<AddressResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(string addressId, CancellationToken ct)
        => Ok(ApiEnvelope<AddressResponse>.Succeeded(await service.SelectForCheckoutAsync(UserId, Id(addressId), ct)));

    [HttpPost]
    [ProducesResponseType<ApiEnvelope<AddressResponse>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync(CreateAddressRequest request, CancellationToken ct)
    {
        var address = await service.CreateAsync(UserId, request, ct);
        return CreatedAtAction(nameof(Get), new { addressId = address.Id }, ApiEnvelope<AddressResponse>.Succeeded(address));
    }

    [HttpPatch("{addressId}")]
    [ProducesResponseType<ApiEnvelope<AddressResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync(string addressId, UpdateAddressRequest request, CancellationToken ct)
        => Ok(ApiEnvelope<AddressResponse>.Succeeded(await service.UpdateAsync(UserId, Id(addressId), request, ct)));

    [HttpPatch("{addressId}/default")]
    [ProducesResponseType<ApiEnvelope<AddressResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> DefaultAsync(string addressId, CancellationToken ct)
        => Ok(ApiEnvelope<AddressResponse>.Succeeded(await service.SetDefaultAsync(UserId, Id(addressId), ct)));

    [HttpDelete("{addressId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(string addressId, CancellationToken ct)
    {
        await service.DeleteAsync(UserId, Id(addressId), ct);
        return NoContent();
    }

    private static int Id(string value) => int.TryParse(value, out var id) && id > 0 ? id
        : throw new AddressException("ADDRESS_ID_INVALID", "Address ID must be a positive integer.", AddressFailure.Invalid, "addressId");
}

// Feature-specific mapping avoids modifying the auth/catalog exception behavior.
