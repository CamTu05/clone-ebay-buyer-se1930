using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Buyer.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Development");
            builder.UseSetting(
                "ConnectionStrings:BuyerDatabase",
                "Server=(localdb)\\MSSQLLocalDB;Database=BuyerApiTests;Trusted_Connection=True");
            builder.UseSetting("BuyerLifecycle:Enabled", "false");
            builder.UseSetting("Jwt:Issuer", "BuyerApi.Tests");
            builder.UseSetting("Jwt:Audience", "BuyerApi.Tests.Client");
            builder.UseSetting("Jwt:SigningKey", "test-only-signing-key-at-least-32-characters");
            builder.UseSetting("Jwt:DurationMinutes", "15");
        }).CreateClient();
    }

    [Theory]
    [InlineData("/health/live")]

    [InlineData("/api/system/status")]
    public async Task EndpointReturnsSuccessAsync(string endpoint)
    {
        var response = await _client.GetAsync(endpoint);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReadinessIsUnavailableWithoutDatabase()
    {
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await _client.GetAsync("/health/ready")).StatusCode);
    }

    [Fact]
    public async Task SwaggerDefinitionIncludesAuthProductsAndAddressesWithoutSchemaConflicts()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/auth/login", out _));
        Assert.True(paths.TryGetProperty("/api/listings/{listingId}", out _));
        Assert.True(paths.TryGetProperty("/api/products/{productId}", out _));
        Assert.True(paths.TryGetProperty("/api/addresses", out _));
        Assert.True(document.RootElement.GetProperty("components").GetProperty("schemas").TryGetProperty("ApiError", out _));
    }
}
