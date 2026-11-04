using System.Security.Claims;
using System.Text.Json;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Auth;
using WebApi.Contracts.ShiftSwaps;
using WebApi.Controllers;
using WebApi.Realtime;
using WebApi.Services;

namespace GF3.Tests.Infrastructure;

internal enum SwapKind { Give, Exchange, ManagerManual }
internal enum SwapScope { Public, Private }
internal enum SwapPart { Full, Prefix, Suffix, Middle, OneMinute }
internal enum ScheduleRelation { SameSchedule, SameContainerDifferentSchedule, DifferentContainer }
internal enum DayRelation { SameDay, DifferentDay, DifferentMonth, DifferentYear }
internal sealed record ShiftSwapScenario(
    SwapKind Kind = SwapKind.Give, SwapScope Scope = SwapScope.Public,
    SwapPart SourcePart = SwapPart.Full, SwapPart ReturnPart = SwapPart.Full,
    ScheduleRelation ScheduleRelation = ScheduleRelation.SameContainerDifferentSchedule,
    DayRelation DayRelation = DayRelation.DifferentDay, int RecipientId = 2,
    bool Published = true, bool AllowSwap = true);

internal sealed record SwapFixture(int Container, int OtherContainer, int SourceSchedule, int ReturnSchedule,
    int Owner, int Target, int Third, int SourceSlot, int ReturnSlot);
internal sealed record AssignmentInterval(int ScheduleId, int Year, int Month, int Day, int EmployeeId,
    int FromMinute, int ToMinute, int SlotId)
{
    public int Minutes => ToMinute - FromMinute;
}

internal static class ShiftSwapScenarioFactory
{
    public static async Task<SwapFixture> SeedAsync(AppDbContext db, ShiftSwapScenario scenario)
    {
        var container = TestDataFactory.CreateDalContainer();
        var other = TestDataFactory.CreateDalContainer("Other");
        var shop = TestDataFactory.CreateDalShop();
        var shop2 = TestDataFactory.CreateDalShop("Second");
        var employees = Enumerable.Range(1, 3).Select(i => TestDataFactory.CreateDalEmployee($"Worker{i}", "Test", email: $"{i}@example.invalid")).ToArray();
        for (var i = 0; i < employees.Length; i++) employees[i].Id = i + 1;
        db.AddRange(container, other, shop, shop2);
        db.AddRange(employees);
        await db.SaveChangesAsync();
        var source = TestDataFactory.CreateDalSchedule(container.Id, shop.Id, "Source", 2026, 12);
        source.AllowSwap = scenario.AllowSwap;
        source.PublicationStatus = scenario.Published ? SchedulePublicationStatus.Public : SchedulePublicationStatus.Private;
        var back = scenario.ScheduleRelation == ScheduleRelation.SameSchedule ? source
            : TestDataFactory.CreateDalSchedule(scenario.ScheduleRelation == ScheduleRelation.DifferentContainer ? other.Id : container.Id,
                shop2.Id, "Return", scenario.DayRelation == DayRelation.DifferentYear ? 2027 : 2026,
                scenario.DayRelation == DayRelation.DifferentYear ? 1 : scenario.DayRelation == DayRelation.DifferentMonth ? 11 : 12);
        if (back != source) { back.AllowSwap = true; back.PublicationStatus = SchedulePublicationStatus.Public; }
        db.Add(source);
        if (back != source) db.Add(back);
        await db.SaveChangesAsync();
        var third = scenario.RecipientId == 3 ? 2 : 3;
        db.AddRange(new ScheduleEmployeeModel { ScheduleId = source.Id, EmployeeId = 1 },
            new ScheduleEmployeeModel { ScheduleId = back.Id, EmployeeId = scenario.RecipientId },
            new ScheduleEmployeeModel { ScheduleId = back.Id, EmployeeId = third });
        var offered = TestDataFactory.CreateDalSlot(source.Id, 1, 1, 1, "09:00", "17:00");
        var returned = TestDataFactory.CreateDalSlot(back.Id, scenario.DayRelation == DayRelation.SameDay ? 1 : 2, 1, scenario.RecipientId,
            scenario.DayRelation == DayRelation.SameDay ? "17:00" : "09:00", scenario.DayRelation == DayRelation.SameDay ? "23:00" : "17:00");
        db.AddRange(offered, returned);
        await db.SaveChangesAsync();
        return new(container.Id, other.Id, source.Id, back.Id, 1, scenario.RecipientId, third, offered.Id, returned.Id);
    }

