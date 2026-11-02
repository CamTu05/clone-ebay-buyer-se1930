namespace Buyer.Application.Catalog;

public interface IProductRepository
{
    Task<ProductDetailResponse?> GetDetailAsync(int productId, CancellationToken cancellationToken);
}
