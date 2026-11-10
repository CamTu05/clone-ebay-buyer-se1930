namespace Buyer.Application.Catalog;

public sealed record ProductCatalogPage(IReadOnlyList<ProductSummaryResponse> Items, int TotalItems);

public interface IProductCatalogRepository
{
    Task<ProductCatalogPage> SearchAsync(ProductListRequest request, DateTime utcNow, CancellationToken ct)
        => GetAvailableProductsAsync(request.Page, request.PageSize, utcNow, ct);
    Task<ProductCatalogPage> GetAvailableProductsAsync(
        int page,
        int pageSize,
        DateTime utcNow,
        CancellationToken cancellationToken);
}

public interface IProductCatalogService
{
    Task<ProductPageResponse> GetProductsAsync(
        ProductListRequest request,
        CancellationToken cancellationToken = default);
}
