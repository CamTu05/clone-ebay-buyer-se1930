using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Buyer.API.Controllers;
using Buyer.Application.Addresses;
using Buyer.Infrastructure.Data;
using Buyer.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace Buyer.IntegrationTests;

public sealed class AddressEndpointTests
{
    private sealed class Factory : WebApplicationFactory<Program>
    {
        internal const string Key = "address-tests-only-signing-key-at-least-32-characters";
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        private readonly DbContextOptions<CloneEbayDbContext> _options;
        public Factory()
        {
            _connection.Open();
            _connection.CreateFunction("sysutcdatetime", () => DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture));
            _options = new DbContextOptionsBuilder<CloneEbayDbContext>().UseSqlite(_connection).Options;
            using var db = new CloneEbayDbContext(_options);
            db.Database.EnsureCreated();
            db.Users.AddRange(new User { id = 1, username = "buyer", email = "buyer@example.test", role = "BUYER", accountStatus = "ACTIVE" },
                new User { id = 2, username = "other", email = "other@example.test", role = "BUYER", accountStatus = "ACTIVE" });
            db.SaveChanges();
        }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("BuyerSchema", "Legacy");
            builder.UseSetting("ConnectionStrings:BuyerDatabase", "Server=unused;Database=unused;Trusted_Connection=True");
            builder.UseSetting("Jwt:Issuer", "Address.Tests");
            builder.UseSetting("Jwt:Audience", "Address.Tests.Client");
            builder.UseSetting("Jwt:SigningKey", Key);
            builder.UseSetting("Jwt:DurationMinutes", "15");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<CloneEbayDbContext>();
                services.AddScoped(_ => new CloneEbayDbContext(_options));
            });
        }
        internal HttpClient Client(int? id = null, bool missingSubject = false)
        {
            var client = CreateClient();
            if (id != null || missingSubject)
            {
                Claim[] claims = missingSubject ? [] : [new(JwtRegisteredClaimNames.Sub, id!.Value.ToString())];
                var token = new JwtSecurityToken("Address.Tests", "Address.Tests.Client", claims,
                    expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)), SecurityAlgorithms.HmacSha256));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
            }
            return client;
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) _connection.Dispose();
        }
    }

    private static readonly CreateAddressRequest Input = new("Buyer", "+84 901234567", "123 Main street", "Hanoi", "VN");

    [Fact]
    public async Task LoginValidatesRecordParametersAndCanAccessAddressBook()
    {
        using var factory = new Factory();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CloneEbayDbContext>();
            var buyer = await db.Users.SingleAsync(u => u.id == 1);
            buyer.emailVerified = true;
            buyer.passwordHash = new Microsoft.AspNetCore.Identity.PasswordHasher<User>().HashPassword(buyer, "TestBuyer@2026!");
            await db.SaveChangesAsync();
        }
        using var client = factory.Client();
        var invalid = await client.PostAsJsonAsync("/api/auth/login", new { email = "invalid", password = "" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "buyer@example.test", password = "TestBuyer@2026!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<ApiEnvelope<Buyer.Application.Authentication.LoginResponse>>();
        Assert.True(body!.Success);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.Data!.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/addresses")).StatusCode);
    }

    [Fact]
    public async Task AuthenticatedBuyerCanManageAndSelectMultipleAddresses()
    {
        using var factory = new Factory();
        using var client = factory.Client(1);
        var created = await client.PostAsJsonAsync("/api/addresses", Input);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var first = (await created.Content.ReadFromJsonAsync<ApiEnvelope<AddressResponse>>())!.Data!;
        Assert.True(first.IsDefault);
        Assert.NotNull(created.Headers.Location);
        var secondResponse = await client.PostAsJsonAsync("/api/addresses", Input with { FullName = "Office" });
        var second = (await secondResponse.Content.ReadFromJsonAsync<ApiEnvelope<AddressResponse>>())!.Data!;
        Assert.False(second.IsDefault);

        var edit = await client.PatchAsJsonAsync($"/api/addresses/{second.Id}", new { street = "New street" });
        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
        Assert.Equal("Office", (await edit.Content.ReadFromJsonAsync<ApiEnvelope<AddressResponse>>())!.Data!.FullName);
        var selected = await client.GetFromJsonAsync<ApiEnvelope<AddressResponse>>($"/api/addresses/{second.Id}");
        Assert.False(selected!.Data!.IsDefault);
        var primary = await client.PatchAsync($"/api/addresses/{second.Id}/default", null);
        Assert.Equal(HttpStatusCode.OK, primary.StatusCode);
        var list = await client.GetFromJsonAsync<ApiEnvelope<AddressResponse[]>>("/api/addresses");
        Assert.Equal(2, list!.Data!.Length);
        Assert.Equal(second.Id, Assert.Single(list.Data.Where(a => a.IsDefault)).Id);
        var deleted = await client.DeleteAsync($"/api/addresses/{second.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal("", await deleted.Content.ReadAsStringAsync());
        var remaining = await client.GetFromJsonAsync<ApiEnvelope<AddressResponse[]>>("/api/addresses");
        Assert.True(Assert.Single(remaining!.Data!).IsDefault);
    }

    [Theory]
    [InlineData("GET", "/api/addresses")]
    [InlineData("GET", "/api/addresses/1")]
    [InlineData("POST", "/api/addresses")]
    [InlineData("PATCH", "/api/addresses/1")]
    [InlineData("DELETE", "/api/addresses/1")]
    [InlineData("PATCH", "/api/addresses/1/default")]
    public async Task AnonymousCallsAreUnauthorized(string method, string path)
    {
        using var factory = new Factory();
        using var client = factory.Client();
        using var request = new HttpRequestMessage(new HttpMethod(method), path) { Content = JsonContent.Create(Input) };
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task SubjectCannotBeReplacedByUserIdInRequest()
    {
        using var factory = new Factory();
        using var owner = factory.Client(1);
        using var other = factory.Client(2);
        var response = await owner.PostAsJsonAsync("/api/addresses?userId=2", new
        {
            userId = 2,
            fullName = "Buyer",
            phone = "0901234567",
            street = "Main street",
            city = "Hanoi",
            countryCode = "VN"
        });
        var address = (await response.Content.ReadFromJsonAsync<ApiEnvelope<AddressResponse>>())!.Data!;
        Assert.Empty((await other.GetFromJsonAsync<ApiEnvelope<AddressResponse[]>>("/api/addresses"))!.Data!);
        foreach (var method in new[] { "GET", "PATCH", "DELETE" })
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), $"/api/addresses/{address.Id}") { Content = JsonContent.Create(new { city = "Other city" }) };
            var result = await other.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
            Assert.Equal("ADDRESS_NOT_FOUND", (await result.Content.ReadFromJsonAsync<ApiEnvelope<object>>())!.Errors![0].Code);
        }
        Assert.Equal(HttpStatusCode.NotFound, (await other.PatchAsync($"/api/addresses/{address.Id}/default", null)).StatusCode);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("2147483648")]
    public async Task InvalidIdsReturn400(string id)
    {
        using var factory = new Factory();
        using var client = factory.Client(1);
        var response = await client.GetAsync($"/api/addresses/{id}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("ADDRESS_ID_INVALID", (await response.Content.ReadFromJsonAsync<ApiEnvelope<object>>())!.Errors![0].Code);
    }

    [Fact]
    public async Task InvalidFieldsAndMalformedJsonReturnEnvelope()
    {
        using var factory = new Factory();
        using var client = factory.Client(1);
        var invalid = await client.PostAsJsonAsync("/api/addresses", Input with { FullName = "   " });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("fullName", (await invalid.Content.ReadFromJsonAsync<ApiEnvelope<object>>())!.Errors![0].Field);
        var malformed = await client.PostAsync("/api/addresses", new StringContent("{", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
        Assert.False((await malformed.Content.ReadFromJsonAsync<ApiEnvelope<object>>())!.Success);
        using var noSubject = factory.Client(missingSubject: true);
        Assert.Equal(HttpStatusCode.Unauthorized, (await noSubject.GetAsync("/api/addresses")).StatusCode);
    }
}
