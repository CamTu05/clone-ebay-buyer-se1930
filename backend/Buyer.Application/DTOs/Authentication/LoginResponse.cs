using System.ComponentModel.DataAnnotations;

namespace Buyer.Application.DTOs.Authentication;

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAt,
    AuthenticatedUser User);
