namespace WebApi.Contracts.Auth;

/// <summary>
/// Response returned after a password-recovery code was sent to the configured recovery email.
/// </summary>
public sealed class PasswordResetCodeDispatchDto
{
    public string DeliveryHint { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; set; }
}
