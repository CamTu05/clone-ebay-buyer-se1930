namespace Buyer.Application.DTOs.Authentication;

public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);
