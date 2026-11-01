namespace DataAccessLayer.Models;

public sealed class UrlopDayModel
{
    public int Id { get; set; }
    public Guid RequestId { get; set; }
    public int ShopId { get; set; }
    public int EmployeeId { get; set; }
    public DateOnly Date { get; set; }
    public string Status { get; set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public int? CreatedByManagerId { get; set; }
    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public int? ReviewedByManagerId { get; set; }
}
