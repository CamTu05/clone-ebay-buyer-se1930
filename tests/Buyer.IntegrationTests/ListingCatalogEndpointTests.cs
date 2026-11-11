using System.Net;
using System.Net.Http.Json;
using Buyer.API.Controllers;
using Buyer.Application.Interfaces.ListingCatalog;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Buyer.IntegrationTests;

public sealed class ListingCatalogEndpointTests
{
    [Fact]
    public async Task AnonymousBuyerReceivesListingDetailsInSuccessEnvelope()
    {
        var listing = new ListingSummaryResponse(
            12, 7, "Wireless headphones", "https://example.com/headphones.jpg", "Brand", "NEW",
            3, "FIXED_PRICE", 350000m, false, null, null, null, null);
        var service = new StubListingCatalogService
        {
            Result = new ListingPageResponse([listing], 1, 20, 1, 1)
        };
        using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/listings");
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<ListingPageResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.Null(envelope.Errors);
        Assert.NotNull(envelope.Data);
        Assert.Equal(listing, Assert.Single(envelope.Data.Items));
        Assert.Equal(1, envelope.Data.TotalItems);
        Assert.Equal(1, envelope.Data.TotalPages);
    }

    [Fact]
    public async Task EmptyCatalogReturnsEmptyItemsWithPagination()
    {
        var service = new StubListingCatalogService();
        using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/listings");
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<ListingPageResponse>>();

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
        var service = new StubListingCatalogService
        {
            Result = new ListingPageResponse([], page, pageSize, 0, 0)
        };
        using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/listings{query}");
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<ListingPageResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, service.CallCount);
        Assert.NotNull(service.LastRequest);
        Assert.Equal(page, service.LastRequest.Page);
        Assert.Equal(pageSize, service.LastRequest.PageSize);
        Assert.NotNull(envelope?.Data);
        Assert.Equal(page, envelope.Data.Page);
        Assert.Equal(pageSize, envelope.Data.PageSize);
    }

    [Fact]
    public async Task SearchFilterSortParametersAreBoundAndPassedToService()
    {
        var service = new StubListingCatalogService();
        using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/listings?search=iphone&categoryId=3&minPrice=100&maxPrice=1000" +
            "&condition=NEW&brand=Apple&listingType=AUCTION&sellerId=5&freeShipping=true" +
            "&minBidCount=1&maxBidCount=10&sort=price_asc&page=2&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var request = Assert.IsType<ListingListRequest>(service.LastRequest);
        Assert.Equal("iphone", request.Search);
        Assert.Equal(3, request.CategoryId);
        Assert.Equal(100, request.MinPrice);
        Assert.Equal(1000, request.MaxPrice);
        Assert.Equal("NEW", request.Condition);
        Assert.Equal("Apple", request.Brand);
        Assert.Equal("AUCTION", request.ListingType);
        Assert.Equal(5, request.SellerId);
        Assert.True(request.FreeShipping);
        Assert.Equal(1, request.MinBidCount);
        Assert.Equal(10, request.MaxBidCount);
        Assert.Equal("price_asc", request.Sort);
        Assert.Equal(2, request.Page);
    }

    [Theory]
    [InlineData("minPrice=1000&maxPrice=100")]
    [InlineData("minBidCount=10&maxBidCount=1")]
    [InlineData("listingType=INVALID")]
    [InlineData("sort=INVALID")]
    [InlineData("listingType=FIXED_PRICE&sort=ending_soon")]
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
        var service = new StubListingCatalogService();
        using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/listings?{query}");
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
    public async Task UnavailableCatalogReturns503WithSafeErrorEnvelope()
    {
        using var factory = CreateFactory(new StubListingCatalogService { Failure = new CatalogUnavailableException() });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/listings");
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<object>>();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.NotNull(envelope);
        Assert.False(envelope.Success);
        Assert.Null(envelope.Data);
        Assert.Equal("CATALOG_UNAVAILABLE", Assert.Single(envelope.Errors!).Code);
    }

    [Fact]
    public async Task ServiceFailureReturnsGenericErrorWithoutLeakingExceptionDetails()
    {
        var service = new StubListingCatalogService
        {
            Failure = new InvalidOperationException("private-database-connection-details")
        };
        using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/listings");
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

    private static WebApplicationFactory<Program> CreateFactory(StubListingCatalogService service) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.UseSetting(
                "ConnectionStrings:BuyerDatabase",
                "Server=(localdb)\\MSSQLLocalDB;Database=BuyerApiTests;Trusted_Connection=True");
            builder.UseSetting("BuyerLifecycle:Enabled", "false");
            builder.UseSetting("Jwt:Issuer", "BuyerApi.Tests");
            builder.UseSetting("Jwt:Audience", "BuyerApi.Tests.Client");
            builder.UseSetting("Jwt:SigningKey", "test-only-signing-key-at-least-32-characters");
            builder.UseSetting("Jwt:DurationMinutes", "15");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IListingCatalogService>();
                services.AddSingleton<IListingCatalogService>(service);
            });
        });

    private sealed class StubListingCatalogService : IListingCatalogService
    {
        public ListingPageResponse Result { get; init; } = new([], 1, 20, 0, 0);
        public Exception? Failure { get; init; }
        public ListingListRequest? LastRequest { get; private set; }
        public int CallCount { get; private set; }

        public Task<ListingPageResponse> GetListingsAsync(
            ListingListRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            CallCount++;
            return Failure is null
                ? Task.FromResult(Result)
                : Task.FromException<ListingPageResponse>(Failure);
        }

        public Task<ListingDetailResponse?> GetListingAsync(
            int listingId,
            CancellationToken cancellationToken = default) => Task.FromResult<ListingDetailResponse?>(null);
    }
}
