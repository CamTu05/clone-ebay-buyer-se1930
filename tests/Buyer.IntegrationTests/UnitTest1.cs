using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Buyer.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
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
