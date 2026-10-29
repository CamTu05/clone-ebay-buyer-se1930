using Buyer.API.Responses;
using Buyer.Application.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buyer.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/products")]
public sealed class ProductsController(IProductCatalogService catalogService, ProductService service) : ControllerBase
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

    [HttpGet("{productId}")]
    [ProducesResponseType<ApiResponse<ProductDetailResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<ProductDetailResponse>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<ProductDetailResponse>>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse<ProductDetailResponse>>(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType<ApiResponse<ProductDetailResponse>>(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType<ApiResponse<ProductDetailResponse>>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetDetail(string productId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(productId, out var id) || id <= 0)
            return BadRequest(ApiResponse<ProductDetailResponse>.Fail(
                "Product ID must be a positive integer.", "PRODUCT_ID_INVALID", "productId"));

        var product = await service.GetDetailAsync(id, cancellationToken);
        return product is null
            ? NotFound(ApiResponse<ProductDetailResponse>.Fail("Product not found.", "PRODUCT_NOT_FOUND"))
            : Ok(ApiResponse<ProductDetailResponse>.Ok(product));
    }

}
