using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

/// <summary>
/// Login credentials for one manager account.
/// Managers are stored separately from employees because they do not participate in scheduling rows.
/// </summary>
[Table("manager_account")]
public class ManagerAccountModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("username")]
    public string Username { get; set; } = null!;

    [Required]
    [MaxLength(160)]
    [Column("display_name")]
    public string DisplayName { get; set; } = null!;

    [Required]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = null!;

    [MaxLength(254)]
    [Column("recovery_email")]
    public string? RecoveryEmail { get; set; }

    [Column("password_updated_at_utc")]
    public DateTimeOffset PasswordUpdatedAtUtc { get; set; }

    [Column("last_login_at_utc")]
    public DateTimeOffset? LastLoginAtUtc { get; set; }

    [Column("password_reset_code_hash")]
    public string? PasswordResetCodeHash { get; set; }

    [Column("password_reset_requested_at_utc")]
    public DateTimeOffset? PasswordResetRequestedAtUtc { get; set; }

    [Column("password_reset_expires_at_utc")]
    public DateTimeOffset? PasswordResetExpiresAtUtc { get; set; }

    [Column("created_at_utc")]
    public DateTimeOffset CreatedAtUtc { get; set; }

    [Column("updated_at_utc")]
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
