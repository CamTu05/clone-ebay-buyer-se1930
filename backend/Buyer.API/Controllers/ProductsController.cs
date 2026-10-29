using Buyer.API.Responses;
using Buyer.Application.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace Buyer.API.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(ProductService service) : ControllerBase
{
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

    [HttpGet]
    [ProducesResponseType<ApiResponse<ProductPageResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<ProductPageResponse>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<ProductPageResponse>>(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType<ApiResponse<ProductPageResponse>>(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType<ApiResponse<ProductPageResponse>>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetPage([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page <= 0 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            return BadRequest(ApiResponse<ProductPageResponse>.Fail(
                "Page must be positive and pageSize must be between 1 and 100.", "PAGINATION_INVALID"));

        return Ok(ApiResponse<ProductPageResponse>.Ok(await service.GetPageAsync(page, pageSize, cancellationToken)));
    }
}
