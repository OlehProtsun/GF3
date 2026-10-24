using System.Security.Claims;
using System.Text.Json;
using BusinessLogicLayer.Common;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WebApi.Auth;
using WebApi.Contracts.ShiftSwaps;
using WebApi.Controllers;
using WebApi.Realtime;
using WebApi.Services;
using WebApi.ShiftSwaps;
using Xunit;

namespace GF3.Tests;

public sealed class ShiftSwapExchangeTests
{
    [Theory]
    [InlineData(false, "day")]
    [InlineData(true, "day")]
    [InlineData(false, "from")]
    [InlineData(true, "from")]
    [InlineData(false, "to")]
    [InlineData(true, "to")]
    [InlineData(false, "month")]
    [InlineData(true, "month")]
    [InlineData(false, "year")]
    [InlineData(true, "year")]
    public async Task EditedSlot_RejectsAcceptanceEvenWhenTheSelectedPeriodStillFits(bool returnSide, string field)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        int offerId, owner, target;
        await using (var db = database.CreateContext())
        {
            var f = await Seed(db); offerId = (await Create(db, f)).Id; owner = f.Owner; target = f.Target;
            var slot = returnSide ? f.Back : f.Source;
            switch (field)
            {
                case "day": slot.DayOfMonth++; break;
                case "from": slot.FromTime = "08:00"; break;
                case "to": slot.ToTime = "17:00"; break;
                case "month": slot.Schedule.Month++; break;
                case "year": slot.Schedule.Year++; break;
            }
            await db.SaveChangesAsync();
        }
        await using var accepting = database.CreateContext();
        var visible = Assert.IsAssignableFrom<IEnumerable<ShiftSwapDto>>(Assert.IsType<OkObjectResult>((await Employee(accepting, target).GetVisible(default)).Result).Value);
        var saved = Assert.Single(visible);
        Assert.False(saved.CanAccept); Assert.Contains("changed", saved.AcceptanceUnavailableReason!);
        Assert.Equal(1, saved.DayOfMonth); Assert.Equal(3, saved.ReturnDayOfMonth);
        await Assert.ThrowsAsync<ValidationException>(() => Employee(accepting, target).Accept(offerId, default));
        Assert.Empty(await accepting.ShiftSwapHistories.ToListAsync()); Assert.Empty(await accepting.ScheduleVersions.ToListAsync());
        await Employee(accepting, owner).Cancel(offerId, default);
        var archived = await accepting.ShiftSwapRequests.SingleAsync();
        Assert.Equal(3, ShiftSwapArchive.ReadView(archived.ArchivedViewsJson!, owner).ReturnDayOfMonth);
        foreach (var actor in new[] { owner, target })
        {
            var actual = await accepting.ScheduleSlots.AsNoTracking().Include(s => s.Schedule)
                .Where(s => s.EmployeeId == actor && s.Schedule.Year == 2026 && s.Schedule.Month == 10).ToListAsync();
            var view = ShiftSwapArchive.ReadView(archived.ArchivedViewsJson!, actor);
            Assert.Equal(actual.Sum(ShiftSwapRules.GetSlotDurationHours), view.CurrentEmployeeHoursBefore, 6);
            Assert.Equal(view.CurrentEmployeeHoursBefore, view.CurrentEmployeeHoursAfter);
        }
    }

    [Fact]
    public async Task MissingReturnSlot_RemainsReadableAndCanBeCancelledWithOriginalDetails()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        int owner, target, offerId;
        await using (var db = database.CreateContext())
        {
            var f = await Seed(db); owner = f.Owner; target = f.Target; offerId = (await Create(db, f)).Id;
            await db.ScheduleSlots.Where(s => s.Id == f.Back.Id).ExecuteDeleteAsync();
        }
        await using var context = database.CreateContext();
        var visible = Assert.IsAssignableFrom<IEnumerable<ShiftSwapDto>>(Assert.IsType<OkObjectResult>((await Employee(context, target).GetVisible(default)).Result).Value);
        var pending = Assert.Single(visible);
        Assert.Equal("F35", pending.ReturnScheduleName); Assert.Equal(3, pending.ReturnDayOfMonth); Assert.False(pending.CanAccept);
        await Employee(context, owner).Cancel(offerId, default);
        var saved = ShiftSwapArchive.ReadView((await context.ShiftSwapRequests.SingleAsync()).ArchivedViewsJson!, owner);
        Assert.Equal("cancelled", saved.Status); Assert.Equal("F35", saved.ReturnScheduleName); Assert.Equal(3, saved.ReturnDayOfMonth);
    }
    [Theory]
    [InlineData(true, -1)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(false, -1)]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    public void ProjectedEligibility_UsesStrictMinuteBoundariesForBothParticipants(bool ownerSide, int delta)
    {
        var sourceGraph = new ScheduleModel { Id = 1, ContainerId = 1, Year = 2026, Month = 10, PublicationStatus = SchedulePublicationStatus.Public, AllowSwap = true };
        var returnGraph = new ScheduleModel { Id = 2, ContainerId = 1, Year = 2026, Month = 10, PublicationStatus = SchedulePublicationStatus.Public, AllowSwap = true };
        var source = TestDataFactory.CreateDalSlot(1, 1, 1, 1, "09:00", "16:00"); source.Id = 1; source.Schedule = sourceGraph;
        var back = TestDataFactory.CreateDalSlot(2, 3, 1, 2, "09:00", "16:00"); back.Id = 2; back.Schedule = returnGraph;
        var start = 16 * 60 + delta;
        var other = TestDataFactory.CreateDalSlot(ownerSide ? 1 : 2, ownerSide ? 3 : 1, 1, ownerSide ? 1 : 2, $"{start / 60:00}:{start % 60:00}", "17:00");
        other.Id = 3; other.Schedule = ownerSide ? sourceGraph : returnGraph;
        var period = new ShiftSwapRules.ShiftSwapPeriod("09:00", "16:00");
        var reason = ShiftSwapExchangeRules.GetExchangeConflictReason(new(1, 2, source, period, back, period, [other, back, source], []));
        Assert.Equal(delta < 0, reason is not null);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompletionSnapshots_SurviveRemovalOfBothSlotReferences(bool accepted)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        int owner, target;
        await using (var db = database.CreateContext())
        {
            var f = await Seed(db); var offer = await Create(db, f); owner = f.Owner; target = f.Target;
            if (accepted) await Employee(db, f.Target).Accept(offer.Id, default);
            else await Employee(db, f.Owner).Cancel(offer.Id, default);
            await db.ScheduleSlots.ExecuteDeleteAsync();
        }
        await using var persisted = database.CreateContext();
        foreach (var actor in new[] { owner, target })
        {
            var visible = Assert.IsAssignableFrom<IEnumerable<ShiftSwapDto>>(Assert.IsType<OkObjectResult>((await Employee(persisted, actor).GetVisible(default)).Result).Value);
            var saved = Assert.Single(visible);
            Assert.Equal(accepted ? "accepted" : "cancelled", saved.Status);
            Assert.Equal("F27", saved.ScheduleName); Assert.Equal("F35", saved.ReturnScheduleName);
            Assert.Equal("09:00", saved.ReturnFromTime); Assert.Equal("16:00", saved.ReturnToTime);
            Assert.False(saved.CanAccept); Assert.False(saved.CanCancel);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GraphDelete_RejectsOpenExchangeOnEitherSide(bool returnSide)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await Seed(db); await Create(db, f);
        var service = System.Reflection.DispatchProxy.Create<BusinessLogicLayer.Services.Abstractions.IContainerService, DeleteGuardServiceProxy>();
        var manager = new ContainersController(service, new NoopWorkflowLogService(), new NoopRealtimeNotifier(), db: db)
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        Assert.IsType<ConflictObjectResult>(await manager.DeleteGraph(f.Source.Schedule.ContainerId, returnSide ? f.Back.ScheduleId : f.Source.ScheduleId, default));
        Assert.Equal(2, await db.Schedules.CountAsync());
        Assert.Equal(ShiftSwapStatus.Open, (await db.ShiftSwapRequests.SingleAsync()).Status);
    }

    public class DeleteGuardServiceProxy : System.Reflection.DispatchProxy
    {
        protected override object? Invoke(System.Reflection.MethodInfo? targetMethod, object?[]? args)
            => targetMethod?.Name == "GetGraphByIdAsync"
                ? Task.FromResult<BusinessLogicLayer.Contracts.Models.ScheduleModel?>(null)
                : throw new InvalidOperationException("The deletion service must not be called for a reserved graph.");
    }
    [Fact]
    public async Task ManagerCancel_LeavesBothIntervalsUnchangedAndKeepsExchangeInLogs()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await Seed(db); var created = await Create(db, f);
        var manager = new ShiftSwapLogsController(db, new NoopWorkflowLogService(), new NoopRealtimeNotifier())
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        var result = await manager.CancelContainerSwap(f.Source.Schedule.ContainerId, created.Id, default);
        Assert.Equal("exchange", Assert.IsType<ShiftSwapDto>(Assert.IsType<OkObjectResult>(result.Result).Value).RequestKind);
        var logs = Assert.IsAssignableFrom<IEnumerable<ShiftSwapDto>>(Assert.IsType<OkObjectResult>((await manager.GetContainerSwaps(f.Source.Schedule.ContainerId, default)).Result).Value);
        Assert.Equal("F35", Assert.Single(logs).ReturnScheduleName);
        Assert.Equal(f.Owner, (await db.ScheduleSlots.SingleAsync(s => s.Id == f.Source.Id)).EmployeeId);
        Assert.Equal(f.Target, (await db.ScheduleSlots.SingleAsync(s => s.Id == f.Back.Id)).EmployeeId);
        Assert.Empty(await db.ShiftSwapHistories.ToListAsync());
    }

    [Fact]
    public async Task GivePreview_CountsUniqueDatesAndUntouchedRemaindersAcrossGraphs()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await Seed(db);
        db.Add(TestDataFactory.CreateDalSlot(f.Back.ScheduleId, 1, 1, f.Owner, "17:00", "19:00"));
        await db.SaveChangesAsync();
        var response = await Employee(db, f.Owner).Preview(new() { ScheduleId = f.Source.ScheduleId, ScheduleSlotId = f.Source.Id, FromTime = "09:00", ToTime = "12:00" }, default);
        var stats = Assert.IsType<ShiftSwapOfferPreviewDto>(Assert.IsType<OkObjectResult>(response.Result).Value).ProjectedMonths.Single();
        Assert.Equal(9, stats.HoursBefore); Assert.Equal(6, stats.HoursAfter);
        Assert.Equal(1, stats.WorkDaysBefore); Assert.Equal(1, stats.WorkDaysAfter);
    }
    [Fact]
    public async Task ReservedReturn_CannotBecomeAnotherSourceOrReturn_AndPrivateReadsStayScoped()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await Seed(db); var created = await Create(db, f);
        var second = TestDataFactory.CreateDalSlot(f.Source.ScheduleId, 5, 1, f.Owner, "09:00", "16:00");
        db.Add(second); await db.SaveChangesAsync();
        f.Request.ScheduleSlotId = second.Id;
        await Assert.ThrowsAsync<ValidationException>(() => Employee(db, f.Owner).Create(f.Request, default));
        await Assert.ThrowsAsync<ValidationException>(() => Employee(db, f.Target).Create(new()
        { ScheduleId = f.Back.ScheduleId, ScheduleSlotId = f.Back.Id, FromTime = "09:00", ToTime = "16:00" }, default));
        var duplicate = new ShiftSwapRequestModel { ScheduleId = f.Source.ScheduleId, ScheduleSlotId = second.Id,
            RequestKind = ShiftSwapRequestKind.Exchange, FromEmployeeId = f.Owner, TargetEmployeeId = f.Target,
            ReturnScheduleId = f.Back.ScheduleId, ReturnScheduleSlotId = f.Back.Id, ReturnFromTime = "09:00", ReturnToTime = "16:00",
            OfferedFromTime = "09:00", OfferedToTime = "16:00", Visibility = ShiftSwapVisibility.Private, CreatedAtUtc = DateTimeOffset.UtcNow };
        db.Add(duplicate);
        var duplicateError = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(2067, Assert.IsType<Microsoft.Data.Sqlite.SqliteException>(duplicateError.InnerException).SqliteExtendedErrorCode);
        db.Entry(duplicate).State = EntityState.Detached;
        var privateOffers = Assert.IsAssignableFrom<IEnumerable<ShiftSwapDto>>(Assert.IsType<OkObjectResult>((await Employee(db, 9999).GetVisible(default)).Result).Value);
        Assert.Empty(privateOffers);
        await Assert.ThrowsAsync<ValidationException>(() => Employee(db, 9999).Accept(created.Id, default));
        var targetOffers = Assert.IsAssignableFrom<IEnumerable<ShiftSwapDto>>(Assert.IsType<OkObjectResult>((await Employee(db, f.Target).GetVisible(default)).Result).Value);
        Assert.True(Assert.Single(targetOffers).CanAccept);
        await Employee(db, f.Owner).Cancel(created.Id, default);
        targetOffers = Assert.IsAssignableFrom<IEnumerable<ShiftSwapDto>>(Assert.IsType<OkObjectResult>((await Employee(db, f.Target).GetVisible(default)).Result).Value);
        Assert.Equal("F35", Assert.Single(targetOffers).ReturnScheduleName);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Notifications_AfterCommitAndNeverRepeated(bool fail)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await Seed(db); var offer = await Create(db, f);
        var notifier = new CountingNotifier { Fail = fail };
        await Employee(db, f.Target, notifier: notifier).Accept(offer.Id, default);
        Assert.Equal(4, notifier.AcceptedEvents);
        await Assert.ThrowsAsync<ValidationException>(() => Employee(db, f.Target, notifier: notifier).Accept(offer.Id, default));
        Assert.Equal(4, notifier.AcceptedEvents);
        await using var persisted = database.CreateContext();
        Assert.Equal(ShiftSwapStatus.Accepted, (await persisted.ShiftSwapRequests.SingleAsync()).Status);
        Assert.Single(await persisted.ShiftSwapHistories.ToListAsync());
    }
    [Theory]
    [InlineData(2026, 11, 1)]
    [InlineData(2027, 1, 15)]
    [InlineData(2026, 10, 31)]
    public async Task CrossMonthAndMinutePrecision_PreviewMatchesAcceptance(int year, int month, int minutes)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await Seed(db);
        f.Back.Schedule.Year = year; f.Back.Schedule.Month = month;
        f.Request.ToTime = $"09:{minutes:00}"; f.Request.ReturnToTime = $"09:{minutes:00}";
        await db.SaveChangesAsync();
        var preview = Assert.IsType<ShiftSwapOfferPreviewDto>(Assert.IsType<OkObjectResult>((await Employee(db, f.Owner).Preview(f.Request, default)).Result).Value);
        Assert.Equal(month == 10 && year == 2026 ? 1 : 2, preview.ProjectedMonths.Count);
        Assert.Equal(7, preview.ProjectedMonths.Sum(s => s.HoursAfter), 6);
        var offer = await Create(db, f);
        await Employee(db, f.Target).Accept(offer.Id, default);
        db.ChangeTracker.Clear();
        Assert.Equal(f.Owner, (await db.ScheduleSlots.SingleAsync(s => s.Id == f.Back.Id)).EmployeeId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SameDay_OutgoingIntervalsAreSubtractedBeforeConflictCheck(bool sameGraph)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await Seed(db, sameGraph);
        f.Back.DayOfMonth = 1; f.Back.FromTime = "17:00"; f.Back.ToTime = "23:59";
        f.Request.ToTime = "15:59"; f.Request.ReturnFromTime = "17:00"; f.Request.ReturnToTime = "23:59";
        await db.SaveChangesAsync();
        var offer = await Create(db, f);
        await Employee(db, f.Target).Accept(offer.Id, default);
        db.ChangeTracker.Clear();
        Assert.Equal(f.Target, (await db.ScheduleSlots.SingleAsync(s => s.Id == f.Source.Id)).EmployeeId);
        Assert.Equal(f.Owner, (await db.ScheduleSlots.SingleAsync(s => s.Id == f.Back.Id)).EmployeeId);
    }

    [Fact]
    public async Task ReturnGraph_CheckoutBlockedWhileOpenAndArchivesBothSidesAfterAcceptance()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await Seed(db);
        var writer = new ScheduleVersionCommitService(db);
        var baseline = (await writer.CommitAsync(f.Back.ScheduleId, null, "baseline", default))!;
        var created = await Create(db, f);
        var manager = new GraphVersionsController(db, new NoopWorkflowLogService(), new NoopRealtimeNotifier(), writer)
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        Assert.IsType<ConflictObjectResult>((await manager.Checkout(f.Source.Schedule.ContainerId, f.Back.ScheduleId, baseline.Id, default)).Result);
        await Employee(db, f.Target).Accept(created.Id, default);
        Assert.IsType<OkObjectResult>((await manager.Checkout(f.Source.Schedule.ContainerId, f.Back.ScheduleId, baseline.Id, default)).Result);
        db.ChangeTracker.Clear();
        var offer = await db.ShiftSwapRequests.SingleAsync();
        Assert.NotNull(offer.ArchivedViewsJson);
        var archived = ShiftSwapArchive.ReadView(offer.ArchivedViewsJson!, f.Owner);
        Assert.Equal("F35", archived.ReturnScheduleName);
        Assert.Equal("09:00", archived.ReturnFromTime);
        Assert.NotNull(archived.ReturnBeforeSnapshot); Assert.NotNull(archived.ReturnAfterSnapshot);
        Assert.Single(await db.ShiftSwapHistories.ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AcceptanceRaces_CommitAtMostOnce(bool cancel)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        Fixture f; int id;
        await using (var db = database.CreateContext()) { f = await Seed(db); id = (await Create(db, f)).Id; }
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = 0;
        async Task<Exception?> Race(bool second)
        {
            await using var context = database.CreateContext();
            if (Interlocked.Increment(ref started) == 2) ready.SetResult();
            await ready.Task;
            return await Record.ExceptionAsync(async () =>
            {
                if (second && cancel) await Employee(context, f.Owner).Cancel(id, default);
                else await Employee(context, f.Target).Accept(id, default);
            });
        }
        var results = await Task.WhenAll(Task.Run(() => Race(false)), Task.Run(() => Race(true)));
        Assert.Single(results, error => error is null);
        Assert.Single(results, error => error is ValidationException);
        await using var persisted = database.CreateContext();
        var offer = await persisted.ShiftSwapRequests.SingleAsync();
        var accepted = offer.Status == ShiftSwapStatus.Accepted;
        Assert.Equal(accepted ? 1 : 0, await persisted.ShiftSwapHistories.CountAsync());
        Assert.Equal(accepted ? 4 : 0, await persisted.ScheduleVersions.CountAsync());
        Assert.Equal(accepted ? f.Target : f.Owner, (await persisted.ScheduleSlots.SingleAsync(s => s.Id == f.Source.Id)).EmployeeId);
        Assert.Equal(accepted ? f.Owner : f.Target, (await persisted.ScheduleSlots.SingleAsync(s => s.Id == f.Back.Id)).EmployeeId);
    }
    [Fact]
    public async Task Migration_UpgradeAndRollbackPreserveLegacyGiveHistoryAndIndexes()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using (var db = database.CreateContext())
        {
            var f = await Seed(db);
            f.Request.RequestKind = null; f.Request.ReturnScheduleId = null; f.Request.ReturnScheduleSlotId = null;
            f.Request.ReturnFromTime = null; f.Request.ReturnToTime = null;
            var offer = await Create(db, f);
            await Employee(db, f.Target).Accept(offer.Id, default);
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync("20261015133115_AddEmployeeSoftDelete");
            await using var connection = db.Database.GetDbConnection();
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('shift_swap_request') WHERE name = 'request_kind'";
            Assert.Equal(0L, await command.ExecuteScalarAsync());
            command.CommandText = "SELECT COUNT(*) FROM shift_swap_history";
            Assert.Equal(1L, await command.ExecuteScalarAsync());
            await connection.CloseAsync();
            await migrator.MigrateAsync();
        }
        await using var upgraded = database.CreateContext();
        Assert.Equal(ShiftSwapRequestKind.Give, (await upgraded.ShiftSwapRequests.SingleAsync()).RequestKind);
        Assert.Equal(ShiftSwapRequestKind.Give, (await upgraded.ShiftSwapHistories.SingleAsync()).RequestKind);
        Assert.Equal(2, await upgraded.ScheduleVersions.CountAsync());
        await upgraded.Database.OpenConnectionAsync();
        await using var indexes = upgraded.Database.GetDbConnection().CreateCommand();
        indexes.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name IN ('ux_shift_swap_open_slot', 'ux_shift_swap_open_return_slot', 'ix_shift_swap_return_schedule_status')";
        Assert.Equal(3L, await indexes.ExecuteScalarAsync());
    }

    [Fact]
    public void ProjectedConflicts_MatchIndependentMinuteOccupancyReference()
    {
        var random = new Random(7913);
        for (var iteration = 0; iteration < 1000; iteration++)
        {
            var duration = random.Next(1, 240);
            var returnDuration = random.Next(1, 240);
            var sourceFrom = random.Next(60, 800); var returnFrom = random.Next(60, 800);
            var sourceSchedule = new ScheduleModel { Id = 1, ContainerId = 1, Year = 2026, Month = 10, PublicationStatus = SchedulePublicationStatus.Public, AllowSwap = true };
            var returnSchedule = new ScheduleModel { Id = 2, ContainerId = 1, Year = 2026, Month = 10, PublicationStatus = SchedulePublicationStatus.Public, AllowSwap = true };
            var source = new ScheduleSlotModel { Id = 1, ScheduleId = 1, Schedule = sourceSchedule, DayOfMonth = 1, SlotNo = 1, EmployeeId = 1, Status = SlotStatus.ASSIGNED, FromTime = Time(sourceFrom - 10), ToTime = Time(sourceFrom + duration + 10) };
            var back = new ScheduleSlotModel { Id = 2, ScheduleId = 2, Schedule = returnSchedule, DayOfMonth = random.Next(1, 3), SlotNo = 1, EmployeeId = 2, Status = SlotStatus.ASSIGNED, FromTime = Time(returnFrom - 10), ToTime = Time(returnFrom + returnDuration + 10) };
            var assignments = new List<ScheduleSlotModel> { source, back };
            for (var other = 0; other < 5; other++)
            {
                var from = random.Next(60, 1000);
                assignments.Add(new() { Id = other + 3, ScheduleId = 2, Schedule = returnSchedule, DayOfMonth = random.Next(1, 3), EmployeeId = random.Next(1, 3), Status = SlotStatus.ASSIGNED, FromTime = Time(from), ToTime = Time(from + 30) });
            }
            var give = new ShiftSwapRules.ShiftSwapPeriod(Time(sourceFrom), Time(sourceFrom + duration));
            var receive = new ShiftSwapRules.ShiftSwapPeriod(Time(returnFrom), Time(returnFrom + returnDuration));
            bool ReferenceCollision(int employee, ScheduleSlotModel outgoing, int from, int outgoingDuration, ScheduleSlotModel incoming, int incomingFrom, int incomingDuration)
            {
                var occupied = new HashSet<(int Day, int Minute)>();
                foreach (var slot in assignments.Where(s => s.EmployeeId == employee))
                    for (var minute = ShiftSwapRules.ParseTimeMinutes(slot.FromTime)!.Value; minute < ShiftSwapRules.ParseTimeMinutes(slot.ToTime); minute++)
                        if (slot.Id != outgoing.Id || minute < from || minute >= from + outgoingDuration) occupied.Add((slot.DayOfMonth, minute));
                return Enumerable.Range(incomingFrom, incomingDuration).Any(minute => occupied.Contains((incoming.DayOfMonth, minute)));
            }
            var expected = ReferenceCollision(1, source, sourceFrom, duration, back, returnFrom, returnDuration) || ReferenceCollision(2, back, returnFrom, returnDuration, source, sourceFrom, duration);
            Assert.Equal(expected, ShiftSwapExchangeRules.GetExchangeConflictReason(new(1, 2, source, give, back, receive, assignments.OrderBy(_ => random.Next()).ToList(), [])) is not null);
            var fragments = ShiftSwapExchangeRules.ApplyExchangeTransferToSlot(source, 1, 2, give, [source]);
            Assert.Equal(2, fragments.Count);
            Assert.Equal(duration + 20, fragments.Append(source).Sum(s => ShiftSwapRules.ParseTimeMinutes(s.ToTime) - ShiftSwapRules.ParseTimeMinutes(s.FromTime)));
            Assert.All(fragments, s => Assert.Equal(1, s.EmployeeId));
        }
        static string Time(int value) => $"{value / 60:00}:{value % 60:00}";
    }
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(true, true, true)]
    public async Task Accept_TransfersBothPeriodsAndVersions(bool partialSource, bool partialReturn, bool sameGraph)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await Seed(db, sameGraph, partialSource, partialReturn);
        var preview = Assert.IsType<ShiftSwapOfferPreviewDto>(Assert.IsType<OkObjectResult>((await Employee(db, f.Owner).Preview(f.Request, default)).Result).Value);
        Assert.True(preview.IsValid);
        Assert.Equal(partialSource ? 2 : 1, preview.ProjectedMonths.Single().WorkDaysAfter);
        var created = await Create(db, f);
        Assert.Equal("exchange", created.RequestKind);
        Assert.Equal(f.Target, created.TargetEmployeeId);
        Assert.Equal(f.Back.ScheduleId, created.ReturnScheduleId);
        Assert.Equal(f.Owner, (await db.ScheduleSlots.SingleAsync(s => s.Id == f.Source.Id)).EmployeeId);
        await Employee(db, f.Target).Accept(created.Id, default);
        await using var persisted = database.CreateContext();
        Assert.Equal(f.Target, (await persisted.ScheduleSlots.SingleAsync(s => s.Id == f.Source.Id)).EmployeeId);
        Assert.Equal(f.Owner, (await persisted.ScheduleSlots.SingleAsync(s => s.Id == f.Back.Id)).EmployeeId);
        Assert.Equal((partialSource ? 2 : 0) + (partialReturn ? 2 : 0) + 2, await persisted.ScheduleSlots.CountAsync());
        Assert.Equal(sameGraph ? 2 : 4, await persisted.ScheduleVersions.CountAsync());
        var history = await persisted.ShiftSwapHistories.SingleAsync();
        Assert.Equal(ShiftSwapRequestKind.Exchange, history.RequestKind);
        Assert.NotNull(history.ReturnBeforeSnapshotJson);
        Assert.NotNull(history.ReturnAfterSnapshotJson);
        Assert.Equal(ShiftSwapStatus.Accepted, (await persisted.ShiftSwapRequests.SingleAsync()).Status);
        var slots = await persisted.ScheduleSlots.Include(s => s.Schedule).ToListAsync();
        var actualHours = slots.Where(s => s.EmployeeId == f.Owner).Sum(s => ShiftSwapRules.GetSlotDurationHours(s));
        Assert.Equal(preview.ProjectedMonths.Sum(s => s.HoursAfter), actualHours, 6);
        Assert.Equal(4, await persisted.ScheduleCellStyles.CountAsync());
        await Assert.ThrowsAsync<ValidationException>(() => Employee(persisted, f.Target).Accept(created.Id, default));
        Assert.Single(await persisted.ShiftSwapHistories.ToListAsync());
    }

    [Theory]
    [InlineData("09:00", "17:00", "15:00", "21:00", false)]
    [InlineData("09:00", "14:00", "10:00", "18:00", false)]
    [InlineData("09:15", "14:00", "10:00", "17:45", true)]
    public async Task UnequalPeriods_PreviewCreateAndAcceptUseActualHours(string from, string to, string backFrom, string backTo, bool partial)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await Seed(db);
        f.Source.FromTime = partial ? "09:00" : from; f.Source.ToTime = partial ? "17:00" : to;
        f.Back.FromTime = partial ? "09:00" : backFrom; f.Back.ToTime = partial ? "18:00" : backTo;
        f.Request.FromTime = from; f.Request.ToTime = to;
        f.Request.ReturnFromTime = backFrom; f.Request.ReturnToTime = backTo;
        await db.SaveChangesAsync();
        var candidates = Assert.IsType<ShiftSwapReturnCandidatePageDto>(Assert.IsType<OkObjectResult>((await Employee(db, f.Owner)
            .GetReturnCandidates(f.Source.ScheduleId, f.Source.Id, from, to, f.Target, default)).Result).Value);
        var candidate = Assert.Single(candidates.Items);
        Assert.True(candidate.CanSelect);
        Assert.Equal(f.Back.FromTime, candidate.SuggestedFromTime); Assert.Equal(f.Back.ToTime, candidate.SuggestedToTime);
        var before = ShiftSwapRules.GetSlotDurationHours(f.Source);
        var giveHours = ShiftSwapExchangeRules.DurationMinutes(new(from, to)) / 60d;
        var receiveHours = ShiftSwapExchangeRules.DurationMinutes(new(backFrom, backTo)) / 60d;
        var preview = Assert.IsType<ShiftSwapOfferPreviewDto>(Assert.IsType<OkObjectResult>((await Employee(db, f.Owner).Preview(f.Request, default)).Result).Value);
        Assert.Equal(before - giveHours + receiveHours, preview.ProjectedMonths.Single().HoursAfter, 6);
        var created = await Create(db, f);
        await Employee(db, f.Target).Accept(created.Id, default);
        await using var persisted = database.CreateContext();
        var slots = await persisted.ScheduleSlots.Include(s => s.Schedule).ToListAsync();
        Assert.Contains(slots, s => s.EmployeeId == f.Target && s.ScheduleId == f.Source.ScheduleId && s.FromTime == from && s.ToTime == to);
        Assert.Contains(slots, s => s.EmployeeId == f.Owner && s.ScheduleId == f.Back.ScheduleId && s.FromTime == backFrom && s.ToTime == backTo);
        Assert.Equal(preview.ProjectedMonths.Single().HoursAfter, slots.Where(s => s.EmployeeId == f.Owner).Sum(ShiftSwapRules.GetSlotDurationHours), 6);
        Assert.Equal(partial ? 6 : 2, slots.Count);
        Assert.Single(await persisted.ShiftSwapHistories.ToListAsync());
        Assert.Equal(4, await persisted.ScheduleVersions.CountAsync());
    }

    [Theory]
    [InlineData("kind")]
    [InlineData("public")]
    [InlineData("self")]
    [InlineData("unknown")]
    [InlineData("deleted")]
    [InlineData("missing")]
    [InlineData("malformed")]
    [InlineData("outside")]
    [InlineData("container")]
    [InlineData("private")]
    [InlineData("disabled")]
    [InlineData("owner")]
    [InlineData("source-owner")]
    [InlineData("give-return")]
    [InlineData("collision-owner")]
    [InlineData("collision-target")]
    [InlineData("reserved")]
    [InlineData("date")]
    public async Task Create_InvalidExchangeLeavesSchedulesUntouched(string invalid)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await Seed(db);
        switch (invalid)
        {
            case "kind": f.Request.RequestKind = "other"; break;
            case "public": f.Request.TargetEmployeeId = null; break;
            case "self": f.Request.TargetEmployeeId = f.Owner; break;
            case "unknown": f.Request.TargetEmployeeId = 99999; break;
            case "deleted": (await db.Employees.SingleAsync(e => e.Id == f.Target)).IsDeleted = true; break;
            case "missing": f.Request.ReturnToTime = null; break;
            case "malformed": f.Request.ReturnFromTime = "9:00"; break;
            case "outside": f.Request.ReturnFromTime = "08:00"; f.Request.ReturnToTime = "15:00"; break;
            case "container": var other = TestDataFactory.CreateDalContainer("Other"); db.Add(other); await db.SaveChangesAsync(); f.Back.Schedule.ContainerId = other.Id; break;
            case "private": f.Back.Schedule.PublicationStatus = SchedulePublicationStatus.Private; break;
            case "disabled": f.Back.Schedule.AllowSwap = false; break;
            case "owner": f.Back.EmployeeId = f.Owner; break;
            case "source-owner": f.Source.EmployeeId = f.Target; break;
            case "give-return": f.Request.RequestKind = "give"; break;
            case "collision-owner": db.Add(TestDataFactory.CreateDalSlot(f.Source.ScheduleId, f.Back.DayOfMonth, 2, f.Owner, "10:00", "12:00")); break;
            case "collision-target": db.Add(TestDataFactory.CreateDalSlot(f.Back.ScheduleId, f.Source.DayOfMonth, 2, f.Target, "10:00", "12:00")); break;
            case "reserved": db.Add(new ShiftSwapRequestModel { ScheduleId = f.Back.ScheduleId, ScheduleSlotId = f.Back.Id,
                FromEmployeeId = f.Target, OfferedFromTime = "09:00", OfferedToTime = "16:00", CreatedAtUtc = DateTimeOffset.UtcNow }); break;
            case "date": f.Source.Schedule.Month = 2; f.Source.DayOfMonth = 31; break;
        }
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ValidationException>(() => Employee(db, f.Owner).Create(f.Request, default));
        Assert.Empty(await db.ShiftSwapHistories.ToListAsync());
        Assert.Empty(await db.ScheduleVersions.ToListAsync());
    }

    [Theory]
    [InlineData("second-version")]
    [InlineData("history")]
    [InlineData("stale")]
    [InlineData("cancelled")]
    [InlineData("locked")]
    public async Task Accept_FailureRollsBackBothSchedules(string failure)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await Seed(db);
        var created = await Create(db, f);
        if (failure == "second-version")
        {
            var trigger = "CREATE TRIGGER fail_exchange BEFORE INSERT ON schedule_version WHEN NEW.schedule_id = " + f.Back.ScheduleId.ToString(System.Globalization.CultureInfo.InvariantCulture) + " AND NEW.version_number = 2 BEGIN SELECT RAISE(ABORT, 'injected failure'); END;";
            await db.Database.ExecuteSqlRawAsync(trigger);
        }
        if (failure == "history") await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_exchange BEFORE INSERT ON shift_swap_history BEGIN SELECT RAISE(ABORT, 'injected failure'); END;");
        if (failure == "stale") { f.Back.EmployeeId = f.Owner; await db.SaveChangesAsync(); }
        if (failure == "cancelled") await Employee(db, f.Owner).Cancel(created.Id, default);
        var locks = new ManagerEditLockService();
        if (failure == "locked") ((IScheduleEditLockService)locks).SetLocks("manager", "Manager", [new ScheduleEditLockTarget(f.Back.Schedule.ContainerId, f.Back.ScheduleId)]);
        var notifier = new CountingNotifier();
        Assert.NotNull(await Record.ExceptionAsync(() => Employee(db, f.Target, locks, notifier).Accept(created.Id, default)));
        Assert.Equal(0, notifier.AcceptedEvents);
        await using var persisted = database.CreateContext();
        Assert.Equal(f.Owner, (await persisted.ScheduleSlots.SingleAsync(s => s.Id == f.Source.Id)).EmployeeId);
        Assert.Equal(failure == "stale" ? f.Owner : f.Target, (await persisted.ScheduleSlots.SingleAsync(s => s.Id == f.Back.Id)).EmployeeId);
        Assert.Empty(await persisted.ShiftSwapHistories.ToListAsync());
        Assert.Empty(await persisted.ScheduleVersions.ToListAsync());
        Assert.Equal(failure == "cancelled" ? ShiftSwapStatus.Cancelled : ShiftSwapStatus.Open, (await persisted.ShiftSwapRequests.SingleAsync()).Status);
    }

    [Fact]
    public async Task Candidates_AuthorizedPaginatedAndPrivate()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await Seed(db);
        for (var day = 5; day < 31; day++) db.Add(TestDataFactory.CreateDalSlot(f.Back.ScheduleId, day, 1, f.Target, "09:00", "16:00"));
        await db.SaveChangesAsync();
        var page = Assert.IsType<ShiftSwapReturnCandidatePageDto>(Assert.IsType<OkObjectResult>((await Employee(db, f.Owner).GetReturnCandidates(f.Source.ScheduleId, f.Source.Id, "09:00", "16:00", f.Target, default, 1, 10)).Result).Value);
        Assert.Equal(10, page.Items.Count); Assert.True(page.HasMore);
        var json = JsonSerializer.Serialize(page);
        Assert.DoesNotContain("email", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("phone", json, StringComparison.OrdinalIgnoreCase);
        await Assert.ThrowsAsync<ValidationException>(() => Employee(db, f.Target).GetReturnCandidates(f.Source.ScheduleId, f.Source.Id, "09:00", "16:00", f.Owner, default));
    }

    [Fact]
    public async Task Candidates_ExposeShortSafeIntervalsWithoutIgnoringConflicts()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await Seed(db);
        db.Add(TestDataFactory.CreateDalSlot(f.Source.ScheduleId, f.Back.DayOfMonth, 2, f.Owner, "10:00", "15:00"));
        await db.SaveChangesAsync();
        var page = Assert.IsType<ShiftSwapReturnCandidatePageDto>(Assert.IsType<OkObjectResult>((await Employee(db, f.Owner)
            .GetReturnCandidates(f.Source.ScheduleId, f.Source.Id, "09:00", "16:00", f.Target, default)).Result).Value);
        var candidate = Assert.Single(page.Items);
        Assert.True(candidate.CanSelect);
        Assert.Equal(new[] { new ShiftSwapEligibleWindowDto("09:00", "10:00"), new ShiftSwapEligibleWindowDto("15:00", "16:00") }, candidate.EligibleWindows);
        f.Request.ReturnToTime = "10:00";
        await Employee(db, f.Owner).Preview(f.Request, default);
        f.Request.ReturnToTime = "10:01";
        await Assert.ThrowsAsync<ValidationException>(() => Employee(db, f.Owner).Preview(f.Request, default));
    }

    private static async Task<ShiftSwapDto> Create(AppDbContext db, Fixture f)
        => Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>((await Employee(db, f.Owner).Create(f.Request, default)).Result).Value);

    private static EmployeeShiftSwapsController Employee(AppDbContext db, int employee, IScheduleEditLockService? locks = null, IRealtimeNotifier? notifier = null)
    {
        var controller = new EmployeeShiftSwapsController(db, locks ?? new ManagerEditLockService(), new NoopWorkflowLogService(), notifier ?? new NoopRealtimeNotifier(), new ScheduleVersionCommitService(db));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.Name, "test"), new Claim(ClaimTypes.Role, AuthRoles.Employee), new Claim("employee_id", employee.ToString())], JwtAuthenticationDefaults.SchemeName)) } };
        return controller;
    }

    private static async Task<Fixture> Seed(AppDbContext db, bool sameGraph = false, bool partialSource = false, bool partialReturn = false)
    {
        var container = TestDataFactory.CreateDalContainer(); var shop = TestDataFactory.CreateDalShop();
        var owner = TestDataFactory.CreateDalEmployee("Owner", "Worker", "owner@example.invalid");
        var target = TestDataFactory.CreateDalEmployee("Target", "Worker", "target@example.invalid");
        db.AddRange(container, shop, owner, target); await db.SaveChangesAsync();
        var source = TestDataFactory.CreateDalSchedule(container.Id, shop.Id, "F27", 2026, 10);
        source.PublicationStatus = SchedulePublicationStatus.Public; source.AllowSwap = true;
        var back = sameGraph ? source : TestDataFactory.CreateDalSchedule(container.Id, shop.Id, "F35", 2026, 10);
        back.PublicationStatus = SchedulePublicationStatus.Public; back.AllowSwap = true;
        db.Add(source); if (!sameGraph) db.Add(back); await db.SaveChangesAsync();
        db.AddRange(new ScheduleEmployeeModel { ScheduleId = source.Id, EmployeeId = owner.Id }, new ScheduleEmployeeModel { ScheduleId = back.Id, EmployeeId = target.Id });
        var offered = TestDataFactory.CreateDalSlot(source.Id, 1, 1, owner.Id, partialSource ? "08:00" : "09:00", partialSource ? "18:00" : "16:00");
        var returned = TestDataFactory.CreateDalSlot(back.Id, 3, 1, target.Id, partialReturn ? "08:00" : "09:00", partialReturn ? "18:00" : "16:00");
        db.AddRange(offered, returned); await db.SaveChangesAsync();
        return new(owner.Id, target.Id, offered, returned, new() { RequestKind = "exchange", ScheduleId = source.Id, ScheduleSlotId = offered.Id,
            FromTime = "09:00", ToTime = "16:00", TargetEmployeeId = target.Id, ReturnScheduleId = back.Id, ReturnScheduleSlotId = returned.Id,
            ReturnFromTime = "09:00", ReturnToTime = "16:00" });
    }
    private sealed record Fixture(int Owner, int Target, ScheduleSlotModel Source, ScheduleSlotModel Back, CreateEmployeeShiftSwapRequest Request);

    private sealed class CountingNotifier : IRealtimeNotifier
    {
        public int AcceptedEvents;
        public bool Fail;
        private Task Record(string reason)
        {
            if (reason.Contains("accepted", StringComparison.Ordinal)) Interlocked.Increment(ref AcceptedEvents);
            return Fail ? Task.FromException(new InvalidOperationException("notification failure")) : Task.CompletedTask;
        }
        public Task NotifyScheduleChangedAsync(int containerId, int graphId, string reason) => Record(reason);
        public Task NotifyShiftSwapsChangedAsync(int? containerId, int? graphId, int? scheduleId, string reason, int? shiftSwapId = null) => Record(reason);
        public Task NotifyManagerDataChangedAsync(string resourceType, string? resourceId, string reason, int? containerId = null, int? graphId = null) => Task.CompletedTask;
        public Task NotifyWorkflowLogCreatedAsync(WorkflowLogEntryModel entry) => Task.CompletedTask;
        public Task NotifyScheduleEditLockChangedAsync(ScheduleEditLockState state) => Task.CompletedTask;
        public Task NotifyManagerEditLockChangedAsync(ManagerEditLockState state) => Task.CompletedTask;
    }
}
