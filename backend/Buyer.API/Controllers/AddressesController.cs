using System.Security.Claims;
using Buyer.Application.Addresses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

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
public sealed class AddressesController(AddressService service) : ControllerBase
{
    // Identity comes exclusively from the validated JWT, never request body/query userId.
    private int UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub"), out var id) && id > 0 ? id
        : throw new AddressException("AUTH_UNAUTHORIZED", "Invalid account identity.", AddressFailure.Unauthorized);

    [HttpGet]
    [ProducesResponseType<ApiEnvelope<IReadOnlyList<AddressResponse>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(ApiEnvelope<IReadOnlyList<AddressResponse>>.Succeeded(await service.ListAsync(UserId, ct)));

    [HttpGet("countries")]
    [ProducesResponseType<ApiEnvelope<IReadOnlyList<CountryResponse>>>(StatusCodes.Status200OK)]
    public IActionResult Countries() => Ok(ApiEnvelope<IReadOnlyList<CountryResponse>>.Succeeded(AddressService.Countries));

    [HttpGet("{addressId}")]
    [ProducesResponseType<ApiEnvelope<AddressResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(string addressId, CancellationToken ct)
        => Ok(ApiEnvelope<AddressResponse>.Succeeded(await service.SelectForCheckoutAsync(UserId, Id(addressId), ct)));

    [HttpPost]
    [ProducesResponseType<ApiEnvelope<AddressResponse>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateAddressRequest request, CancellationToken ct)
    {
        var address = await service.CreateAsync(UserId, request, ct);
        return CreatedAtAction(nameof(Get), new { addressId = address.Id }, ApiEnvelope<AddressResponse>.Succeeded(address));
    }

    [HttpPatch("{addressId}")]
    [ProducesResponseType<ApiEnvelope<AddressResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(string addressId, UpdateAddressRequest request, CancellationToken ct)
        => Ok(ApiEnvelope<AddressResponse>.Succeeded(await service.UpdateAsync(UserId, Id(addressId), request, ct)));

    [HttpPatch("{addressId}/default")]
    [ProducesResponseType<ApiEnvelope<AddressResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Default(string addressId, CancellationToken ct)
        => Ok(ApiEnvelope<AddressResponse>.Succeeded(await service.SetDefaultAsync(UserId, Id(addressId), ct)));

    [HttpDelete("{addressId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string addressId, CancellationToken ct)
    {
        await service.DeleteAsync(UserId, Id(addressId), ct);
        return NoContent();
    }

    private static int Id(string value) => int.TryParse(value, out var id) && id > 0 ? id
        : throw new AddressException("ADDRESS_ID_INVALID", "Address ID must be a positive integer.", AddressFailure.Invalid, "addressId");
}

// Feature-specific mapping avoids modifying the auth/catalog exception behavior.
public sealed class AddressExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not AddressException error) return;
        var status = error.Failure switch
        {
            AddressFailure.Invalid => 400,
            AddressFailure.NotFound => 404,
            AddressFailure.Unauthorized => 401,
            AddressFailure.Forbidden => 403,
            AddressFailure.Conflict => 409,
            AddressFailure.Unavailable => 503,
            _ => 500
        };
        context.Result = new ObjectResult(ApiEnvelope<object>.Failure(error.Code, error.Message, error.Field)) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}
