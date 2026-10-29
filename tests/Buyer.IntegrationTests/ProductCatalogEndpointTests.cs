using System.Net;
using System.Net.Http.Json;
using Buyer.API.Controllers;
using Buyer.Application.Catalog;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Buyer.IntegrationTests;

public sealed class ProductCatalogEndpointTests
{
    [Fact]
    public async Task AnonymousBuyerReceivesProductDetailsInSuccessEnvelope()
    {
        var product = new ProductSummaryResponse(
            7, 12, "Wireless headphones", 350000m, "https://example.com/headphones.jpg", "FIXED_PRICE");
        var service = new StubProductCatalogService
        {
            Result = new ProductPageResponse([product], 1, 20, 1, 1)
        };
        using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/products");
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<ProductPageResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.Null(envelope.Errors);
        Assert.NotNull(envelope.Data);
        Assert.Equal(product, Assert.Single(envelope.Data.Items));
        Assert.Equal(1, envelope.Data.TotalItems);
        Assert.Equal(1, envelope.Data.TotalPages);
    }

    [Fact]
    public async Task EmptyCatalogReturnsEmptyItemsWithPagination()
    {
        var service = new StubProductCatalogService();
        using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/products");
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<ProductPageResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.NotNull(envelope.Data);
        Assert.Empty(envelope.Data.Items);
        Assert.Equal(1, envelope.Data.Page);
        Assert.Equal(20, envelope.Data.PageSize);
        Assert.Equal(0, envelope.Data.TotalItems);
        Assert.Equal(0, envelope.Data.TotalPages);
    }

    [Theory]
    [InlineData("", 1, 20)]
    [InlineData("?page=3&pageSize=2", 3, 2)]
    [InlineData("?pageSize=100", 1, 100)]
    [InlineData("?page=2147483647&pageSize=100", int.MaxValue, 100)]
    public async Task PaginationParametersAreBoundAndPassedToService(string query, int page, int pageSize)
    {
        var service = new StubProductCatalogService
        {
            Result = new ProductPageResponse([], page, pageSize, 0, 0)
        };
        using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/products{query}");
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<ProductPageResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, service.CallCount);
        Assert.NotNull(service.LastRequest);
        Assert.Equal(page, service.LastRequest.Page);
        Assert.Equal(pageSize, service.LastRequest.PageSize);
        Assert.NotNull(envelope?.Data);
        Assert.Equal(page, envelope.Data.Page);
        Assert.Equal(pageSize, envelope.Data.PageSize);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("page=-1")]
    [InlineData("page=abc")]
    [InlineData("page=1.5")]
    [InlineData("page=2147483648")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=-1")]
    [InlineData("pageSize=101")]
    [InlineData("pageSize=abc")]
    [InlineData("pageSize=2147483648")]
    public async Task InvalidPaginationReturnsValidationEnvelopeWithoutCallingService(string query)
    {
        var service = new StubProductCatalogService();
        using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/products?{query}");
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<object>>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(envelope);
        Assert.False(envelope.Success);
        Assert.Null(envelope.Data);
        Assert.NotNull(envelope.Errors);
        Assert.NotEmpty(envelope.Errors);
        Assert.All(envelope.Errors, error => Assert.Equal("VALIDATION_ERROR", error.Code));
        Assert.Equal(0, service.CallCount);
    }

    [Fact]
    public async Task ServiceFailureReturnsGenericErrorWithoutLeakingExceptionDetails()
    {
        var service = new StubProductCatalogService
        {
            Failure = new InvalidOperationException("private-database-connection-details")
        };
        using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/products");
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<object>>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(envelope);
        Assert.False(envelope.Success);
        Assert.Null(envelope.Data);
        Assert.Equal("An unexpected error occurred", envelope.Message);
        Assert.NotNull(envelope.Errors);
        Assert.Equal("INTERNAL_ERROR", Assert.Single(envelope.Errors).Code);
        Assert.DoesNotContain("private-database-connection-details", await response.Content.ReadAsStringAsync());
    }

    private static WebApplicationFactory<Program> CreateFactory(StubProductCatalogService service) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.UseSetting(
                "ConnectionStrings:BuyerDatabase",
                "Server=(localdb)\\MSSQLLocalDB;Database=BuyerApiTests;Trusted_Connection=True");
            builder.UseSetting("Jwt:Issuer", "BuyerApi.Tests");
            builder.UseSetting("Jwt:Audience", "BuyerApi.Tests.Client");
            builder.UseSetting("Jwt:SigningKey", "test-only-signing-key-at-least-32-characters");
            builder.UseSetting("Jwt:DurationMinutes", "15");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IProductCatalogService>();
                services.AddSingleton<IProductCatalogService>(service);
            });
        });

    private sealed class StubProductCatalogService : IProductCatalogService
    {
        public ProductPageResponse Result { get; init; } = new([], 1, 20, 0, 0);
        public Exception? Failure { get; init; }
        public ProductListRequest? LastRequest { get; private set; }
        public int CallCount { get; private set; }

        public Task<ProductPageResponse> GetProductsAsync(
            ProductListRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            CallCount++;
            return Failure is null
                ? Task.FromResult(Result)
                : Task.FromException<ProductPageResponse>(Failure);
        }
    }
}
