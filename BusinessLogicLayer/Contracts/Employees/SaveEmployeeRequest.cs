namespace BusinessLogicLayer.Contracts.Employees;

/// <summary>
/// Command-style DTO used by employee create and update flows in the business layer.
/// </summary>
public sealed class SaveEmployeeRequest
{
    /// <summary>
    /// <c>0</c> means create; a positive value means update the existing employee with that identifier.
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
}
