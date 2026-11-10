namespace Buyer.Application.DTOs.Authentication;

public sealed record RegisterResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAt,
    RegisteredUser User);
