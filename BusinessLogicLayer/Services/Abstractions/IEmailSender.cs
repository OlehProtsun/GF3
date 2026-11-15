namespace BusinessLogicLayer.Services.Abstractions;

/// <summary>
/// Minimal outbound email abstraction used by password-recovery flows.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(
        string toEmail,
        string? toName,
        string subject,
        string textBody,
        CancellationToken ct = default);
}
