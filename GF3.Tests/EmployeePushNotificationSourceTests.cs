using System.Text.Json;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using GF3.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WebApi.Services;

namespace GF3.Tests;

public sealed class EmployeePushNotificationSourceTests
{
    [Fact]
    public async Task PublicRequiresSharedMembership_PrivateRequiresTarget_AndArchivedAcceptanceStaysOwned()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext(new FixedClock());
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

    internal sealed class FixedClock : TimeProvider
    {
        public DateTimeOffset Value = Now;
        public override DateTimeOffset GetUtcNow() => Value;
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

    [Theory]
    [InlineData("pl")]
    [InlineData("en")]
    [InlineData("xx")]
    public async Task FourInboxClasses_HaveCanonicalIdsAndSecureRecipients(string language)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext(new FixedClock());
        var (owner, recipient, schedule, slot) = await Seed(db);
        db.Add(new EmployeeAccountModel { EmployeeId = recipient, Username = "recipient", PasswordHash = "hash", Language = language });
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
        var visible = JsonSerializer.Serialize(group.PublishedAtUtc, new JsonSerializerOptions(JsonSerializerDefaults.Web)).Trim('"');
        Assert.Equal(5, candidates.Count);
        Assert.Contains(candidates, x => x.Title == (language == "pl" ? "Opublikowano nowy grafik" : "New schedule published"));
        Assert.Contains(candidates, x => x.Title == (language == "pl" ? "Udostępniono nową dostępność" : "New availability published"));
        Assert.Contains(candidates, x => x.Title == (language == "pl" ? "Dostępna nowa zamiana" : "Shift swap available"));
        Assert.Contains(candidates, x => x.Title == (language == "pl" ? "Opublikowano wolną zmianę" : "Open shift posted"));
        Assert.Contains(candidates, x => x.Title == (language == "pl" ? "Twoja zamiana została zaakceptowana" : "Your shift was accepted"));
        Assert.Contains(candidates, x => x.NotificationId == $"schedule-public:{schedule.Id}:{timestamp}");
        Assert.Contains(candidates, x => x.NotificationId == $"availability-public:{group.Id}:published:{visible}");
        Assert.Contains(candidates, x => x.NotificationId == $"swap-public:{ordinary.Id}" && x.Url == $"/swap?swapId={ordinary.Id}");
        Assert.Contains(candidates, x => x.NotificationId == $"open-shift:{manual.Id}");
        Assert.Contains(candidates, x => x.NotificationId == $"swap-accepted:{accepted.Id}");
        using var contract = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "notificationContract.fixture.json")));
        Assert.Equal(contract.RootElement.GetProperty("ids").EnumerateArray().Select(x => x.GetString()),
            candidates.Select(x => x.NotificationId).Order(StringComparer.Ordinal));
        Assert.Equal(Now, contract.RootElement.GetProperty("nowUtc").GetDateTimeOffset());
        Assert.Equal(EmployeeNotificationRetention.Duration.TotalDays, contract.RootElement.GetProperty("retentionDays").GetInt32());
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
    public async Task Availability_WaitsForStartAndRetainsClosedFormsUntilRetentionExpires()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext(new FixedClock());
        var (_, recipient, _, _) = await Seed(db);
        var group = new AvailabilityGroupModel { Name = "Future", Year = 2026, Month = 10, PublicationStatus = AvailabilityPublicationStatus.Public,
            VisibleFromUtc = Now.AddSeconds(30), VisibleToUtc = Now.AddHours(1), Members = [new() { EmployeeId = recipient }] };
        db.Add(group); await db.SaveChangesAsync();
        Assert.DoesNotContain(await Source(db).GetCurrentAsync(recipient, Now, default), x => x.Tone == "availability");
        Assert.Contains(await Source(db).GetCurrentAsync(recipient, Now.AddSeconds(30), default), x => x.Tone == "availability");
        Assert.Contains(await Source(db).GetCurrentAsync(recipient, Now.AddHours(2), default), x => x.Tone == "availability" && x.Body.Contains("Submissions have closed"));
        Assert.DoesNotContain(await Source(db).GetCurrentAsync(recipient, Now.AddDays(5).AddSeconds(30), default), x => x.Tone == "availability");
        group.PublicationStatus = AvailabilityPublicationStatus.Private; await db.SaveChangesAsync();
        Assert.DoesNotContain(await Source(db).GetCurrentAsync(recipient, Now.AddMinutes(1), default), x => x.Tone == "availability");
    }

    [Fact]
    public async Task RetentionAndCap_ExcludeBoundaryAndCancellation()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext(new FixedClock());
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

    [Fact]
    public async Task PublicationMembershipAndInbox_UseRealEligibilityAndPreserveMetadataOnPartialUpdates()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var clock = new FixedClock();
        await using var db = database.CreateContext(clock);
        var (owner, recipient, _, _) = await Seed(db);
        var group = new AvailabilityGroupModel { Name = "November", Year = 2026, Month = 11,
            VisibleFromUtc = Now.AddHours(-3), VisibleToUtc = Now.AddHours(1), Members = [new() { EmployeeId = recipient }] };
        db.Add(group); await db.SaveChangesAsync();
        Assert.Null(group.PublishedAtUtc);
        Assert.DoesNotContain(await Source(db).GetCurrentAsync(recipient, Now, default), x => x.Tone == "availability");
        group.PublicationStatus = AvailabilityPublicationStatus.Public;
        await db.SaveChangesAsync();
        var candidate = Assert.Single(await Source(db).GetCurrentAsync(recipient, Now, default), x => x.Tone == "availability");
        Assert.Equal(Now, candidate.OccurredAtUtc);
        var service = new BusinessLogicLayer.Services.AvailabilityGroupService(
            new DataAccessLayer.Repositories.AvailabilityGroupRepository(db),
            new DataAccessLayer.Repositories.AvailabilityGroupMemberRepository(db),
            new DataAccessLayer.Repositories.AvailabilityGroupDayRepository(db));
        var inbox = Assert.Single(await service.GetPublishedForEmployeeAsync(recipient, Now));
        Assert.Equal(candidate.OccurredAtUtc, inbox.NotificationOccurredAtUtc);
        Assert.Equal(group.PublishedAtUtc, inbox.PublishedAtUtc);
        Assert.Equal(candidate.NotificationId, inbox.NotificationId);
        clock.Value = Now.AddMinutes(5);
        await service.UpdateAsync(new() { Id = group.Id, Name = "Renamed", Year = 2026, Month = 11,
            PublicationStatus = BusinessLogicLayer.Contracts.Enums.AvailabilityPublicationStatus.Public,
            VisibleFromUtc = Now.AddHours(-2), VisibleToUtc = Now.AddHours(1) });
        var edited = Assert.Single(await Source(db).GetCurrentAsync(recipient, clock.Value, default), x => x.Tone == "availability");
        Assert.Equal(candidate.NotificationId, edited.NotificationId);
        Assert.Equal(candidate.OccurredAtUtc, edited.OccurredAtUtc);
        db.ChangeTracker.Clear();
        group = await db.AvailabilityGroups.Include(x => x.Members).SingleAsync(x => x.Id == group.Id);
        group.Members.Add(new() { EmployeeId = owner }); await db.SaveChangesAsync();
        var added = Assert.Single(await Source(db).GetCurrentAsync(owner, clock.Value, default), x => x.Tone == "availability");
        Assert.Equal(clock.Value, added.OccurredAtUtc);
        db.Remove(group.Members.Single(x => x.EmployeeId == recipient)); await db.SaveChangesAsync();
        Assert.DoesNotContain(await Source(db).GetCurrentAsync(recipient, clock.Value, default), x => x.Tone == "availability");
        group.PublicationStatus = AvailabilityPublicationStatus.Private; await db.SaveChangesAsync();
        Assert.DoesNotContain(await Source(db).GetCurrentAsync(owner, clock.Value, default), x => x.Tone == "availability");
        clock.Value = Now.AddMinutes(10);
        group.PublicationStatus = AvailabilityPublicationStatus.Public; await db.SaveChangesAsync();
        var republished = Assert.Single(await Source(db).GetCurrentAsync(owner, clock.Value, default), x => x.Tone == "availability");
        Assert.NotEqual(candidate.NotificationId, republished.NotificationId);
        Assert.Equal(clock.Value, republished.OccurredAtUtc);
    }

    [Fact]
    public async Task ScheduledPublication_CanBeRescheduledBeforeVisibility_WithoutRenewingAnExistingEvent()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var clock = new FixedClock(); await using var db = database.CreateContext(clock);
        var (_, recipient, _, _) = await Seed(db);
        var group = new AvailabilityGroupModel { Name = "Future", Year = 2026, Month = 11, PublicationStatus = AvailabilityPublicationStatus.Public,
            VisibleFromUtc = Now.AddHours(1), Members = [new() { EmployeeId = recipient }] };
        db.Add(group); await db.SaveChangesAsync();
        clock.Value = Now.AddMinutes(5); group.VisibleFromUtc = Now.AddHours(2); await db.SaveChangesAsync();
        Assert.DoesNotContain(await Source(db).GetCurrentAsync(recipient, Now.AddHours(1), default), x => x.Tone == "availability");
        var candidate = Assert.Single(await Source(db).GetCurrentAsync(recipient, Now.AddHours(2), default), x => x.Tone == "availability");
        Assert.Equal(Now.AddHours(2), candidate.OccurredAtUtc);
        clock.Value = Now.AddHours(3); group.VisibleFromUtc = Now.AddHours(4); await db.SaveChangesAsync();
        var unchanged = Assert.Single(await Source(db).GetCurrentAsync(recipient, Now.AddHours(4), default), x => x.Tone == "availability");
        Assert.Equal(candidate.NotificationId, unchanged.NotificationId);
        Assert.Equal(candidate.OccurredAtUtc, unchanged.OccurredAtUtc);
    }

    [Fact]
    public async Task Migration_PreservesLegacyDataAndReadIds_WithoutReplayingUnknownPublications()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext(new FixedClock());
        var (_, recipient, _, _) = await Seed(db);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261019144937_AddEmployeeWebPush");
        var now = DateTimeOffset.UtcNow;
        var past = now.AddHours(-1); var future = now.AddHours(1);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO availability_group (id, name, year, month, publication_status, visible_from_utc)
            VALUES (100, 'Existing visible', 2026, 11, 'Public', {past}), (101, 'Existing scheduled', 2026, 11, 'Public', {future});
            """);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO availability_group_member (availability_group_id, employee_id, display_order)
            VALUES (100, {recipient}, 0), (101, {recipient}, 1);
            """);
        var legacyId = $"availability-public:100:{past:O}";
        db.Add(new EmployeeNotificationReadModel { EmployeeId = recipient, NotificationId = legacyId, ReadAtUtc = now });
        db.Add(new EmployeePushSubscriptionModel { EmployeeId = recipient, Endpoint = "https://fcm.googleapis.com/send/existing",
            P256dh = "existing-key", Auth = "existing-auth", CreatedAtUtc = now.AddDays(-1), UpdatedAtUtc = now });
        await db.SaveChangesAsync();
        await migrator.MigrateAsync(); db.ChangeTracker.Clear();
        Assert.False(db.Database.HasPendingModelChanges());
        var old = await db.AvailabilityGroups.Include(x => x.Members).SingleAsync(x => x.Id == 100);
        Assert.Null(old.PublishedAtUtc); Assert.Null(old.NotificationAvailableAtUtc); Assert.Null(Assert.Single(old.Members).JoinedAtUtc);
        var scheduled = await db.AvailabilityGroups.SingleAsync(x => x.Id == 101);
        Assert.Null(scheduled.PublishedAtUtc); Assert.Equal(future, scheduled.NotificationAvailableAtUtc);
        Assert.DoesNotContain(await Source(db).GetCurrentAsync(recipient, now, default), x => x.Tone == "availability");
        var candidate = Assert.Single(await Source(db).GetCurrentAsync(recipient, future, default), x => x.Tone == "availability");
        Assert.Equal(future, candidate.OccurredAtUtc);
        Assert.Equal($"availability-public:101:{JsonSerializer.Serialize(future, new JsonSerializerOptions(JsonSerializerDefaults.Web)).Trim('"')}", candidate.NotificationId);
        Assert.Equal(legacyId, (await db.EmployeeNotificationReads.SingleAsync()).NotificationId);
        Assert.Equal("existing-key", (await db.EmployeePushSubscriptions.SingleAsync()).P256dh);
        var idBeforeEdit = AvailabilityNotificationTiming.Id(scheduled);
        var occurredBeforeEdit = AvailabilityNotificationTiming.OccurredAt(old, Assert.Single(old.Members));
        old.VisibleFromUtc = now.AddHours(2); scheduled.VisibleFromUtc = now.AddHours(3);
        await db.SaveChangesAsync();
        Assert.Equal(idBeforeEdit, AvailabilityNotificationTiming.Id(scheduled));
        Assert.Equal(occurredBeforeEdit, AvailabilityNotificationTiming.OccurredAt(old, Assert.Single(old.Members)));
    }

    [Fact]
    public async Task LegacyRowsStillCountTowardTheInboxCap_WithoutBackfillingThemOrSendingRowsOutsideTheInbox()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext(new FixedClock());
        var (owner, recipient, schedule, slot) = await Seed(db);
        db.Add(Offer(owner, schedule, slot));
        for (var i = 0; i < 80; i++) db.Add(new AvailabilityGroupModel
        {
            Name = $"Legacy {i}", Year = 2026, Month = 11, PublicationStatus = AvailabilityPublicationStatus.Public,
            VisibleFromUtc = Now.AddSeconds(-1), Members = [new() { EmployeeId = recipient }]
        });
        await db.SaveChangesAsync();
        await db.AvailabilityGroups.ExecuteUpdateAsync(x => x.SetProperty(g => g.PublishedAtUtc, (DateTimeOffset?)null)
            .SetProperty(g => g.NotificationAvailableAtUtc, (DateTimeOffset?)null)
            .SetProperty(g => g.NotificationLegacyVisibleFromUtc, Now.AddSeconds(-1)));
        await db.AvailabilityGroupMembers.ExecuteUpdateAsync(x => x.SetProperty(m => m.JoinedAtUtc, (DateTimeOffset?)null));
        Assert.Empty(await Source(db).GetCurrentAsync(recipient, Now, default));
    }
}
