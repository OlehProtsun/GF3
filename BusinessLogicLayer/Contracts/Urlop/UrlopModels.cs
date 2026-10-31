namespace BusinessLogicLayer.Contracts.Urlop;

public static class UrlopStatus
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
}

public sealed record UrlopShop(int Id, string Name);
public sealed record UrlopMonth(int ShopId, int Year, int Month, bool IsOpen);
public sealed record ApprovedUrlopDay(int ShopId, int EmployeeId, DateOnly Date, string EmployeeName = "");
public sealed record UrlopRequest(Guid RequestId, int EmployeeId, string EmployeeName, int ShopId,
    int Year, int Month, IReadOnlyList<int> Days, string Status, DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReviewedAtUtc, int? CreatedByManagerId, int? ReviewedByManagerId);
public sealed record UrlopCalendarMonth(int ShopId, int Year, int Month, bool IsOpen,
    IReadOnlyList<UrlopRequest> Requests, IReadOnlyList<ApprovedUrlopDay> Approved);
public sealed record UrlopEmployeeCalendar(IReadOnlyList<UrlopShop> Shops, IReadOnlyList<UrlopCalendarMonth> Months);
public sealed record UrlopManagerMonth(int ShopId, string ShopName, int Year, int Month, bool IsOpen,
    IReadOnlyList<UrlopRequest> Requests, IReadOnlyList<ApprovedUrlopDay> ApprovedDays);
