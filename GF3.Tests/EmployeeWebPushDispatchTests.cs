using System.Net;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using GF3.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WebApi.Infrastructure;
using WebApi.Options;
using WebApi.Services;
using WebPush;

namespace GF3.Tests;

public sealed class EmployeeWebPushDispatchTests
{
    private sealed class Gateway : HttpMessageHandler
    {
        public List<int> Ttls = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Ttls.Add(int.Parse(request.Headers.GetValues("TTL").Single()));
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://fcm.googleapis.com/send/test", request.RequestUri!.AbsoluteUri);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created));
        }
    }
    [Fact]
    public async Task StandardsSender_EncryptsMultipleSendsAndBoundsTtl()
    {
        var vapid = VapidHelper.GenerateVapidKeys(); var encryption = VapidHelper.GenerateVapidKeys(); var gateway = new Gateway();
        using var http = new HttpClient(gateway);
        using var sender = new EmployeeWebPushSender(http, Options.Create(new PushOptions { Enabled = true, VapidSubject = "mailto:operator@example.com", VapidPublicKey = vapid.PublicKey, VapidPrivateKey = vapid.PrivateKey }), new Clock());
        var subscription = new EmployeePushSubscriptionModel { Endpoint = "https://fcm.googleapis.com/send/test", P256dh = encryption.PublicKey, Auth = EmployeePushControllerTests.Encode(new byte[16]) };
        var now = EmployeePushNotificationSourceTests.Now;
        await sender.SendAsync(subscription, new("swap-public:1", now, "Shift", "Available", "/swap?swapId=1", "swap"), default);
        await sender.SendAsync(subscription, new("schedule-public:1", now.AddDays(-4), "Schedule", "Published", "/schedule", "schedule"), default);
        Assert.Equal([3600, 86400], gateway.Ttls);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendAsync(subscription, new("x", now, "Bad", "Bad", "https://evil.example", "swap"), default));
        subscription.Endpoint = "https://127.0.0.1/test";
        await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendAsync(subscription, new("x", now, "Bad", "Bad", "/schedule", "schedule"), default));
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = EmployeePushNotificationSourceTests.Now;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class Sender : IEmployeeWebPushSender
    {
        public int Calls;
        public Func<CancellationToken, Task>? Send;
        public Task SendAsync(EmployeePushSubscriptionModel subscription, EmployeePushCandidate candidate, CancellationToken ct)
        { Calls++; return Send?.Invoke(ct) ?? Task.CompletedTask; }
    }
    private sealed class Source : IEmployeePushNotificationSource
    {
        public Func<IReadOnlyList<EmployeePushCandidate>>? Read;
        public IReadOnlyList<EmployeePushCandidate> Items = [new("swap-public:1", EmployeePushNotificationSourceTests.Now, "Shift swap available", "View in GF", "/swap?swapId=1", "swap")];
        public Task<IReadOnlyList<EmployeePushCandidate>> GetCurrentAsync(int employeeId, DateTimeOffset nowUtc, CancellationToken ct) => Task.FromResult(Read?.Invoke() ?? Items);
    }

    private static async Task<int> Seed(SqliteTestDatabase database, int devices = 1)
    {
        await using var db = database.CreateContext();
        var employee = TestDataFactory.CreateDalEmployee(); db.Add(employee); await db.SaveChangesAsync();
        for (var i = 0; i < devices; i++) db.Add(new EmployeePushSubscriptionModel
        {
            EmployeeId = employee.Id, Endpoint = $"https://fcm.googleapis.com/send/{i}", P256dh = "test", Auth = "test",
            CreatedAtUtc = EmployeePushNotificationSourceTests.Now.AddMinutes(-1), UpdatedAtUtc = EmployeePushNotificationSourceTests.Now
        });
        await db.SaveChangesAsync(); return employee.Id;
    }
    private static ServiceProvider Provider(SqliteTestDatabase database, Source source, Sender sender) => database.BuildServiceProvider(services =>
    {
        services.AddSingleton<IEmployeePushNotificationSource>(source);
        services.AddSingleton<IEmployeeWebPushSender>(sender);
    });
    private static EmployeeWebPushHostedService Worker(ServiceProvider provider, Clock clock, bool enabled = true) =>
        new(provider.GetRequiredService<IServiceScopeFactory>(), clock, Options.Create(new PushOptions { Enabled = enabled }), NullLogger<EmployeeWebPushHostedService>.Instance);

    [Fact]
    public async Task CancellationBetweenInitialScanAndSend_CannotSendStaleCandidate()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await Seed(database);
        var source = new Source(); var reads = 0; source.Read = () => ++reads == 1 ? source.Items : [];
        var sender = new Sender(); await using var provider = Provider(database, source, sender); using var worker = Worker(provider, new Clock());
        await worker.ScanAsync(default); Assert.Equal(0, sender.Calls);
        await using var db = database.CreateContext(); Assert.Equal("expired", (await db.EmployeePushDeliveries.SingleAsync()).Outcome);
    }

    [Fact]
    public async Task ShutdownCancellation_PropagatesWithoutFinalizingTheLease()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await Seed(database);
        using var cancellation = new CancellationTokenSource();
        var sender = new Sender { Send = ct => { cancellation.Cancel(); ct.ThrowIfCancellationRequested(); return Task.CompletedTask; } };
        await using var provider = Provider(database, new Source(), sender); using var worker = Worker(provider, new Clock());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => worker.ScanAsync(cancellation.Token));
        await using var db = database.CreateContext(); var job = await db.EmployeePushDeliveries.SingleAsync();
        Assert.Null(job.Outcome); Assert.NotNull(job.LeaseToken);
    }

    [Fact]
    public async Task MultipleDevices_DeduplicateAndSkipReadAndOldItems()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); var employee = await Seed(database, 2);
        var source = new Source(); var sender = new Sender(); var clock = new Clock();
        await using var provider = Provider(database, source, sender); using var worker = Worker(provider, clock);
        await worker.ScanAsync(default); await worker.ScanAsync(default);
        Assert.Equal(2, sender.Calls);
        await using var db = database.CreateContext();
        Assert.All(await db.EmployeePushDeliveries.ToListAsync(), x => Assert.Equal("sent", x.Outcome));
        source.Items = [new("swap-public:2", clock.Now, "Read", "Read", "/swap?swapId=2", "swap"), new("swap-public:3", clock.Now.AddDays(-1), "Old", "Old", "/swap?swapId=3", "swap")];
        db.Add(new EmployeeNotificationReadModel { EmployeeId = employee, NotificationId = "swap-public:2", ReadAtUtc = clock.Now }); await db.SaveChangesAsync();
        await worker.ScanAsync(default); Assert.Equal(2, sender.Calls);
        Assert.Equal(2, await db.EmployeePushDeliveries.CountAsync());
    }

    [Theory]
    [InlineData(429, true)] [InlineData(500, true)] [InlineData(503, true)]
    [InlineData(400, false)] [InlineData(401, false)] [InlineData(403, false)]
    public async Task GatewayFailures_RetryOrFinalize(int code, bool retries)
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await Seed(database);
        var source = new Source(); var sender = new Sender { Send = _ => throw new WebPushException("Fake", new PushSubscription("https://fcm.googleapis.com/test", "test", "test"), new HttpResponseMessage((HttpStatusCode)code)) }; var clock = new Clock();
        await using var provider = Provider(database, source, sender); using var worker = Worker(provider, clock);
        await worker.ScanAsync(default);
        await using var db = database.CreateContext(); var job = await db.EmployeePushDeliveries.AsNoTracking().SingleAsync();
        Assert.Equal(1, job.AttemptCount); Assert.Null(job.LeaseToken);
        Assert.Equal(retries ? null : "permanent-failure", job.Outcome);
        await worker.ScanAsync(default); Assert.Equal(1, sender.Calls);
        if (retries)
        {
            Assert.Equal(clock.Now.AddSeconds(60), job.NextAttemptAtUtc);
            clock.Now = job.NextAttemptAtUtc; sender.Send = null; await worker.ScanAsync(default);
            Assert.Equal("sent", (await db.EmployeePushDeliveries.AsNoTracking().SingleAsync()).Outcome);
        }
    }

    [Theory] [InlineData(404)] [InlineData(410)]
    public async Task Gone_DeletesOnlyThatDeviceAndContinues(int code)
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await Seed(database, 2);
        var sender = new Sender(); sender.Send = _ => sender.Calls == 1 ? throw new WebPushException("Fake", new PushSubscription("https://fcm.googleapis.com/test", "test", "test"), new HttpResponseMessage((HttpStatusCode)code)) : Task.CompletedTask;
        await using var provider = Provider(database, new Source(), sender); using var worker = Worker(provider, new Clock());
        await worker.ScanAsync(default);
        await using var db = database.CreateContext(); Assert.Single(await db.EmployeePushSubscriptions.ToListAsync());
        Assert.Equal("sent", (await db.EmployeePushDeliveries.SingleAsync()).Outcome); Assert.Equal(2, sender.Calls);
    }

    [Fact]
    public async Task Lease_IsAtomicAndRecoversAfterCrash()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await Seed(database);
        await using var db = database.CreateContext();
        var subscription = await db.EmployeePushSubscriptions.SingleAsync(); var now = EmployeePushNotificationSourceTests.Now;
        var job = new EmployeePushDeliveryModel { SubscriptionId = subscription.Id, NotificationId = "swap-public:1", CreatedAtUtc = now, NextAttemptAtUtc = now };
        db.Add(job); await db.SaveChangesAsync();
        await using var other = database.CreateContext();
        var claims = await Task.WhenAll(EmployeeWebPushHostedService.TryClaimAsync(db, job.Id, "first", now, default), EmployeeWebPushHostedService.TryClaimAsync(other, job.Id, "second", now, default));
        Assert.Single(claims, x => x);
        Assert.False(await EmployeeWebPushHostedService.TryClaimAsync(other, job.Id, "early", now.AddSeconds(119), default));
        Assert.True(await EmployeeWebPushHostedService.TryClaimAsync(other, job.Id, "recovered", now.AddMinutes(2), default));
    }

    [Fact]
    public async Task Retries_RecheckVisibilityAndReadState()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); var employee = await Seed(database);
        var sender = new Sender { Send = _ => throw new HttpRequestException("Offline") }; var source = new Source(); var clock = new Clock();
        await using var provider = Provider(database, source, sender); using var worker = Worker(provider, clock);
        await worker.ScanAsync(default); source.Items = []; clock.Now = clock.Now.AddMinutes(2); await worker.ScanAsync(default);
        await using var db = database.CreateContext(); Assert.Equal("expired", (await db.EmployeePushDeliveries.SingleAsync()).Outcome); Assert.Equal(1, sender.Calls);
        source.Items = [new("swap-public:2", clock.Now, "Read", "Read", "/swap?swapId=2", "swap")];
        await worker.ScanAsync(default); db.Add(new EmployeeNotificationReadModel { EmployeeId = employee, NotificationId = "swap-public:2", ReadAtUtc = clock.Now }); await db.SaveChangesAsync();
        clock.Now = clock.Now.AddMinutes(2); await worker.ScanAsync(default);
        Assert.Equal("read", (await db.EmployeePushDeliveries.AsNoTracking().SingleAsync(x => x.NotificationId == "swap-public:2")).Outcome);
    }

    [Fact]
    public async Task Timeout_MaxRetriesCleanupAndDisabledWorker()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await Seed(database);
        var sender = new Sender { Send = _ => throw new TaskCanceledException("Timeout") }; var source = new Source(); var clock = new Clock();
        await using var provider = Provider(database, source, sender); using var worker = Worker(provider, clock);
        for (var i = 0; i < 8; i++) { await worker.ScanAsync(default); clock.Now = clock.Now.AddHours(1); }
        await using var db = database.CreateContext(); var job = await db.EmployeePushDeliveries.SingleAsync();
        Assert.Equal(8, job.AttemptCount); Assert.Equal("permanent-failure", job.Outcome);
        source.Items = []; clock.Now = clock.Now.AddDays(6); await worker.ScanAsync(default); Assert.Empty(await db.EmployeePushDeliveries.AsNoTracking().ToListAsync());
        Assert.Single(await db.EmployeePushSubscriptions.ToListAsync());
        using var disabled = Worker(provider, clock, false); await disabled.ScanAsync(default); Assert.Equal(8, sender.Calls);
    }
}
