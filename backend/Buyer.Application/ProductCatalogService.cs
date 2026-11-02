using Buyer.Application.Authentication;

namespace Buyer.Application.Catalog;

public sealed class ProductCatalogService(IProductCatalogRepository repository, IClock clock)
    : IProductCatalogService
{
    public async Task<ProductPageResponse> GetProductsAsync(
        ProductListRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await repository.GetAvailableProductsAsync(
            request.Page, request.PageSize, clock.UtcNow, cancellationToken);

        return new ProductPageResponse(
            result.Items,
            request.Page,
            request.PageSize,
            result.TotalItems,
            (int)Math.Ceiling((double)result.TotalItems / request.PageSize));
    }
}
