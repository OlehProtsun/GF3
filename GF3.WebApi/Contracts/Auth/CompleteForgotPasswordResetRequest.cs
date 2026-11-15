using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Auth;

/// <summary>
/// Anonymous password reset confirmation using the code sent to the employee recovery email.
/// </summary>
public sealed class CompleteForgotPasswordResetRequest
{
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(6)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string NewPassword { get; set; } = string.Empty;
}
