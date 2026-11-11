namespace Buyer.Application.DTOs.Authentication;

public sealed record AuthUser(
    int Id,
    string Email,
    string Username,
    string? PasswordHash,
    string Role,
    string AccountStatus,
    bool EmailVerified,
    bool PhoneVerified = false,
    int AuthVersion = 0);
