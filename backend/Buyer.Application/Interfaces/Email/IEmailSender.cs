namespace Buyer.Application.Interfaces.Email;

public interface IEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
