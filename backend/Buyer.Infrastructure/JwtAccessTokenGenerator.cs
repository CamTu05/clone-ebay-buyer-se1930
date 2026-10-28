using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Buyer.Application.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Buyer.Infrastructure.Authentication;

public sealed class JwtAccessTokenGenerator(IOptions<JwtOptions> options, IClock clock) : IAccessTokenGenerator
{
    public AccessToken Generate(AuthUser user)
    {
        var settings = options.Value;
        var expiresAtUtc = clock.UtcNow.AddMinutes(settings.DurationMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            settings.Issuer,
            settings.Audience,
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ],
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
