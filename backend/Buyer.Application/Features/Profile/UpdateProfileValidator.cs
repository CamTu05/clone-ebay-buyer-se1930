using System.Text.RegularExpressions;
using Buyer.Application.Features.Profile.DTOs;

namespace Buyer.Application.Features.Profile;

public static partial class UpdateProfileValidator
{
    private const int MinimumUsernameLength = 6;
    private const int MaximumUsernameLength = 64;
    private const int MaximumAvatarUrlLength = 1000;

    // Project's conservative eBay-inspired subset, not a claim to reproduce
    // eBay's undisclosed protected/trademark username screening.
    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9*._-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex UsernameCharacters();

    [GeneratedRegex(@"^[0-9]+$", RegexOptions.CultureInvariant)]
    private static partial Regex DigitsOnly();

    [GeneratedRegex(@"^[0-9][0-9.-]{5,}$", RegexOptions.CultureInvariant)]
    private static partial Regex PhoneLike();

    [GeneratedRegex(@"(?:www\.|https?://|\.com\b|\.net\b|\.org\b|\.vn\b)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex WebAddressLike();

    public static IReadOnlyList<ProfileFieldError> Validate(UpdateProfileRequest? request)
    {
        var errors = new List<ProfileFieldError>();
        if (request is null)
        {
            errors.Add(new("PROFILE_REQUEST_REQUIRED", null));
            return errors;
        }

        if (request.AdditionalFields?.Count > 0)
            errors.Add(new("PROFILE_FIELD_NOT_EDITABLE", null));

        if (request.ClearAvatar && request.AvatarUrl is not null)
            errors.Add(new("PROFILE_AVATAR_ACTION_CONFLICT", "avatarUrl"));

        if (request.Username is { } username)
        {
            if (username.Length < MinimumUsernameLength ||
                username.Length > MaximumUsernameLength ||
                !UsernameCharacters().IsMatch(username) ||
                username.Contains("__", StringComparison.Ordinal) ||
                username.Contains("ebay", StringComparison.OrdinalIgnoreCase) ||
                DigitsOnly().IsMatch(username) ||
                PhoneLike().IsMatch(username) ||
                WebAddressLike().IsMatch(username))
            {
                errors.Add(new("PROFILE_USERNAME_INVALID", "username"));
            }
        }

        if (request.AvatarUrl is { } avatarUrl)
        {
            if (avatarUrl.Length > MaximumAvatarUrlLength ||
                !Uri.TryCreate(avatarUrl, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps ||
                uri.HostNameType != UriHostNameType.Dns ||
                uri.UserInfo.Length != 0 ||
                uri.Fragment.Length != 0 ||
                uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                uri.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(new("PROFILE_AVATAR_URL_INVALID", "avatarUrl"));
            }
        }

        return errors;
    }

    public static IReadOnlyList<ProfileFieldError> ValidateForAccount(
        string? requestedUsername, string accountEmail)
    {
        if (requestedUsername is null)
            return Array.Empty<ProfileFieldError>();

        // Do not allow a username identical to the user's email local-part.
        int atIndex = accountEmail.IndexOf('@');
        string emailLocalPart = atIndex > 0
            ? accountEmail[..atIndex]
            : accountEmail;

        if (string.Equals(requestedUsername, emailLocalPart,
                StringComparison.OrdinalIgnoreCase))
        {
            return new[] { new ProfileFieldError("PROFILE_USERNAME_EMAIL_LIKE", "username") };
        }

        return Array.Empty<ProfileFieldError>();
    }
}