    public static EmployeeShiftSwapsController EmployeeController(AppDbContext db, int employeeId,
        IScheduleEditLockService? locks = null, IRealtimeNotifier? notifier = null)
    {
        var controller = new EmployeeShiftSwapsController(db, locks ?? new ManagerEditLockService(),
            new NoopWorkflowLogService(), notifier ?? new NoopRealtimeNotifier(), new ScheduleVersionCommitService(db));
        SetUser(controller, AuthRoles.Employee, "employee_id", employeeId);
        return controller;
    }

    public static ShiftSwapLogsController ManagerController(AppDbContext db, IRealtimeNotifier? notifier = null, IManagerEditLockService? locks = null)
    {
        var controller = new ShiftSwapLogsController(db, new NoopWorkflowLogService(), notifier ?? new NoopRealtimeNotifier(), locks);
        SetUser(controller, AuthRoles.Manager, "manager_id", 1);
        return controller;
    }

    private static void SetUser(ControllerBase controller, string role, string claim, int id)
        => controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "matrix"),
                new Claim(ClaimTypes.Role, role), new Claim(claim, id.ToString())], JwtAuthenticationDefaults.SchemeName)) } };

    public static CreateEmployeeShiftSwapRequest Request(SwapFixture f, ShiftSwapScenario scenario)
    {
        var source = Period(scenario.SourcePart);
        var back = Period(scenario.ReturnPart);
        return new() { RequestKind = scenario.Kind == SwapKind.Exchange ? "exchange" : "give",
            ScheduleId = f.SourceSchedule, ScheduleSlotId = f.SourceSlot, FromTime = source.From, ToTime = source.To,
            TargetEmployeeId = scenario.Scope == SwapScope.Private || scenario.Kind == SwapKind.Exchange ? f.Target : null,
            ReturnScheduleId = scenario.Kind == SwapKind.Exchange ? f.ReturnSchedule : null,
            ReturnScheduleSlotId = scenario.Kind == SwapKind.Exchange ? f.ReturnSlot : null,
            ReturnFromTime = scenario.Kind == SwapKind.Exchange ? back.From : null,
            ReturnToTime = scenario.Kind == SwapKind.Exchange ? back.To : null };
    }

    public static (string From, string To) Period(SwapPart part) => part switch {
        SwapPart.Prefix => ("09:00", "13:00"), SwapPart.Suffix => ("13:00", "17:00"),
        SwapPart.Middle => ("11:00", "15:00"), SwapPart.OneMinute => ("12:01", "12:02"), _ => ("09:00", "17:00") };
    public static int Minute(string value) => int.Parse(value.AsSpan(0, 2)) * 60 + int.Parse(value.AsSpan(3, 2));

    public static async Task<List<AssignmentInterval>> ReadAssignmentIntervalsAsync(AppDbContext db)
    {
        var slots = await db.ScheduleSlots.AsNoTracking().Include(s => s.Schedule).Where(s => s.EmployeeId != null).ToListAsync();
        return slots.Select(s => new AssignmentInterval(s.ScheduleId, s.Schedule.Year, s.Schedule.Month, s.DayOfMonth,
            s.EmployeeId!.Value, Minute(s.FromTime), Minute(s.ToTime), s.Id))
            .OrderBy(s => s.EmployeeId).ThenBy(s => s.Year).ThenBy(s => s.Month).ThenBy(s => s.Day).ThenBy(s => s.FromMinute).ThenBy(s => s.ScheduleId).ToList();
    }

    public static void AssertIntervalsEqual(IEnumerable<(int Employee, int Day, int From, int To)> expected, IEnumerable<AssignmentInterval> actual)
        => Assert.Equal(expected.OrderBy(x => x.Employee).ThenBy(x => x.Day).ThenBy(x => x.From),
            actual.Select(x => (Employee: x.EmployeeId, x.Day, From: x.FromMinute, To: x.ToMinute)).OrderBy(x => x.Employee).ThenBy(x => x.Day).ThenBy(x => x.From));

    public static async Task AssertNoOverlaps(AppDbContext db)
    {
        var schedules = await db.Schedules.AsNoTracking().Where(s => s.PublicationStatus == SchedulePublicationStatus.Public).ToDictionaryAsync(s => s.Id);
        var intervals = await ReadAssignmentIntervalsAsync(db);
        foreach (var group in intervals.Where(s => schedules.ContainsKey(s.ScheduleId))
            .GroupBy(s => (schedules[s.ScheduleId].ContainerId, s.EmployeeId, s.Year, s.Month, s.Day)))
        {
            var sorted = group.OrderBy(s => s.FromMinute).ToArray();
            for (var i = 1; i < sorted.Length; i++) Assert.True(sorted[i - 1].ToMinute <= sorted[i].FromMinute, $"Overlap for {group.Key}: {sorted[i - 1]} / {sorted[i]}");
        }
    }

    public static void AssertSourceReturnMinuteConservation(List<AssignmentInterval> before, List<AssignmentInterval> after,
        int owner, int target, int sourceMinutes, int returnMinutes = 0)
    {
        Assert.Equal(before.Sum(s => s.Minutes), after.Sum(s => s.Minutes));
        foreach (var (employee, delta) in new[] { (owner, returnMinutes - sourceMinutes), (target, sourceMinutes - returnMinutes) })
            Assert.Equal(before.Where(s => s.EmployeeId == employee).Sum(s => s.Minutes) + delta, after.Where(s => s.EmployeeId == employee).Sum(s => s.Minutes));
    }

    // Raw rows include styles, version states, requests and history, avoiding EF tracking and DTO omissions.
    public static async Task<string> SnapshotAsync(AppDbContext db)
    {
        await db.Database.OpenConnectionAsync();
        var connection = db.Database.GetDbConnection();
        var tables = db.Model.GetEntityTypes().Select(e => e.GetTableName()!).Distinct().Order().ToArray();
        var result = new Dictionary<string, List<string>>();
        foreach (var table in tables)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT * FROM \"{table.Replace("\"", "\"\"")}\"";
            await using var reader = await command.ExecuteReaderAsync();
            var rows = new List<string>();
            while (await reader.ReadAsync()) rows.Add(JsonSerializer.Serialize(Enumerable.Range(0, reader.FieldCount).Select(i => reader.IsDBNull(i) ? null : reader.GetValue(i)).ToArray()));
            rows.Sort(StringComparer.Ordinal);
            result[table] = rows;
        }
        return JsonSerializer.Serialize(result);
    }

    public static async Task AssertUnchangedAfterFailure(SqliteTestDatabase database, string before)
    {
        await using var fresh = database.CreateContext();
        Assert.Equal(before, await SnapshotAsync(fresh));
    }

    public static async Task AssertVersionAndHistoryConsistency(AppDbContext db, int requestId, params int[] schedules)
    {
        var history = await db.ShiftSwapHistories.SingleAsync(h => h.SourceShiftSwapRequestId == requestId);
        Assert.NotEqual(history.BeforeSnapshotJson, history.AfterSnapshotJson);
        foreach (var schedule in schedules.Distinct())
        {
            var versions = await db.ScheduleVersions.Where(v => v.ScheduleId == schedule).OrderBy(v => v.VersionNumber).ToListAsync();
            Assert.Equal(2, versions.Count);
            Assert.Equal(versions[0].Id, versions[1].ParentVersionId);
            Assert.NotEqual(versions[0].SnapshotJson, versions[1].SnapshotJson);
        }
    }

    public static async Task AssertInvariants(AppDbContext db)
    {
        var slots = await db.ScheduleSlots.AsNoTracking().ToListAsync();
        foreach (var s in slots)
        {
            Assert.Equal(s.EmployeeId.HasValue, s.Status == SlotStatus.ASSIGNED);
            Assert.True(Minute(s.FromTime) < Minute(s.ToTime), $"Invalid interval on slot {s.Id}");
        }
        Assert.All(slots.GroupBy(s => (s.ScheduleId, s.DayOfMonth, s.FromTime, s.ToTime, s.SlotNo)), g => Assert.Single(g));
        Assert.All((await db.ShiftSwapHistories.AsNoTracking().ToListAsync()).GroupBy(h => h.SourceShiftSwapRequestId), g => Assert.Single(g));
        Assert.All((await db.ScheduleVersions.AsNoTracking().ToListAsync()).GroupBy(v => (v.ScheduleId, v.VersionNumber)), g => Assert.Single(g));
        await AssertNoOverlaps(db);
    }
}
