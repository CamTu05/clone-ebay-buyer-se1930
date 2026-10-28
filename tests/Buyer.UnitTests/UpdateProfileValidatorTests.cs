using Buyer.Application.Features.Profile;
using Buyer.Application.Features.Profile.DTOs;

namespace Buyer.UnitTests;

public sealed class UpdateProfileValidatorTests
{
    [Theory]
    [InlineData("abc")]
    [InlineData("_buyer123")]
    [InlineData("buyer__123")]
    [InlineData("12345678")]
    [InlineData("buyer ebay")]
    [InlineData("eBaySupport")]
    [InlineData("www.test.com")]
    [InlineData("buyer@domain")]
    public void RejectsInvalidUsername(string username)
    {
        var result = UpdateProfileValidator.Validate(new() { Username = username });
        Assert.Contains(result, x => x.Code == "PROFILE_USERNAME_INVALID");
    }

    [Theory]
    [InlineData("buyer_123")]
    [InlineData("My.Buyer-2026")]
    [InlineData("B*player")]
    public void AcceptsSupportedUsernames(string username)
    {
        Assert.Empty(UpdateProfileValidator.Validate(new() { Username = username }));
    }

    [Theory]
    [InlineData("http://example.com/a.png")]
    [InlineData("https://localhost/a.png")]
    [InlineData("https://user:pass@example.com/a.png")]
    [InlineData("data:image/png;base64,AA")]
    public void RejectsUnsafeAvatarInput(string avatarUrl)
    {
        Assert.Contains(UpdateProfileValidator.Validate(new() { AvatarUrl = avatarUrl }),
            x => x.Code == "PROFILE_AVATAR_URL_INVALID");
    }

    [Fact]
    public void RejectsSimultaneousClearAndReplacement()
    {
        var errors = UpdateProfileValidator.Validate(new()
        {
            ClearAvatar = true, AvatarUrl = "https://example.com/photo.png"
        });
        Assert.Contains(errors, x => x.Code == "PROFILE_AVATAR_ACTION_CONFLICT");
    }

    [Fact]
    public void RejectsUnknownFields()
    {
        var errors = UpdateProfileValidator.Validate(new()
        {
            AdditionalFields = new() { ["role"] = default }
        });
        Assert.Contains(errors, x => x.Code == "PROFILE_FIELD_NOT_EDITABLE");
    }

    [Fact]
    public void RejectsUsernameMatchingEmailLocalPart()
    {
        var errors = UpdateProfileValidator.ValidateForAccount(
            "buyername", "BuyerName@example.com");
        Assert.Contains(errors, x => x.Code == "PROFILE_USERNAME_EMAIL_LIKE");
    }
}
