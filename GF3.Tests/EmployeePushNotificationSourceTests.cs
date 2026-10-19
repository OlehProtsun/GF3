using System.Text.Json;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using GF3.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebApi.Services;

namespace GF3.Tests;

public sealed class EmployeePushNotificationSourceTests
{
    [Fact]
    public async Task PublicRequiresSharedMembership_PrivateRequiresTarget_AndArchivedAcceptanceStaysOwned()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var (owner, recipient, schedule, slot) = await Seed(db);
        var outsider = TestDataFactory.CreateDalEmployee("Outside", "Container"); db.Add(outsider); await db.SaveChangesAsync();
        var offer = Offer(owner, schedule, slot); db.Add(offer); await db.SaveChangesAsync();
        Assert.DoesNotContain(await Source(db).GetCurrentAsync(outsider.Id, Now, default), x => x.Tone == "swap");
        offer.Visibility = ShiftSwapVisibility.Private; offer.TargetEmployeeId = outsider.Id; await db.SaveChangesAsync();
        Assert.Contains(await Source(db).GetCurrentAsync(outsider.Id, Now, default), x => x.NotificationId == $"swap-public:{offer.Id}");
        Assert.DoesNotContain(await Source(db).GetCurrentAsync(recipient, Now, default), x => x.Tone == "swap");
        offer.ScheduleSlotId = null; await db.SaveChangesAsync();
        Assert.DoesNotContain(await Source(db).GetCurrentAsync(outsider.Id, Now, default), x => x.Tone == "swap");
        offer.Status = ShiftSwapStatus.Accepted; offer.AcceptedAtUtc = Now; offer.ArchivedViewsJson = "{}";
        schedule.PublicationStatus = SchedulePublicationStatus.Private; await db.SaveChangesAsync();
        Assert.Contains(await Source(db).GetCurrentAsync(owner, Now, default), x => x.NotificationId == $"swap-accepted:{offer.Id}");
        Assert.DoesNotContain(await Source(db).GetCurrentAsync(outsider.Id, Now, default), x => x.Tone == "swap");
    }

    internal static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-19T12:00:00+00:00");
    internal static EmployeePushNotificationSource Source(AppDbContext db) => new(db, Options.Create(new Microsoft.AspNetCore.Mvc.JsonOptions()));

    internal static async Task<(int Owner, int Recipient, ScheduleModel Schedule, ScheduleSlotModel Slot)> Seed(AppDbContext db)
    {
        var owner = TestDataFactory.CreateDalEmployee("Owner", "Push");
        var recipient = TestDataFactory.CreateDalEmployee("Recipient", "Push");
        var container = TestDataFactory.CreateDalContainer();
        var shop = TestDataFactory.CreateDalShop();
        db.AddRange(owner, recipient, container, shop);
        await db.SaveChangesAsync();
        var schedule = TestDataFactory.CreateDalSchedule(container.Id, shop.Id);
        schedule.PublicationStatus = SchedulePublicationStatus.Public;
        schedule.PublishedAtUtc = Now.AddHours(-1);
        schedule.Employees.Add(new() { EmployeeId = owner.Id });
        schedule.Employees.Add(new() { EmployeeId = recipient.Id });
        db.Add(schedule);
        await db.SaveChangesAsync();
        await db.Schedules.Where(x => x.Id == schedule.Id).ExecuteUpdateAsync(x => x.SetProperty(s => s.PublishedAtUtc, Now.AddHours(-1)));
        await db.Entry(schedule).ReloadAsync();
        var slot = TestDataFactory.CreateDalSlot(schedule.Id, 1, 1, owner.Id, "08:00", "16:00");
        db.Add(slot);
        await db.SaveChangesAsync();
        return (owner.Id, recipient.Id, schedule, slot);
    }

    internal static ShiftSwapRequestModel Offer(int owner, ScheduleModel schedule, ScheduleSlotModel slot) => new()
    { ScheduleId = schedule.Id, ScheduleSlotId = slot.Id, FromEmployeeId = owner, CreatedAtUtc = Now.AddMinutes(-1), Visibility = ShiftSwapVisibility.Public };

    [Fact]
    public async Task FourInboxClasses_HaveCanonicalIdsAndSecureRecipients()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var (owner, recipient, schedule, slot) = await Seed(db);
        var ordinary = Offer(owner, schedule, slot);
        var manualSlot = TestDataFactory.CreateDalSlot(schedule.Id, 2, 1, null, "08:00", "16:00"); db.Add(manualSlot); await db.SaveChangesAsync();
        var manual = Offer(owner, schedule, manualSlot); manual.FromEmployeeId = null; manual.IsManagerCreated = true; manual.ManualColumnId = 1;
        var accepted = Offer(recipient, schedule, slot); accepted.Status = ShiftSwapStatus.Accepted; accepted.AcceptedAtUtc = Now; accepted.RequestKind = ShiftSwapRequestKind.Exchange;
        var group = new AvailabilityGroupModel { Name = "Push", Year = 2026, Month = 10, PublicationStatus = AvailabilityPublicationStatus.Public,
            VisibleFromUtc = Now.AddMinutes(-2), VisibleToUtc = Now.AddHours(1), Members = [new() { EmployeeId = recipient }] };
        db.AddRange(ordinary, manual, accepted, group);
        await db.SaveChangesAsync();
        var candidates = await Source(db).GetCurrentAsync(recipient, Now, default);
        using var scheduleDto = JsonDocument.Parse(JsonSerializer.Serialize(new WebApi.Contracts.EmployeeSchedules.EmployeeScheduleDto
        { Id = schedule.Id, PublishedAtUtc = schedule.PublishedAtUtc }, new Microsoft.AspNetCore.Mvc.JsonOptions().JsonSerializerOptions));
        var timestamp = scheduleDto.RootElement.GetProperty("publishedAtUtc").GetString();
        var visible = JsonSerializer.Serialize(group.VisibleFromUtc, new JsonSerializerOptions(JsonSerializerDefaults.Web)).Trim('"');
        Assert.Equal(5, candidates.Count);
        Assert.Contains(candidates, x => x.NotificationId == $"schedule-public:{schedule.Id}:{timestamp}");
        Assert.Contains(candidates, x => x.NotificationId == $"availability-public:{group.Id}:{visible}");
        Assert.Contains(candidates, x => x.NotificationId == $"swap-public:{ordinary.Id}" && x.Url == $"/swap?swapId={ordinary.Id}");
        Assert.Contains(candidates, x => x.NotificationId == $"open-shift:{manual.Id}");
        Assert.Contains(candidates, x => x.NotificationId == $"swap-accepted:{accepted.Id}");
        Assert.Equal(new[] { "availability-public:1:2026-10-19T11:58:00+00:00", "open-shift:2", "schedule-public:1:2026-10-19T11:00:00+00:00", "swap-accepted:3", "swap-public:1" },
            candidates.Select(x => x.NotificationId).Order(StringComparer.Ordinal));
        Assert.DoesNotContain(await Source(db).GetCurrentAsync(owner, Now, default), x => x.NotificationId == $"swap-public:{ordinary.Id}" || x.NotificationId == $"swap-accepted:{accepted.Id}");
        Assert.Empty(await Source(db).GetCurrentAsync(999, Now, default));
        ordinary.Visibility = ShiftSwapVisibility.Private; ordinary.TargetEmployeeId = owner;
        manual.ScheduleSlotId = null;
        group.VisibleFromUtc = Now.AddMinutes(1);
        schedule.PublicationStatus = SchedulePublicationStatus.Private;
        await db.SaveChangesAsync();
        Assert.Empty(await Source(db).GetCurrentAsync(recipient, Now, default));
    }

    [Fact]
    public async Task Availability_WaitsForStartAndExcludesExpiredPrivateAndNonmember()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var (_, recipient, _, _) = await Seed(db);
        var group = new AvailabilityGroupModel { Name = "Future", Year = 2026, Month = 10, PublicationStatus = AvailabilityPublicationStatus.Public,
            VisibleFromUtc = Now.AddSeconds(30), VisibleToUtc = Now.AddHours(1), Members = [new() { EmployeeId = recipient }] };
        db.Add(group); await db.SaveChangesAsync();
        Assert.DoesNotContain(await Source(db).GetCurrentAsync(recipient, Now, default), x => x.Tone == "availability");
        Assert.Contains(await Source(db).GetCurrentAsync(recipient, Now.AddSeconds(30), default), x => x.Tone == "availability");
        Assert.DoesNotContain(await Source(db).GetCurrentAsync(recipient, Now.AddHours(2), default), x => x.Tone == "availability");
        group.PublicationStatus = AvailabilityPublicationStatus.Private; await db.SaveChangesAsync();
        Assert.DoesNotContain(await Source(db).GetCurrentAsync(recipient, Now.AddMinutes(1), default), x => x.Tone == "availability");
    }

    [Fact]
    public async Task RetentionAndCap_ExcludeBoundaryAndCancellation()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var (owner, recipient, schedule, slot) = await Seed(db);
        for (var i = 0; i < 85; i++)
        {
            var nextSlot = TestDataFactory.CreateDalSlot(schedule.Id, 2, i + 1, owner, $"{i / 60:D2}:{i % 60:D2}", "16:00");
            var offer = Offer(owner, schedule, slot); offer.ScheduleSlot = nextSlot; db.Add(offer);
        }
        var old = Offer(owner, schedule, slot); old.CreatedAtUtc = Now.AddDays(-5); db.Add(old);
        var cancelled = Offer(owner, schedule, slot); cancelled.Status = ShiftSwapStatus.Cancelled; db.Add(cancelled);
        await db.SaveChangesAsync();
        var candidates = await Source(db).GetCurrentAsync(recipient, Now, default);
        Assert.Equal(80, candidates.Count);
        Assert.DoesNotContain(candidates, x => x.NotificationId == $"swap-public:{old.Id}" || x.NotificationId == $"swap-public:{cancelled.Id}");
        Assert.Equal(candidates.Count, candidates.DistinctBy(x => x.NotificationId).Count());
    }
}
