namespace Buyer.Application.Features.Profile.DTOs;

public sealed record ProfileResponse(
    int Id,
    string Username,
    string Email,
    string? PhoneNumber,
    string? AvatarUrl,
    bool EmailVerified,
    bool PhoneVerified);
