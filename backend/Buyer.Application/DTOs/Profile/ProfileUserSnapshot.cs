namespace Buyer.Application.DTOs.Profile;

public sealed record ProfileUserSnapshot(
    int Id,
    string Username,
    string Email,
    string? PhoneNumber,
    string? AvatarUrl,
    bool EmailVerified,
    string Role,
    string AccountStatus);
