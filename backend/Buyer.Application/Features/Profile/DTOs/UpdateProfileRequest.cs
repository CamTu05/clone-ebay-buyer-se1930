using System.Text.Json;
using System.Text.Json.Serialization;

namespace Buyer.Application.Features.Profile.DTOs;

// Only these fields may be edited with PATCH /api/me.
public sealed class UpdateProfileRequest
{
    public string? Username { get; init; }
    public string? AvatarUrl { get; init; }
    public bool ClearAvatar { get; init; }

    // Reject unknown JSON fields instead of silently ignoring privilege escalation attempts.
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalFields { get; set; }
}
