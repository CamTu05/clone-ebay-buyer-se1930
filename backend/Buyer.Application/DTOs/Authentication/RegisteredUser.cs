namespace Buyer.Application.DTOs.Authentication;

public sealed record RegisteredUser(
    int Id,
    string Username,
    string Email,
    string Role);
