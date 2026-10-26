using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Buyer.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting(
                "ConnectionStrings:BuyerDatabase",
                "Server=(localdb)\\MSSQLLocalDB;Database=BuyerApiTests;Trusted_Connection=True");
            builder.UseSetting("Jwt:Issuer", "BuyerApi.Tests");
            builder.UseSetting("Jwt:Audience", "BuyerApi.Tests.Client");
            builder.UseSetting("Jwt:SigningKey", "test-only-signing-key-at-least-32-characters");
            builder.UseSetting("Jwt:DurationMinutes", "15");
        }).CreateClient();
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    [InlineData("/api/system/status")]
    public async Task EndpointReturnsSuccessAsync(string endpoint)
    {
        var response = await _client.GetAsync(endpoint);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
