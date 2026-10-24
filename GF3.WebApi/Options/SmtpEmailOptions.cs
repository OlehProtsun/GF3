namespace WebApi.Options;

/// <summary>
/// SMTP settings used to send password-recovery emails.
/// </summary>
public sealed class SmtpEmailOptions
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public string FromAddress { get; set; } = string.Empty;

    public string? FromDisplayName { get; set; }
}
