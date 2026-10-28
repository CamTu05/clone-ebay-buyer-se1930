using Buyer.Application.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buyer.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/products")]
public sealed class ProductsController(IProductCatalogService catalogService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ApiEnvelope<ProductPageResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiEnvelope<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiEnvelope<object>>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ApiEnvelope<object>>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] ProductListRequest request,
        CancellationToken cancellationToken)
    {
        var result = await catalogService.GetProductsAsync(request, cancellationToken);
        return Ok(ApiEnvelope<ProductPageResponse>.Succeeded(result));
    }
}
