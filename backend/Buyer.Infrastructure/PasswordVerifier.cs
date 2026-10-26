using Buyer.Application.Authentication;
using Buyer.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace Buyer.Infrastructure.Authentication;

public sealed class PasswordVerifier : IPasswordVerifier
{
    private static readonly User DummyUser = new();
    private readonly IPasswordHasher<User> passwordHasher;
    private readonly string dummyPasswordHash;

    public PasswordVerifier(IPasswordHasher<User> passwordHasher)
    {
        this.passwordHasher = passwordHasher;
        dummyPasswordHash = passwordHasher.HashPassword(DummyUser, Guid.NewGuid().ToString());
    }

    public Buyer.Application.Authentication.PasswordVerificationResult Verify(
        AuthUser user,
        string hashedPassword,
        string providedPassword)
    {
        var result = passwordHasher.VerifyHashedPassword(ToEntity(user), hashedPassword, providedPassword);
        return result switch
        {
            Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success => Buyer.Application.Authentication.PasswordVerificationResult.Success,
            Microsoft.AspNetCore.Identity.PasswordVerificationResult.SuccessRehashNeeded => Buyer.Application.Authentication.PasswordVerificationResult.SuccessRehashNeeded,
            _ => Buyer.Application.Authentication.PasswordVerificationResult.Failed
        };
    }

    public void PerformDummyVerification(string providedPassword) =>
        passwordHasher.VerifyHashedPassword(DummyUser, dummyPasswordHash, providedPassword);

    public string Hash(AuthUser user, string password) =>
        passwordHasher.HashPassword(ToEntity(user), password);

    private static User ToEntity(AuthUser user) => new()
    {
        id = user.Id,
        email = user.Email,
        username = user.Username,
        passwordHash = user.PasswordHash,
        role = user.Role,
        accountStatus = user.AccountStatus,
        emailVerified = user.EmailVerified,
        phoneVerified = user.PhoneVerified
    };
}
