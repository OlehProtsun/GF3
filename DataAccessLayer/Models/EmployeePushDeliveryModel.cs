namespace DataAccessLayer.Models;

public sealed class EmployeePushDeliveryModel
{
    public long Id { get; set; }
    public int SubscriptionId { get; set; }
    public string NotificationId { get; set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset NextAttemptAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? LeaseUntilUtc { get; set; }
    public string? LeaseToken { get; set; }
    public DateTimeOffset? FinalizedAtUtc { get; set; }
    public string? Outcome { get; set; }
    public EmployeePushSubscriptionModel Subscription { get; set; } = null!;
}
