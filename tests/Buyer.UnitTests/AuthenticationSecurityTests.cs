using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Buyer.Application.Authentication;
using Buyer.Infrastructure.Authentication;
using Buyer.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ApplicationPasswordVerificationResult = Buyer.Application.Authentication.PasswordVerificationResult;

namespace Buyer.UnitTests;

public sealed class AuthenticationSecurityTests
{
    private static readonly AuthUser User = new(
        42, "buyer@example.com", "buyer", null, "BUYER", "ACTIVE", true, false);

    [Fact]
    public void PasswordVerifierAcceptsCorrectPasswordAndRejectsIncorrectPassword()
    {
        var verifier = new PasswordVerifier(new PasswordHasher<User>());
        var hash = verifier.Hash(User, "correct-password");

        Assert.Equal(ApplicationPasswordVerificationResult.Success, verifier.Verify(User, hash, "correct-password"));
        Assert.Equal(ApplicationPasswordVerificationResult.Failed, verifier.Verify(User, hash, "incorrect-password"));
    }

    [Fact]
    public void PasswordVerifierRequestsRehashForLegacyIdentityHash()
    {
        var legacyHasher = new PasswordHasher<User>(Options.Create(new PasswordHasherOptions
        {
            CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2
        }));
        var entity = new User();
        var legacyHash = legacyHasher.HashPassword(entity, "correct-password");
        var verifier = new PasswordVerifier(new PasswordHasher<User>());

        Assert.Equal(
            ApplicationPasswordVerificationResult.SuccessRehashNeeded,
            verifier.Verify(User, legacyHash, "correct-password"));
    }

    [Fact]
    public void JwtValidatesWithConfiguredIssuerAudienceKeyAndHs256()
    {
        var options = CreateJwtOptions("0123456789abcdef0123456789abcdef");
        var token = new JwtAccessTokenGenerator(Options.Create(options), new FixedClock(DateTime.UtcNow))
            .Generate(User);
        var handler = new JwtSecurityTokenHandler();

        var principal = handler.ValidateToken(token.Value, ValidationParameters(options), out var validatedToken);

        Assert.Equal(User.Id.ToString(), principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal(SecurityAlgorithms.HmacSha256, ((JwtSecurityToken)validatedToken).Header.Alg);
    }

    [Fact]
    public void JwtFailsValidationWithInvalidKey()
    {
        var options = CreateJwtOptions("0123456789abcdef0123456789abcdef");
        var token = new JwtAccessTokenGenerator(Options.Create(options), new FixedClock(DateTime.UtcNow))
            .Generate(User);
        var wrongOptions = CreateJwtOptions("abcdef0123456789abcdef0123456789");

        Assert.ThrowsAny<SecurityTokenValidationException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(token.Value, ValidationParameters(wrongOptions), out _));
    }

    [Fact]
    public void JwtFailsValidationWhenExpired()
    {
        var options = CreateJwtOptions("0123456789abcdef0123456789abcdef");
        var token = new JwtAccessTokenGenerator(
            Options.Create(options),
            new FixedClock(DateTime.UtcNow.AddMinutes(-options.DurationMinutes - 1)))
            .Generate(User);

        Assert.Throws<SecurityTokenExpiredException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(token.Value, ValidationParameters(options), out _));
    }

    private static JwtOptions CreateJwtOptions(string signingKey) => new()
    {
        Issuer = "buyer-tests",
        Audience = "buyer-tests-client",
        SigningKey = signingKey,
        DurationMinutes = 15
    };

    private static TokenValidationParameters ValidationParameters(JwtOptions options) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = options.Issuer,
        ValidateAudience = true,
        ValidAudience = options.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
        RequireSignedTokens = true,
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow => utcNow;
    }
}
