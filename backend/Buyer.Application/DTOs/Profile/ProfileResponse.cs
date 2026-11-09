namespace Buyer.Application.DTOs.Profile;

public sealed record ProfileResponse(
    int Id,
    string Username,
    string Email,
    string? PhoneNumber,
    string? AvatarUrl,
    bool EmailVerified);
