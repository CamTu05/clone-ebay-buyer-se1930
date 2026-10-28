using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Buyer.Application.Features.Profile;
using Buyer.Application.Features.Profile.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace Buyer.IntegrationTests;

public sealed class ProfileApiTests
{
    private const string SigningKey =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Fact]
    public async Task GetRequiresAuthentication()
    {
        using var factory = CreateFactory(new StubProfiles());
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRejectsSellerJwt()
    {
        using var factory = CreateFactory(new StubProfiles());
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", Token(42, "SELLER"));
        var response = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPassesAuthenticatedUserIdToService()
    {
        var service = new StubProfiles();
        using var factory = CreateFactory(service);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", Token(42, "BUYER"));
        var response = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(42, service.LastId);
    }

    [Fact]
    public async Task PatchMapsUsernameTakenTo409()
    {
        var service = new StubProfiles { RejectUpdate = true };
        using var factory = CreateFactory(service);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", Token(42, "BUYER"));
        var response = await client.PatchAsync("/api/me", new StringContent(
            "{\"username\":\"mybuyer42\"}", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(42, service.LastId);
        Assert.Contains("PROFILE_USERNAME_TAKEN", await response.Content.ReadAsStringAsync());
    }

    private static WebApplicationFactory<Program> CreateFactory(
    IProfileService profiles) =>
    new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseSetting(
            "ConnectionStrings:BuyerDatabase",
            "Server=localhost;Database=BuyerApiTests;" +
            "User Id=test_user;Password=TestOnly_Password123!;" +
            "TrustServerCertificate=True");

        builder.UseSetting("Jwt:SigningKey", SigningKey);
        builder.UseSetting("Jwt:Issuer", "profile-api-tests");
        builder.UseSetting("Jwt:Audience", "profile-api-tests");
        builder.UseSetting("Jwt:DurationMinutes", "15");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IProfileService>();
            services.AddSingleton(profiles);
        });
    });
    private static string Token(int userId, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var token = new JwtSecurityToken(
            issuer: "profile-api-tests",
            audience: "profile-api-tests",
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            },
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(key,
                SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class StubProfiles : IProfileService
    {
        public int LastId { get; private set; }
        public bool RejectUpdate { get; set; }

        public Task<ProfileServiceResult> GetProfileAsync(int authenticatedUserId,
            CancellationToken cancellationToken = default)
        {
            LastId = authenticatedUserId;
            return Task.FromResult(ProfileServiceResult.Success(
                new ProfileResponse(authenticatedUserId, "mybuyer42",
                    "buyer@example.com", null, null, true, false)));
        }

        public Task<ProfileServiceResult> UpdateProfileAsync(int authenticatedUserId,
            UpdateProfileRequest request,
            CancellationToken cancellationToken = default)
        {
            LastId = authenticatedUserId;
            return Task.FromResult(RejectUpdate
                ? ProfileServiceResult.Failed(ProfileFailure.UsernameTaken,
                    new ProfileFieldError("PROFILE_USERNAME_TAKEN", "username"))
                : ProfileServiceResult.Success(new ProfileResponse(
                    authenticatedUserId, request.Username ?? "mybuyer42",
                    "buyer@example.com", null, request.AvatarUrl, true, false)));
        }
    }
}