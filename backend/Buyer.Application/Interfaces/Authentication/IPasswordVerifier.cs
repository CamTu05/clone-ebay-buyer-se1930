namespace Buyer.Application.Interfaces.Authentication;

public interface IPasswordVerifier
{
    PasswordVerificationResult Verify(AuthUser user, string hashedPassword, string providedPassword);
    void PerformDummyVerification(string providedPassword);
    string Hash(AuthUser user, string password);
}
