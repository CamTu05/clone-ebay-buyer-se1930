namespace Buyer.Application.DTOs.Profile;

public sealed record ProfileChanges(
    int UserId,
    string ExpectedUsername,
    string? NewUsername,
    string? NewAvatarUrl,
    bool ClearAvatar);
