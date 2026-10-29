namespace Buyer.Application.Catalog;

public sealed class ProductService(IProductRepository repository)
{
    public Task<ProductDetailResponse?> GetDetailAsync(int productId, CancellationToken cancellationToken)
        => repository.GetDetailAsync(productId, cancellationToken);

}
