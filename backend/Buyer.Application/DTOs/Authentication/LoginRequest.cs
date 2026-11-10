using System.ComponentModel.DataAnnotations;

namespace Buyer.Application.DTOs.Authentication;

public sealed record LoginRequest(
    [param: Required, EmailAddress] string? Email,
    [param: Required] string? Password);
