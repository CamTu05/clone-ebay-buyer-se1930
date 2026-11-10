using Buyer.Application.DTOs.Profile;

namespace Buyer.Application.DTOs.Profile;

public sealed record ProfileFieldError(string Code, string? Field);
