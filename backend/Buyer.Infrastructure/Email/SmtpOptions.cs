namespace Buyer.Infrastructure.Email;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; init; } = "smtp.gmail.com";
    public int Port { get; init; } = 587;
    public bool EnableSsl { get; init; } = true;
    public string SenderEmail { get; init; } = string.Empty;
    public string SenderName { get; init; } = "eBay Clone";
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
