using System.ComponentModel.DataAnnotations;
using BusinessLogicLayer.Contracts.Urlop;

namespace WebApi.Contracts.Urlop;

public sealed record UrlopShopDto(int Id, string Name);
public sealed record UrlopMonthDto(int ShopId, int Year, int Month, bool IsOpen);
public sealed record ApprovedUrlopDayDto(int ShopId, int EmployeeId, DateOnly Date, string EmployeeName = "");
public sealed record UrlopRequestDto(Guid RequestId, int EmployeeId, string EmployeeName, int ShopId,
    int Year, int Month, IReadOnlyList<int> Days, string Status, DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReviewedAtUtc, int? CreatedByManagerId, int? ReviewedByManagerId);
public sealed record UrlopCalendarMonthDto(int ShopId, int Year, int Month, bool IsOpen,
    IReadOnlyList<UrlopRequestDto> Requests, IReadOnlyList<ApprovedUrlopDayDto> Approved);
public sealed record UrlopEmployeeCalendarDto(IReadOnlyList<UrlopShopDto> Shops, IReadOnlyList<UrlopCalendarMonthDto> Months);
public sealed record UrlopManagerMonthDto(int ShopId, string ShopName, int Year, int Month, bool IsOpen,
    IReadOnlyList<UrlopRequestDto> Requests, IReadOnlyList<ApprovedUrlopDayDto> ApprovedDays);

public sealed record SubmitUrlopDto([Range(1, int.MaxValue)] int ShopId,
    [Range(1, 9999)] int Year, [Range(1, 12)] int Month, [Required, MinLength(1)] int[] Days);
public sealed record CreateUrlopDto([Range(1, int.MaxValue)] int ShopId, [Range(1, int.MaxValue)] int EmployeeId,
    [Range(1, 9999)] int Year, [Range(1, 12)] int Month, [Required, MinLength(1)] int[] Days);
public sealed record SetUrlopMonthDto([Range(1, int.MaxValue)] int ShopId,
    [Range(1, 9999)] int Year, [Range(1, 12)] int Month, bool IsOpen);
public sealed record ReplaceUrlopDaysDto([Required, MinLength(1)] int[] Days);

internal static class UrlopDtoMapping
{
    public static UrlopRequestDto ToDto(this UrlopRequest x) => new(x.RequestId, x.EmployeeId, x.EmployeeName, x.ShopId,
        x.Year, x.Month, x.Days, x.Status, x.CreatedAtUtc, x.ReviewedAtUtc, x.CreatedByManagerId, x.ReviewedByManagerId);
    public static ApprovedUrlopDayDto ToDto(this ApprovedUrlopDay x) => new(x.ShopId, x.EmployeeId, x.Date, x.EmployeeName);
    public static UrlopMonthDto ToDto(this UrlopMonth x) => new(x.ShopId, x.Year, x.Month, x.IsOpen);
    public static UrlopEmployeeCalendarDto ToDto(this UrlopEmployeeCalendar x) => new(
        x.Shops.Select(s => new UrlopShopDto(s.Id, s.Name)).ToArray(),
        x.Months.Select(m => new UrlopCalendarMonthDto(m.ShopId, m.Year, m.Month, m.IsOpen,
            m.Requests.Select(ToDto).ToArray(), m.Approved.Select(ToDto).ToArray())).ToArray());
    public static UrlopManagerMonthDto ToDto(this UrlopManagerMonth x) => new(x.ShopId, x.ShopName, x.Year, x.Month,
        x.IsOpen, x.Requests.Select(ToDto).ToArray(), x.ApprovedDays.Select(ToDto).ToArray());
}
