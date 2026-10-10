using System.Net;
using System.Net.Mail;
using BusinessLogicLayer.Common;
using BusinessLogicLayer.Services.Abstractions;
using Microsoft.Extensions.Options;
using WebApi.Options;

namespace WebApi.Infrastructure;

/// <summary>
/// SMTP-backed email sender used for employee password recovery.
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpEmailOptions _options;

    public SmtpEmailSender(IOptions<SmtpEmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAsync(
        string toEmail,
        string? toName,
        string subject,
        string textBody,
        CancellationToken ct = default)
    {
        var host = _options.Host.Trim();
        var fromAddress = _options.FromAddress.Trim();

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress))
        {
            throw new ValidationException("Email delivery is not configured yet. Add SMTP settings before using password recovery.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress, _options.FromDisplayName),
            Subject = subject,
            Body = textBody,
            IsBodyHtml = false,
        };
        message.To.Add(new MailAddress(toEmail.Trim(), toName));

        using var client = new SmtpClient(host, _options.Port <= 0 ? 587 : _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };

        if (!string.IsNullOrWhiteSpace(_options.UserName))
        {
            client.Credentials = new NetworkCredential(_options.UserName.Trim(), _options.Password ?? string.Empty);
        }

        await client.SendMailAsync(message).WaitAsync(ct).ConfigureAwait(false);
    }
}
