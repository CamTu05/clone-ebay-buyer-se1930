using Buyer.Application.Catalog;

namespace Buyer.Infrastructure.Data;

public sealed class UnavailableProductRepository : IProductRepository
{
    public Task<ProductDetailResponse?> GetDetailAsync(int productId, CancellationToken cancellationToken)
        => throw new CatalogUnavailableException();

}
