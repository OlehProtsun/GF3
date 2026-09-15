using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

/// <summary>
/// Login credentials for one employee.
/// Credentials are intentionally separated from the profile row so future auth changes do not
/// force scheduling tables to know about password storage concerns.
/// </summary>
[Table("employee_account")]
public class EmployeeAccountModel
{
    [Required]
    [MaxLength(2)]
    [Column("language")]
    public string Language { get; set; } = "en";

    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("employee_id")]
    public int EmployeeId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("username")]
    public string Username { get; set; } = null!;

    [Required]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = null!;

    [Column("password_updated_at_utc")]
    public DateTimeOffset PasswordUpdatedAtUtc { get; set; }

    [Column("last_login_at_utc")]
    public DateTimeOffset? LastLoginAtUtc { get; set; }

    [Column("last_seen_at_utc")]
    public DateTimeOffset? LastSeenAtUtc { get; set; }

    [Column("session_version")]
    public int SessionVersion { get; set; }

    [Column("password_reset_code_hash")]
    public string? PasswordResetCodeHash { get; set; }

    [Column("password_reset_requested_at_utc")]
    public DateTimeOffset? PasswordResetRequestedAtUtc { get; set; }

    [Column("password_reset_expires_at_utc")]
    public DateTimeOffset? PasswordResetExpiresAtUtc { get; set; }

    public EmployeeModel Employee { get; set; } = null!;
}
