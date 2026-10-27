using System.Net;
using System.Net.Http.Json;
using Buyer.API.Responses;
using Buyer.Application.Catalog;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Buyer.IntegrationTests;

public sealed class ProductEndpointTests
{
    private sealed class CatalogFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
            => builder.ConfigureServices(services =>
            {
                services.RemoveAll<IProductRepository>();
                services.AddSingleton<IProductRepository>(new SampleProducts());
            });
    }

    private sealed class SampleProducts : IProductRepository
    {
        public Task<ProductDetailResponse?> GetDetailAsync(int productId, CancellationToken cancellationToken)
            => Task.FromResult(productId == 1 ? new ProductDetailResponse(1, "Camera", "Camera description",
                "Brand", "Used", [new(1, "https://example.com/camera.jpg", true)],
                [new(11, "FIXED_PRICE", 500000, new(2, "seller", null, "Camera store")),
                 new(12, "AUCTION", 400000, new(3, "other-seller", null, null))]) : null);

        public Task<ProductPageResponse> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken)
            => Task.FromResult(new ProductPageResponse([new(1, "Camera", "https://example.com/camera.jpg")], page, pageSize, 1, 1));
    }

    [Fact]
    public async Task DetailReturnsProductImagesAndSellerSpecificPrices()
    {
        using var factory = new CatalogFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/products/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProductDetailResponse>>();
        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.Null(body.Errors);
        Assert.Equal("Camera description", body.Data!.Description);
        Assert.Single(body.Data.Images);
        Assert.Equal(2, body.Data.Listings.Count);
        Assert.Equal(500000m, body.Data.Listings[0].Price);
        Assert.Equal("seller", body.Data.Listings[0].Seller.Username);
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", json);
        Assert.DoesNotContain("maximumBid", json);
    }

    [Theory]
    [InlineData("abc", HttpStatusCode.BadRequest, "PRODUCT_ID_INVALID")]
    [InlineData("0", HttpStatusCode.BadRequest, "PRODUCT_ID_INVALID")]
    [InlineData("-1", HttpStatusCode.BadRequest, "PRODUCT_ID_INVALID")]
    [InlineData("2147483648", HttpStatusCode.BadRequest, "PRODUCT_ID_INVALID")]
    [InlineData("999", HttpStatusCode.NotFound, "PRODUCT_NOT_FOUND")]
    public async Task InvalidOrMissingIdReturnsDocumentedError(string id, HttpStatusCode status, string code)
    {
        using var factory = new CatalogFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/products/{id}");
        Assert.Equal(status, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProductDetailResponse>>();
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Null(body.Data);
        Assert.Equal(code, Assert.Single(body.Errors!).Code);
    }

    [Fact]
    public async Task ListContainsProductIdUsedToOpenDetails()
    {
        using var factory = new CatalogFactory();
        using var client = factory.CreateClient();
        var page = await client.GetFromJsonAsync<ApiResponse<ProductPageResponse>>("/api/products");
        var product = Assert.Single(page!.Data!.Items);
        var detail = await client.GetAsync($"/api/products/{product.ProductId}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=101")]
    [InlineData("page=abc")]
    [InlineData("page=2147483647&pageSize=100")]
    public async Task InvalidPaginationReturnsEnvelope(string query)
    {
        using var factory = new CatalogFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/products?{query}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProductPageResponse>>();
        Assert.False(body!.Success);
        Assert.NotEmpty(body.Errors!);
    }
}
