using Buyer.Application.Catalog;

namespace Buyer.Infrastructure.Data;

public sealed class UnavailableProductRepository : IProductRepository, IProductCatalogRepository
{
    public Task<ProductCatalogPage> GetAvailableProductsAsync(
        int page, int pageSize, DateTime utcNow, CancellationToken cancellationToken)
        => throw new CatalogUnavailableException();

    public Task<ProductDetailResponse?> GetDetailAsync(int productId, CancellationToken cancellationToken)
        => throw new CatalogUnavailableException();

}
