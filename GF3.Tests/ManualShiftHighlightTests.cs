using System.Text.Json.Nodes;
using BusinessLogicLayer.Common;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WebApi.Contracts.ShiftSwaps;
using WebApi.ShiftSwaps;
using static GF3.Tests.Infrastructure.ShiftSwapScenarioFactory;
using static WebApi.ShiftSwaps.ShiftSwapRules;
using WebApi.Realtime;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusinessLogicLayer.Contracts.Auth;
using DataAccessLayer.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebApi.Auth;
using WebApi.Infrastructure;
using WebApi.Options;

namespace GF3.Tests;

public sealed class ManualShiftHighlightTests
{
    [Fact]
    public async Task RealHttp_CrossRoleSettingAndAcceptance_PersistBothCells()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var setup = database.CreateContext();
        var f = await SeedAsync(setup, new());
        var schedule = await setup.Schedules.SingleAsync(s => s.Id == f.SourceSchedule);
        schedule.Note = "[[GF3_GRAPH_META:{\"m\":[[7,\"Generated\",{\"10\":\"09:00-17:00\",\"11\":\"09:00-17:00\",\"12\":\"09:00-17:00\"}]],\"g\":[7],\"o\":[1,-7]}]]";
        var manager = new ManagerAccountModel { Username = "manual-test-manager", DisplayName = "Manager", PasswordHash = "synthetic-test-hash", Language = "en", PasswordUpdatedAtUtc = DateTimeOffset.UtcNow };
        setup.Add(manager);
        setup.EmployeeAccounts.Add(new EmployeeAccountModel { EmployeeId = f.Target, Username = "manual-test-employee", PasswordHash = "synthetic-test-hash", Language = "en" });
        await setup.SaveChangesAsync();
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> {
            ["ConnectionStrings:Default"] = database.ConnectionString,
            ["Jwt:SigningKey"] = "manual-highlight-integration-test-signing-key-only",
            ["Jwt:Issuer"] = "manual-tests", ["Jwt:Audience"] = "manual-tests",
        });
        builder.Services.AddWebApiCore(builder.Configuration);
        builder.Services.AddControllers().AddApplicationPart(typeof(WebApi.Controllers.ShiftSwapLogsController).Assembly);
        await using var app = builder.Build();
        app.UseAuthentication(); app.UseAuthorization(); app.MapControllers();
        app.MapHub<EmployeePresenceHub>(EmployeePresenceHub.RoutePattern);
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
        var frontend = Path.Combine(root, "FrontEnd");
        if (Directory.Exists(Path.Combine(frontend, "dist")))
        {
            var files = new PhysicalFileProvider(Path.Combine(frontend, "dist"));
            app.UseStaticFiles(new StaticFileOptions { FileProvider = files });
            app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = files });
        }
        await app.StartAsync();
        var tokens = new JwtTokenService(app.Services.GetRequiredService<IOptions<JwtAuthOptions>>());
        var managerToken = tokens.CreateAccessToken(new AuthenticatedSessionDto { Role = AuthRoles.Manager, UserName = manager.Username,
            DisplayName = manager.DisplayName, ManagerId = manager.Id, CredentialVersion = manager.PasswordUpdatedAtUtc.UtcTicks }).AccessToken;
        var employeeToken = tokens.CreateAccessToken(new AuthenticatedSessionDto { Role = AuthRoles.Employee, UserName = "manual-test-employee", EmployeeId = f.Target }).AccessToken;
        using var managerClient = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        using var employeeClient = new HttpClient { BaseAddress = managerClient.BaseAddress };
        var setting = $"/api/containers/{f.Container}/graphs/{f.SourceSchedule}/shift-swaps/manual-highlight-setting";
        Assert.Equal(HttpStatusCode.Unauthorized, (await managerClient.GetAsync(setting)).StatusCode);
        employeeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await employeeClient.PutAsJsonAsync(setting, new { highlightColor = "#AABBCC" })).StatusCode);
        managerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        Assert.Equal(HttpStatusCode.BadRequest, (await managerClient.PutAsJsonAsync(setting, new { highlightColor = "#ABC" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await managerClient.GetAsync(setting.Replace($"graphs/{f.SourceSchedule}", "graphs/999999"))).StatusCode);
        var saved = await managerClient.PutAsJsonAsync(setting, new { highlightColor = "#aabbcc" });
        saved.EnsureSuccessStatusCode();
        Assert.Equal("#AABBCC", (await saved.Content.ReadFromJsonAsync<ShiftSwapHighlightSettingDto>())!.HighlightColor);
        var graphPath = $"/api/containers/{f.Container}/graphs/{f.SourceSchedule}";
        var ordinarySetting = setting.Replace("manual-highlight-setting", "highlight-setting");
        (await managerClient.PutAsJsonAsync(ordinarySetting, new { highlightColor = "#FED7AA" })).EnsureSuccessStatusCode();
        var graphPayload = await managerClient.GetFromJsonAsync<JsonObject>(graphPath);
        (await managerClient.PutAsJsonAsync(graphPath, graphPayload)).EnsureSuccessStatusCode();
        Assert.Equal("#AABBCC", (await managerClient.GetFromJsonAsync<ShiftSwapHighlightSettingDto>(setting))!.HighlightColor);
        Assert.Equal("#FED7AA", (await managerClient.GetFromJsonAsync<ShiftSwapHighlightSettingDto>(ordinarySetting))!.HighlightColor);
        var offered = await managerClient.PostAsJsonAsync($"/api/containers/{f.Container}/graphs/{f.SourceSchedule}/shift-swaps/manual",
            new { manualColumnId = 7, dayOfMonth = 12, fromTime = "09:00", toTime = "17:00" });
        offered.EnsureSuccessStatusCode();
        var offer = (await offered.Content.ReadFromJsonAsync<ShiftSwapDto>())!;
        (await employeeClient.PostAsync($"/api/employee-shift-swaps/{offer.Id}/accept", null)).EnsureSuccessStatusCode();
        await using var fresh = database.CreateContext();
        Assert.Equal(ShiftSwapHighlightRules.ToArgb("#AABBCC"), (await fresh.ScheduleCellStyles.SingleAsync()).BackgroundColorArgb);
        Assert.Contains("b64:", (await fresh.Schedules.SingleAsync(s => s.Id == f.SourceSchedule)).Note);
        if (Environment.GetEnvironmentVariable("GF3_RUN_LIVE_BROWSER") == "1")
        {
            var start = new ProcessStartInfo("node") { WorkingDirectory = frontend, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            foreach (var arg in new[] { "node_modules/@playwright/test/cli.js", "test", "e2e/generated-manual-shifts.spec.ts", "--grep", "real backend manual acceptance", "--project=chromium", "--project=webkit-laptop", "--workers=1", "--reporter=line", "--output=../GF3.Tests/TestResults/live-browser" }) start.ArgumentList.Add(arg);
            start.Environment["GF3_LIVE_URL"] = app.Urls.Single();
            start.Environment["GF3_LIVE_MANAGER_TOKEN"] = managerToken;
            start.Environment["GF3_LIVE_EMPLOYEE_TOKEN"] = employeeToken;
            start.Environment["GF3_LIVE_CONTAINER"] = f.Container.ToString();
            start.Environment["GF3_LIVE_GRAPH"] = f.SourceSchedule.ToString();
            start.Environment["GF3_LIVE_EMPLOYEE"] = f.Target.ToString();
            using var process = Process.Start(start)!;
            var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
            try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromMinutes(4)); }
            catch { process.Kill(entireProcessTree: true); throw; }
            var log = await stdout + await stderr;
            Directory.CreateDirectory(Path.Combine(root, "GF3.Tests", "TestResults"));
            await File.WriteAllTextAsync(Path.Combine(root, "GF3.Tests", "TestResults", "manual-live-browser.log"), log);
            Assert.True(process.ExitCode == 0, log);
        }
        await app.StopAsync();
    }

    [Fact]
    public async Task LockedSetting_LeavesEveryTableUnchanged()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await SeedAsync(db, new());
        var locks = new ManagerEditLockService();
        locks.SetLocks("other-connection", 99, "Other manager", [ManagerEditLockTargets.Schedule(f.Container, f.SourceSchedule)]);
        var before = await SnapshotAsync(db);
        var result = await ManagerController(db, locks: locks).SaveManualHighlightSetting(f.Container, f.SourceSchedule,
            new() { HighlightColor = "#AABBCC" }, default);
        Assert.Equal(409, Assert.IsType<ConflictObjectResult>(result.Result).StatusCode);
        await AssertUnchangedAfterFailure(database, before);
    }
    [Fact]
    public async Task Setting_PersistsNormalizedColor_IndependentlyPerGraph()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await SeedAsync(db, new());
        var manager = ManagerController(db);
        var initial = await manager.GetManualHighlightSetting(f.Container, f.SourceSchedule, default);
        Assert.Equal("#BAE6FD", Assert.IsType<ShiftSwapHighlightSettingDto>(Assert.IsType<OkObjectResult>(initial.Result).Value).HighlightColor);
        await manager.SaveManualHighlightSetting(f.Container, f.SourceSchedule, new() { HighlightColor = "#aabbcc" }, default);
        await using var fresh = database.CreateContext();
        var source = await fresh.Schedules.SingleAsync(s => s.Id == f.SourceSchedule);
        Assert.Equal("#AABBCC", source.AcceptedManualShiftHighlightColor);
        Assert.Equal("#BBF7D0", source.AcceptedSwapHighlightColor);
        Assert.Equal("#BAE6FD", (await fresh.Schedules.SingleAsync(s => s.Id == f.ReturnSchedule)).AcceptedManualShiftHighlightColor);
    }

    [Theory]
    [InlineData("#ABC")][InlineData("")][InlineData("#GGGGGG")]
    public async Task InvalidSetting_LeavesEveryTableUnchanged(string color)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await SeedAsync(db, new());
        var before = await SnapshotAsync(db);
        await Assert.ThrowsAsync<ValidationException>(() => ManagerController(db).SaveManualHighlightSetting(
            f.Container, f.SourceSchedule, new() { HighlightColor = color }, default));
        await AssertUnchangedAfterFailure(database, before);
    }

    [Fact]
    public async Task MissingOrWrongContainer_LeavesEveryTableUnchanged()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await SeedAsync(db, new());
        var before = await SnapshotAsync(db);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => ManagerController(db).SaveManualHighlightSetting(
            f.OtherContainer, f.SourceSchedule, new() { HighlightColor = "#AABBCC" }, default));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => ManagerController(db).GetManualHighlightSetting(f.Container, int.MaxValue, default));
        await AssertUnchangedAfterFailure(database, before);
    }

    [Theory]
    [InlineData(false, false)][InlineData(false, true)][InlineData(true, false)][InlineData(true, true)]
    public void NoteUpsert_UsesEffectiveRepresentation_PreservesTextAndMetadata(bool legacy, bool encoded)
    {
        var meta = JsonNode.Parse("{\"m\":[[7,\"Manual\"]],\"g\":[7],\"o\":[1,-7],\"s\":[[-7,1,123,456],[0,2,789]],\"t\":[[1,2,\"text\"]],\"u\":{\"1\":[\"1:2\"]},\"opaque\":42}")!;
        if (legacy) meta["cellStyles"] = new JsonArray(new JsonObject { ["employeeId"] = -7, ["dayOfMonth"] = 1, ["textColorArgb"] = 456 });
        var raw = meta.ToJsonString();
        var prefix = "visible text\n\n";
        var note = prefix + "[[GF3_GRAPH_META:" + (encoded ? "b64:" + EncodeGraphNoteMetaValue(raw) : raw) + "]]";
        note = UpsertManualColumnBackgroundColorInNote(note, 7, 1, -4528387);
        note = UpsertManualColumnBackgroundColorInNote(note, 7, 1, -4528387);
        Assert.StartsWith(prefix, note);
        var after = ParseGraphNoteMeta(note.Split("[[GF3_GRAPH_META:")[1][..^2])!;
        foreach (var key in new[] { "m", "g", "o", "t", "u", "opaque" }) Assert.True(JsonNode.DeepEquals(meta[key], after[key]));
        if (legacy)
        {
            var style = Assert.Single((JsonArray)after["cellStyles"]!);
            Assert.Equal(-4528387, (int)style!["backgroundColorArgb"]!);
            Assert.Equal(456, (int)style["textColorArgb"]!);
            Assert.True(JsonNode.DeepEquals(meta["s"], after["s"]));
        }
        else
        {
            var style = Assert.Single(((JsonArray)after["s"]!).OfType<JsonArray>(), s => (int)s[0]! == -7);
            Assert.Equal(-4528387, (int)style[2]!); Assert.Equal(456, (int)style[3]!);
        }
    }

    [Theory]
    [InlineData(0,1)][InlineData(7,0)][InlineData(7,32)][InlineData(8,1)]
    public void NoteUpsert_RejectsMissingColumnOrInvalidCoordinates(int column, int day)
        => Assert.Throws<ValidationException>(() => UpsertManualColumnBackgroundColorInNote("[[GF3_GRAPH_META:{\"m\":[[7,\"Manual\"]]}]]", column, day, 123));

    [Theory]
    [InlineData("09:00", "17:00")][InlineData("09:00", "13:00")]
    [InlineData("13:00", "17:00")][InlineData("11:00", "15:00")]
    public async Task ManualAcceptance_HighlightsBothCells_AndPersistsHistory(string from, string to)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await SeedAsync(db, new());
        var schedule = await db.Schedules.SingleAsync(s => s.Id == f.SourceSchedule);
        schedule.AcceptedManualShiftHighlightColor = "#AABBCC";
        schedule.Note = "[[GF3_GRAPH_META:{\"m\":[[7,\"Manual\",{\"1\":\"09:00-17:00\"}]],\"s\":[[-7,1,null,456]]}]]";
        await db.SaveChangesAsync();
        var created = await ManagerController(db).CreateManualOffer(f.Container, f.SourceSchedule,
            new() { ManualColumnId = 7, DayOfMonth = 1, FromTime = "09:00", ToTime = "17:00", AllowPartialAcceptance = true }, default);
        var offer = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>(created.Result).Value);
        await EmployeeController(db, f.Target).Accept(offer.Id, default, new() { FromTime = from, ToTime = to });
        await using var fresh = database.CreateContext();
        var real = Assert.Single(await fresh.ScheduleCellStyles.ToListAsync());
        Assert.Equal(f.Target, real.EmployeeId); Assert.Equal(1, real.DayOfMonth);
        Assert.Equal(ShiftSwapHighlightRules.ToArgb("#AABBCC"), real.BackgroundColorArgb);
        var note = (await fresh.Schedules.SingleAsync(s => s.Id == f.SourceSchedule)).Note!;
        var meta = ParseGraphNoteMeta(note.Split("[[GF3_GRAPH_META:")[1][..^2])!;
        var style = Assert.IsType<JsonArray>(Assert.Single((JsonArray)meta["s"]!));
        Assert.Equal(-7, (int)style[0]!); Assert.Equal(1, (int)style[1]!);
        Assert.Equal(real.BackgroundColorArgb, (int)style[2]!); Assert.Equal(456, (int)style[3]!);
        Assert.Equal(1, await fresh.ShiftSwapHistories.CountAsync());
        Assert.Equal(2, await fresh.ScheduleVersions.CountAsync());
        var version = await fresh.ScheduleVersions.OrderByDescending(v => v.VersionNumber).FirstAsync();
        var snapshot = WebApi.Services.ScheduleVersionCommitService.DeserializeSnapshot(version.SnapshotJson);
        Assert.Equal(note, snapshot.Note);
        Assert.Equal(real.BackgroundColorArgb, Assert.Single(snapshot.CellStyles).BackgroundColorArgb);
        await AssertNoOverlaps(fresh);
    }

    [Fact]
    public async Task Migration_UpgradeCopyAndRollback_PreservesExistingSchedule()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await SeedAsync(db, new());
        var migrations = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        var previous = migrations[^2];
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(previous);
        var copiedPath = Path.Combine(database.RootPath, "existing-copy.db");
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        File.Copy(database.DatabasePath, copiedPath);
        await using var copy = new DataAccessLayer.Models.DataBaseContext.AppDbContext(
            new DbContextOptionsBuilder<DataAccessLayer.Models.DataBaseContext.AppDbContext>().UseSqlite($"Data Source={copiedPath}").Options);
        await copy.Database.MigrateAsync();
        var schedule = await copy.Schedules.AsNoTracking().SingleAsync(s => s.Id == f.SourceSchedule);
        Assert.Equal("#BAE6FD", schedule.AcceptedManualShiftHighlightColor);
        Assert.Equal("#BBF7D0", schedule.AcceptedSwapHighlightColor);
        await migrator.MigrateAsync();
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Equal("#BAE6FD", (await db.Schedules.AsNoTracking().SingleAsync(s => s.Id == f.SourceSchedule)).AcceptedManualShiftHighlightColor);
    }
}
