using System.ComponentModel.DataAnnotations;

namespace Buyer.Application.DTOs.Authentication;

public sealed record RegisterRequest(
    [param: Required, EmailAddress, StringLength(255)]
    string? Email,

    [param: Required, MinLength(6), MaxLength(100)]
    string? Password,

    [param: Required, StringLength(100, MinimumLength = 2)]
    string? Username);
