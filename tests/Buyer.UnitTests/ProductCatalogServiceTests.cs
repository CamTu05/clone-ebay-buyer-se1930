using Buyer.Application.Authentication;
using Buyer.Application.Catalog;

namespace Buyer.UnitTests;

public sealed class ProductCatalogServiceTests
{
    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 1)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(int.MaxValue, 100, 21474837)]
    public async Task PaginationIncludesPartialLastPageAndHandlesEmptyCatalog(
        int totalItems, int pageSize, int expectedPages)
    {
        var repository = new StubProductCatalogRepository(new ProductCatalogPage([], totalItems));
        var service = new ProductCatalogService(repository, new FixedClock());

        var result = await service.GetProductsAsync(new ProductListRequest { Page = 2, PageSize = pageSize });

        Assert.Equal(2, result.Page);
        Assert.Equal(pageSize, result.PageSize);
        Assert.Equal(totalItems, result.TotalItems);
        Assert.Equal(expectedPages, result.TotalPages);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task RepositoryReceivesUtcSnapshotPaginationAndRequestCancellation()
    {
        var product = new ProductSummaryResponse(5, 8, "Camera", 1500000m, null, "FIXED_PRICE");
        var repository = new StubProductCatalogRepository(new ProductCatalogPage([product], 21));
        var clock = new FixedClock();
        var service = new ProductCatalogService(repository, clock);
        using var cancellation = new CancellationTokenSource();

        var result = await service.GetProductsAsync(
            new ProductListRequest { Page = 3, PageSize = 10 }, cancellation.Token);

        Assert.Equal(3, repository.Page);
        Assert.Equal(10, repository.PageSize);
        Assert.Equal(clock.UtcNow, repository.UtcNow);
        Assert.Equal(cancellation.Token, repository.CancellationToken);
        Assert.Equal(product, Assert.Single(result.Items));
        Assert.Equal(3, result.TotalPages);
    }

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow { get; } = new(2026, 10, 27, 8, 0, 0, DateTimeKind.Utc);
    }

    private sealed class StubProductCatalogRepository(ProductCatalogPage result) : IProductCatalogRepository
    {
        public int Page { get; private set; }
        public int PageSize { get; private set; }
        public DateTime UtcNow { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public Task<ProductCatalogPage> GetAvailableProductsAsync(
            int page,
            int pageSize,
            DateTime utcNow,
            CancellationToken cancellationToken)
        {
            Page = page;
            PageSize = pageSize;
            UtcNow = utcNow;
            CancellationToken = cancellationToken;
            return Task.FromResult(result);
        }
    }
}
