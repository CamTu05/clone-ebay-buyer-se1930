namespace Buyer.Application.Interfaces.Authentication;

public interface IAccessTokenGenerator
{
    AccessToken Generate(AuthUser user);
}
