using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Buyer.Application.Features.Cart;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Buyer.IntegrationTests;

public sealed class CartApiTests
{
    private const string SigningKey =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Fact]
    public async Task GetRequiresAuthentication()
    {
        using var factory = CreateFactory(new StubCartService());
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/cart")).StatusCode);
    }

    [Fact]
    public async Task GetRejectsSellerJwt()
    {
        using var factory = CreateFactory(new StubCartService());
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", Token(42, "SELLER"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/cart")).StatusCode);
    }

    [Fact]
    public async Task GetUsesAuthenticatedBuyerIdAndReturnsSubtotal()
    {
        var service = new StubCartService();
        using var factory = CreateFactory(service);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", Token(42, "BUYER"));
        var response = await client.GetAsync("/api/cart");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(42, service.LastUserId);
        Assert.Contains("\"subtotal\":200", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task PatchRejectsQuantityAboveStockWithoutReturningCart()
    {
        var service = new StubCartService { UpdateFailure = CartFailure.QuantityExceedsStock };
        using var factory = CreateFactory(service);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", Token(42, "BUYER"));
        var response = await client.PatchAsJsonAsync("/api/cart/items/5", new { quantity = 99 });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("CART_QUANTITY_EXCEEDS_STOCK", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DeleteReturnsRecalculatedEmptyCart()
    {
        var service = new StubCartService { RemoveReturnsEmpty = true };
        using var factory = CreateFactory(service);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", Token(42, "BUYER"));
        var response = await client.DeleteAsync("/api/cart/items/5");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"items\":[]", json);
        Assert.Contains("\"subtotal\":0", json);
        Assert.Equal(42, service.LastUserId);
        Assert.Equal(5, service.LastCartItemId);
    }

    private static WebApplicationFactory<Program> CreateFactory(ICartService service) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:BuyerDatabase",
                "Server=(localdb)\\MSSQLLocalDB;Database=BuyerApiTests;Trusted_Connection=True");
            builder.UseSetting("Jwt:SigningKey", SigningKey);
            builder.UseSetting("BuyerLifecycle:Enabled", "false");
            builder.UseSetting("BuyerSchema", "Legacy");
            builder.UseSetting("Jwt:Issuer", "cart-api-tests");
            builder.UseSetting("Jwt:Audience", "cart-api-tests");
            builder.UseSetting("Jwt:DurationMinutes", "15");
            builder.ConfigureTestServices(services =>
            {
                services.AddLogging(logging => logging.ClearProviders());
                services.RemoveAll<ICartService>();
                services.AddSingleton(service);
            });
        });

    private static string Token(int userId, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var token = new JwtSecurityToken(
            issuer: "cart-api-tests",
            audience: "cart-api-tests",
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            ],
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class StubCartService : ICartService
    {
        private static readonly CartResponse Cart = new(1,
            [new(5, 11, 7, "Camera", null, new(2, "seller", null),
                100m, 2, 200m, 5, true, null)], 200m, "VND", true);
        public int LastUserId { get; private set; }
        public int LastCartItemId { get; private set; }
        public CartFailure UpdateFailure { get; init; }
        public bool RemoveReturnsEmpty { get; init; }

        public Task<CartResponse> GetAsync(int userId, CancellationToken cancellationToken = default)
        {
            LastUserId = userId;
            return Task.FromResult(Cart);
        }

        public Task<CartResult> UpdateQuantityAsync(int userId, int cartItemId, int quantity,
            CancellationToken cancellationToken = default)
        {
            LastUserId = userId;
            LastCartItemId = cartItemId;
            return Task.FromResult(UpdateFailure == CartFailure.None
                ? CartResult.Success(Cart)
                : CartResult.Failed(UpdateFailure));
        }

        public Task<CartResult> RemoveAsync(int userId, int cartItemId,
            CancellationToken cancellationToken = default)
        {
            LastUserId = userId;
            LastCartItemId = cartItemId;
            return Task.FromResult(CartResult.Success(RemoveReturnsEmpty
                ? new CartResponse(null, [], 0m, "VND", false)
                : Cart));
        }
    }
}
