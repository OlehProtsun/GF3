namespace DataAccessLayer.Models;

public sealed class EmployeePushSubscriptionModel
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string Endpoint { get; set; } = null!;
    public string P256dh { get; set; } = null!;
    public string Auth { get; set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public EmployeeModel Employee { get; set; } = null!;
}
