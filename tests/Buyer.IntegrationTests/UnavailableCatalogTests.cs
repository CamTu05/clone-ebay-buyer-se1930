using Buyer.Application.Catalog;
using Buyer.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Buyer.IntegrationTests;

public sealed class UnavailableCatalogTests
{
    [Fact]
    public async Task MissingDatabaseConfigurationResolvesCatalogAndReportsUnavailable()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build());
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var catalog = scope.ServiceProvider.GetRequiredService<IProductCatalogService>();

        await Assert.ThrowsAsync<CatalogUnavailableException>(() =>
            catalog.GetProductsAsync(new ProductListRequest()));
    }
}

