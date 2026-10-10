namespace WebApi.Contracts.Employees;

/// <summary>
/// API representation of one employee returned to clients.
/// </summary>
public sealed class EmployeeDto
{
    /// <summary>
    /// Persistent employee identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Employee given name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Employee family name.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Optional contact phone number.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Optional contact email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Optional username used for application login.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Whether a login account already exists for this employee.
    /// </summary>
    public bool HasLoginAccount { get; set; }

    /// <summary>
    /// Whether the employee was active recently enough to be considered online.
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Most recent successful sign-in timestamp in UTC.
    /// </summary>
    public DateTimeOffset? LastLoginAtUtc { get; set; }
}
