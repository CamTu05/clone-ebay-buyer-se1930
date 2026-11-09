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

public sealed class ListingDetailEndpointTests
{
    [Fact]
    public async Task FixedPriceListingReturnsListingDetailEnvelope()
    {
        var detail = FixedPriceDetail(11, 1, 2, 500000m);
        using var factory = CreateFactory(new StubListingCatalogService { Detail = detail });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/listings/11");
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<ListingDetailResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(envelope?.Data);
        Assert.Equal(11, envelope.Data.ListingId);
        Assert.Equal(1, envelope.Data.ProductId);
        Assert.Equal(2, envelope.Data.SellerId);
        Assert.Equal("seller-2", envelope.Data.SellerUsername);
        Assert.Single(envelope.Data.Images);
        Assert.Single(envelope.Data.Shipping.AvailableOptions);
        Assert.Null(envelope.Data.Auction);
        Assert.Equal(500000m, envelope.Data.EffectivePrice);
        Assert.DoesNotContain("maximumBid", await response.Content.ReadAsStringAsync());
        Assert.DoesNotContain("passwordHash", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task AuctionListingReturnsAuctionCurrentPriceAndIdentifier()
    {
        var detail = FixedPriceDetail(12, 1, 3, 400000m) with
        {
            ListingType = "AUCTION",
            EffectivePrice = 400000m,
            BuyItNowPrice = null,
            Auction = new AuctionDetailResponse(
                99, 300000m, 400000m, 10000m, 4,
                new DateTime(2026, 10, 27, 8, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 10, 28, 8, 0, 0, DateTimeKind.Utc), "ACTIVE")
        };
        using var factory = CreateFactory(new StubListingCatalogService { Detail = detail });
        using var client = factory.CreateClient();

        var envelope = await client.GetFromJsonAsync<ApiEnvelope<ListingDetailResponse>>("/api/listings/12");

        Assert.Equal(400000m, envelope!.Data!.EffectivePrice);
        Assert.Equal(99, envelope.Data.Auction!.AuctionId);
        Assert.Equal(400000m, envelope.Data.Auction.CurrentPrice);
    }

    [Theory]
    [InlineData("abc", HttpStatusCode.BadRequest, "LISTING_ID_INVALID")]
    [InlineData("0", HttpStatusCode.BadRequest, "LISTING_ID_INVALID")]
    [InlineData("-1", HttpStatusCode.BadRequest, "LISTING_ID_INVALID")]
    [InlineData("2147483648", HttpStatusCode.BadRequest, "LISTING_ID_INVALID")]
    [InlineData("999", HttpStatusCode.NotFound, "LISTING_NOT_FOUND")]
    public async Task InvalidOrMissingListingReturnsDocumentedError(
        string id, HttpStatusCode status, string code)
    {
        using var factory = CreateFactory(new StubListingCatalogService());
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/listings/{id}");
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<object>>();

        Assert.Equal(status, response.StatusCode);
        Assert.NotNull(envelope);
        Assert.False(envelope.Success);
        Assert.Equal(code, Assert.Single(envelope.Errors!).Code);
    }

    [Fact]
    public async Task CatalogListingIdOpensTheCorrectListingDetail()
    {
        var detail = FixedPriceDetail(27, 4, 8, 700000m);
        var service = new StubListingCatalogService
        {
            Detail = detail,
            Page = new ListingPageResponse([
                new(27, 4, detail.Title, null, detail.Brand, detail.ItemCondition, detail.SellerId,
                    detail.ListingType, detail.EffectivePrice, detail.AllowBestOffer, false,
                    null, null, null, detail.PublishedAt)
            ], 1, 20, 1, 1)
        };
        using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var page = await client.GetFromJsonAsync<ApiEnvelope<ListingPageResponse>>("/api/listings");
        var item = Assert.Single(page!.Data!.Items);
        var response = await client.GetAsync($"/api/listings/{item.ListingId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(27, service.LastListingId);
    }

    private static ListingDetailResponse FixedPriceDetail(
        int listingId, int productId, int sellerId, decimal price) => new(
        listingId,
        productId,
        "Camera",
        "Camera description",
        "Brand",
        "Used",
        new CategorySummaryResponse(1, "Electronics", "electronics"),
        [new ProductImageResponse(1, "https://example.com/camera.jpg", true, 0)],
        sellerId,
        $"seller-{sellerId}",
        98.5m,
        20,
        new StoreSummaryResponse(1, "Camera Store", null),
        "FIXED_PRICE",
        price,
        price,
        2,
        true,
        new ShippingSummaryResponse("FREE", [
            new ShippingOptionResponse(1, "Standard", "STANDARD", 0, 2, 4)
        ]),
        null,
        new DateTime(2026, 10, 27, 8, 0, 0, DateTimeKind.Utc));

    private static WebApplicationFactory<Program> CreateFactory(StubListingCatalogService service) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.UseSetting("ConnectionStrings:BuyerDatabase",
                "Server=(localdb)\\MSSQLLocalDB;Database=BuyerApiTests;Trusted_Connection=True");
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
        public ListingDetailResponse? Detail { get; init; }
        public ListingPageResponse Page { get; init; } = new([], 1, 20, 0, 0);
        public int? LastListingId { get; private set; }

        public Task<ListingPageResponse> GetListingsAsync(
            ListingListRequest request,
            CancellationToken cancellationToken = default) => Task.FromResult(Page);

        public Task<ListingDetailResponse?> GetListingAsync(
            int listingId,
            CancellationToken cancellationToken = default)
        {
            LastListingId = listingId;
            return Task.FromResult(Detail?.ListingId == listingId ? Detail : null);
        }
    }
}
