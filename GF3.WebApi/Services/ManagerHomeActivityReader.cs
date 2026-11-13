using System.Globalization;
using System.Text.Json;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.ManagerHome;

namespace WebApi.Services;

public static class ManagerHomeActivityReader
{
    public static async Task<ManagerNavBadgesDto> ReadNavBadgesAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var counts = await db.Database.SqlQueryRaw<NavBadgeCountsRow>(CanonicalSql + """
            SELECT COALESCE(SUM(RequiresApproval = 1), 0) AS TotalPending,
                   COALESCE(SUM(RequiresApproval = 1 AND TargetType = 'graph'), 0) AS GraphPending,
                   COALESCE(SUM(RequiresApproval = 1 AND TargetType = 'urlop'), 0) AS UrlopPending
            FROM events
            """).AsNoTracking().SingleAsync(cancellationToken);
        return new(counts.TotalPending, counts.GraphPending, counts.UrlopPending);
    }

    private sealed class NavBadgeCountsRow
    {
        public int TotalPending { get; set; }
        public int GraphPending { get; set; }
        public int UrlopPending { get; set; }
    }

    public static async Task<ManagerHomeActivityPageDto> ReadAsync(AppDbContext db, int year, int month,
        string tab, int offset, int limit, CancellationToken cancellationToken)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");
        var localStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var start = localStart == DateTime.MinValue ? DateTime.MinValue : TimeZoneInfo.ConvertTimeToUtc(localStart, zone);
        DateTime? end = year == 9999 && month == 12 ? null : TimeZoneInfo.ConvertTimeToUtc(localStart.AddMonths(1), zone);
        object[] Parameters() => [
            new SqliteParameter("@start", start.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture)),
            new SqliteParameter("@end", (object?)end?.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture) ?? DBNull.Value),
            new SqliteParameter("@approvals", tab == "approvals"),
            new SqliteParameter("@offset", offset), new SqliteParameter("@limit", limit + 1)];

        var counts = (await db.Database.SqlQueryRaw<ActivityCounts>(CanonicalSql + """
            SELECT COALESCE(SUM(CASE WHEN OccurredAt >= @start AND (@end IS NULL OR OccurredAt < @end) THEN 1 ELSE 0 END), 0) AS AllActivityCount,
                   COALESCE(SUM(RequiresApproval), 0) AS RequiresApprovalCount
            FROM events
            """, Parameters()).AsNoTracking().ToListAsync(cancellationToken)).Single();
        var rows = await db.Database.SqlQueryRaw<ActivityRow>(CanonicalSql + """
            SELECT Id, Kind, OccurredAt, Status, RequiresApproval, TargetType, Payload FROM events
            WHERE (@approvals = 1 AND RequiresApproval = 1)
               OR (@approvals = 0 AND OccurredAt >= @start AND (@end IS NULL OR OccurredAt < @end))
            ORDER BY OccurredAt DESC, Id COLLATE BINARY ASC LIMIT @limit OFFSET @offset
            """, Parameters()).AsNoTracking().ToListAsync(cancellationToken);
        var items = rows.Take(limit).Select(ToDto).ToArray();
        return new(items, counts.AllActivityCount, counts.RequiresApprovalCount, rows.Count > limit, offset + items.Length);
    }

    private static ManagerHomeActivityItemDto ToDto(ActivityRow row)
    {
        using var document = JsonDocument.Parse(row.Payload);
        var data = document.RootElement;
        string? Text(string key) => data.TryGetProperty(key, out var value) && value.ValueKind != JsonValueKind.Null ? value.GetString() : null;
        int? Number(string key) => data.TryGetProperty(key, out var value) && value.ValueKind != JsonValueKind.Null ? value.GetInt32() : null;
        DateOnly? Day(string prefix = "") => Number(prefix + "Year") is { } y && Number(prefix + "Month") is { } m && Number(prefix + "Day") is { } d
            && y is >= 1 and <= 9999 && m is >= 1 and <= 12 && d >= 1 && d <= DateTime.DaysInMonth(y, m) ? new(y, m, d) : null;
        var employeeId = Number("EmployeeId");
        var name = Text("EmployeeName")?.Trim();
        var vacationDays = Text("VacationDays")?.Split(',').Select(value => DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture))
            .Distinct().Order().ToArray();
        var shiftDate = Day();
        return new()
        {
            Id = row.Id, Kind = row.Kind,
            OccurredAtUtc = new DateTimeOffset(DateTime.ParseExact(row.OccurredAt, "yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal)),
            EmployeeName = string.IsNullOrWhiteSpace(name) ? employeeId?.ToString(CultureInfo.InvariantCulture) ?? "—" : name,
            CounterpartyName = Text("CounterpartyName"), Status = row.Status, RequiresApproval = row.RequiresApproval,
            TargetType = row.TargetType, ContainerId = Number("ContainerId"), GraphId = Number("GraphId"),
            AvailabilityGroupId = Number("AvailabilityGroupId"), ShopId = Number("ShopId"), EmployeeId = employeeId,
            ScheduleName = Text("ScheduleName"), ShopName = Text("ShopName"), ShiftDate = shiftDate,
            FromTime = Text("FromTime"), ToTime = Text("ToTime"), ReturnShiftDate = Day("Return"),
            ReturnFromTime = Text("ReturnFromTime"), ReturnToTime = Text("ReturnToTime"), VacationDays = vacationDays,
            Year = vacationDays?.FirstOrDefault().Year ?? Number("Year"), Month = vacationDays?.FirstOrDefault().Month ?? Number("Month")
        };
    }

    private sealed class ActivityCounts
    {
        public int AllActivityCount { get; set; }
        public int RequiresApprovalCount { get; set; }
    }

    private sealed class ActivityRow
    {
        public string Id { get; set; } = "";
        public string Kind { get; set; } = "";
        public string OccurredAt { get; set; } = "";
        public string Status { get; set; } = "";
        public bool RequiresApproval { get; set; }
        public string TargetType { get; set; } = "";
        public string Payload { get; set; } = "";
    }

    // SQLite's EF provider cannot order DateTimeOffset. Normalize the stored offset and retain all
    // seven fractional digits here; julianday/strftime fractional seconds would round real events.
    private const string CanonicalSql = """
        WITH swaps AS (
            SELECT r.id, r.created_at_utc, r.cancelled_at_utc, lower(r.status) AS status,
                json_object('EmployeeId', r.from_employee_id, 'EmployeeName', e.first_name || ' ' || e.last_name,
                    'CounterpartyName', COALESCE(a.first_name || ' ' || a.last_name, t.first_name || ' ' || t.last_name),
                    'ContainerId', s.container_id, 'GraphId', s.id, 'ScheduleName', s.name, 'ShopName', p.name,
                    'Year', s.year, 'Month', s.month, 'Day', slot.day_of_month,
                    'FromTime', COALESCE(r.offered_from_time, slot.from_time), 'ToTime', COALESCE(r.offered_to_time, slot.to_time),
                    'ReturnYear', rs.year, 'ReturnMonth', rs.month, 'ReturnDay', rslot.day_of_month,
                    'ReturnFromTime', r.return_from_time, 'ReturnToTime', r.return_to_time) AS payload
            FROM shift_swap_request r JOIN schedule s ON s.id = r.schedule_id
            JOIN container c ON c.id = s.container_id JOIN shop p ON p.id = s.shop_id
            LEFT JOIN employee e ON e.id = r.from_employee_id LEFT JOIN employee t ON t.id = r.target_employee_id
            LEFT JOIN employee a ON a.id = r.accepted_by_employee_id LEFT JOIN schedule_slot slot ON slot.id = r.schedule_slot_id
            LEFT JOIN schedule rs ON rs.id = r.return_schedule_id LEFT JOIN schedule_slot rslot ON rslot.id = r.return_schedule_slot_id
            WHERE s.id > 0 AND c.id > 0
        ), history AS (
            SELECT h.source_shift_swap_request_id AS id, h.created_at_utc, h.accepted_at_utc,
                NOT EXISTS(SELECT 1 FROM shift_swap_request r WHERE r.id = h.source_shift_swap_request_id) AS history_only,
                json_object('EmployeeId', h.from_employee_id, 'EmployeeName', h.from_employee_name,
                    'CounterpartyName', h.accepted_by_employee_name, 'ContainerId', s.container_id, 'GraphId', s.id,
                    'ScheduleName', h.schedule_name, 'ShopName', h.shop_name, 'Year', h.year, 'Month', h.month, 'Day', h.day_of_month,
                    'FromTime', h.from_time, 'ToTime', h.to_time, 'ReturnYear', h.return_year, 'ReturnMonth', h.return_month,
                    'ReturnDay', h.return_day_of_month, 'ReturnFromTime', h.return_from_time, 'ReturnToTime', h.return_to_time) AS payload
            FROM shift_swap_history h JOIN schedule s ON s.id = h.schedule_id
            JOIN container c ON c.id = s.container_id JOIN shop p ON p.id = s.shop_id WHERE s.id > 0 AND c.id > 0
        ), corrections AS (
            SELECT r.id, r.created_at_utc, r.reviewed_at_utc, lower(r.status) AS status, r.schedule_slot_id,
                json_object('EmployeeId', r.employee_id, 'EmployeeName', e.first_name || ' ' || e.last_name,
                    'ContainerId', s.container_id, 'GraphId', s.id, 'ScheduleName', s.name, 'ShopName', p.name,
                    'Year', s.year, 'Month', s.month, 'Day', r.day_of_month, 'FromTime', r.original_from_time,
                    'ToTime', r.original_to_time, 'ReturnYear', s.year, 'ReturnMonth', s.month, 'ReturnDay', r.day_of_month,
                    'ReturnFromTime', r.requested_from_time, 'ReturnToTime', r.requested_to_time) AS payload
            FROM shift_correction_request r JOIN schedule s ON s.id = r.schedule_id
            JOIN container c ON c.id = s.container_id JOIN shop p ON p.id = s.shop_id
            LEFT JOIN employee e ON e.id = r.employee_id WHERE s.id > 0 AND c.id > 0
        ), approvals AS (
            SELECT r.id, r.created_at_utc, r.reviewed_at_utc, r.status,
                json_object('EmployeeId', r.employee_id, 'EmployeeName', e.first_name || ' ' || e.last_name,
                    'CounterpartyName', t.first_name || ' ' || t.last_name,
                    'ContainerId', s.container_id, 'GraphId', s.id, 'ScheduleName', s.name, 'ShopName', p.name,
                    'Year', s.year, 'Month', s.month, 'Day', r.original_day_of_month, 'FromTime', r.from_time, 'ToTime', r.to_time) AS payload
            FROM contract_shift_approval_request r JOIN schedule s ON s.id = r.schedule_id
            JOIN container c ON c.id = s.container_id JOIN shop p ON p.id = s.shop_id
            LEFT JOIN employee e ON e.id = r.employee_id LEFT JOIN employee t ON t.id = r.target_employee_id
            WHERE s.id > 0 AND c.id > 0 AND r.kind IN ('claim', 'below_minimum_give')
        ), vacations AS (
            SELECT r.request_id AS id, MIN(r.created_at_utc) AS created_at_utc, MAX(r.reviewed_at_utc) AS reviewed_at_utc,
                CASE WHEN SUM(r.status = 'pending') > 0 THEN 'pending'
                     WHEN SUM(r.status = 'approved') > 0 THEN 'approved' ELSE 'rejected' END AS status,
                MAX(r.created_by_manager_id) AS manager_id,
                json_object('EmployeeId', r.employee_id, 'EmployeeName', e.first_name || ' ' || e.last_name,
                    'ShopId', p.id, 'ShopName', p.name, 'VacationDays', group_concat(DISTINCT r.date)) AS payload
            FROM urlop_day r JOIN shop p ON p.id = r.shop_id LEFT JOIN employee e ON e.id = r.employee_id
            WHERE p.id > 0 GROUP BY r.request_id
        ), availability AS (
            SELECT m.id, m.employee_last_modified_at_utc AS occurred_at,
                json_object('EmployeeId', m.employee_id, 'EmployeeName', e.first_name || ' ' || e.last_name,
                    'AvailabilityGroupId', g.id, 'ScheduleName', g.name, 'Year', g.year, 'Month', g.month) AS payload
            FROM availability_group_member m JOIN availability_group g ON g.id = m.availability_group_id
            LEFT JOIN employee e ON e.id = m.employee_id WHERE g.id > 0 AND m.employee_last_modified_at_utc IS NOT NULL
        ), transitions AS (
            SELECT 'swap-created:' || id AS Id, 'swap_created' AS Kind, created_at_utc AS stamp, status AS Status,
                0 AS RequiresApproval, 'graph' AS TargetType, payload AS Payload FROM swaps
            UNION ALL SELECT 'swap-cancelled:' || id, 'swap_cancelled', cancelled_at_utc, status, 0, 'graph', payload FROM swaps WHERE cancelled_at_utc IS NOT NULL
            UNION ALL SELECT 'swap-created:' || id, 'swap_created', created_at_utc, 'accepted', 0, 'graph', payload FROM history WHERE history_only
            UNION ALL SELECT 'swap-accepted:' || id, 'swap_accepted', accepted_at_utc, 'accepted', 0, 'graph', payload FROM history
            UNION ALL SELECT 'correction-created:' || id, CASE WHEN schedule_slot_id IS NULL THEN 'day_off_requested' ELSE 'correction_requested' END,
                created_at_utc, status, status = 'pending', 'graph', payload FROM corrections
            UNION ALL SELECT 'correction-reviewed:' || id, CASE WHEN schedule_slot_id IS NULL THEN 'day_off_reviewed' ELSE 'correction_reviewed' END,
                reviewed_at_utc, status, 0, 'graph', payload FROM corrections WHERE reviewed_at_utc IS NOT NULL AND status IN ('approved', 'rejected')
            UNION ALL SELECT 'approval-created:' || id, 'approval_requested', created_at_utc, status, status = 'pending', 'graph', payload FROM approvals
            UNION ALL SELECT 'approval-reviewed:' || id, 'approval_reviewed', reviewed_at_utc, status, 0, 'graph', payload FROM approvals WHERE reviewed_at_utc IS NOT NULL AND status IN ('approved', 'rejected')
            UNION ALL SELECT 'urlop-created:' || lower(id), CASE WHEN manager_id IS NULL THEN 'urlop_requested' ELSE 'urlop_manager_created' END,
                created_at_utc, status, status = 'pending', 'urlop', payload FROM vacations
            UNION ALL SELECT 'urlop-reviewed:' || lower(id), 'urlop_reviewed', reviewed_at_utc, status, 0, 'urlop', payload FROM vacations WHERE reviewed_at_utc IS NOT NULL AND status IN ('approved', 'rejected')
            UNION ALL SELECT 'availability-updated:' || id, 'availability_updated', occurred_at, 'updated', 0, 'availability', payload FROM availability
        ), events AS (
            SELECT Id, Kind, datetime(substr(stamp, 1, 19) || substr(stamp, -6)) || '.' ||
                substr(CASE WHEN substr(stamp, 20, 1) = '.' THEN substr(stamp, 21, length(stamp) - 26) ELSE '' END || '0000000', 1, 7) AS OccurredAt,
                Status, RequiresApproval, TargetType, Payload FROM transitions
        )
        """;
}
