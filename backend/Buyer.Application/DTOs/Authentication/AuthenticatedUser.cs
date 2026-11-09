using System.ComponentModel.DataAnnotations;

namespace Buyer.Application.DTOs.Authentication;

public sealed record AuthenticatedUser(
    int Id,
    string Username,
    string Email,
    string Role,
    bool EmailVerified);
