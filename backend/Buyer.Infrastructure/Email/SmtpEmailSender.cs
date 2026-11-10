using System.Net;
using System.Net.Mail;
using Buyer.Application.Interfaces.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Buyer.Infrastructure.Email;

public sealed class SmtpEmailSender(
    IOptions<SmtpOptions> options,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpOptions _settings = options.Value;

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.SenderEmail) || string.IsNullOrWhiteSpace(_settings.Password))
        {
            logger.LogWarning("SMTP is not fully configured. Email to {To} was not sent via network.", toEmail);
            return;
        }

        try
        {
            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password)
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message, cancellationToken);
            logger.LogInformation("Email successfully sent to {To} with subject '{Subject}'", toEmail, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {To}", toEmail);
            throw;
        }
    }
}
