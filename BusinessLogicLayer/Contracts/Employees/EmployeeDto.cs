namespace BusinessLogicLayer.Contracts.Employees;

/// <summary>
/// Business-layer DTO used to expose employee data to higher layers such as facades and API mappers.
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
    /// Optional username that can be used to sign into the application.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Whether a login account already exists for this employee.
    /// </summary>
    public bool HasLoginAccount { get; set; }

    /// <summary>
    /// Whether the employee has been active recently enough to be considered online.
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Most recent successful sign-in timestamp in UTC.
    /// </summary>
    public DateTimeOffset? LastLoginAtUtc { get; set; }
}
