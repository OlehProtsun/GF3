using BusinessLogicLayer.Contracts.Communications;
using BusinessLogicLayer.Services;
using DataAccessLayer.Administration;
using DataAccessLayer.Repositories;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using WebApi.Services;
using Xunit;

namespace GF3.Tests;

public sealed class CommunicationGifTests
{
    private const string Header = "47494638396101000100800000000000FFFFFF";
    private const string Frame = "2C0000000001000100000202440100";

    [Fact]
    public async Task AnimatedGifIsStoredUnderAnOpaqueKeyAndNormalizedToLoop()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var storage = new CommunicationGifStorage(new SqliteDatabaseWorkspace(database.DatabasePath));
        var body = Convert.FromHexString(Header + Frame + Frame + "3B");
        using var source = new MemoryStream(body);
        var upload = new FormFile(source, 0, source.Length, "file", "../demo.gif");

        var attachment = await storage.SaveAsync(upload, CancellationToken.None);
        await using var stream = storage.Open(attachment.Key);
        Assert.NotNull(stream);
        using var result = new MemoryStream();
        await stream!.CopyToAsync(result);

        Assert.Equal("demo.gif", attachment.FileName);
        Assert.Equal((long)attachment.Size, result.Length);
        Assert.Contains("NETSCAPE2.0", System.Text.Encoding.ASCII.GetString(result.ToArray()));
        Assert.Equal(
            new byte[] { 0x03, 0x01, 0x00, 0x00, 0x00 },
            result.ToArray().AsSpan(Header.Length / 2 + 14, 5).ToArray());
        Assert.Null(storage.Open(Guid.NewGuid()));
    }

    [Theory]
    [InlineData("3C68746D6C3E")]
    [InlineData(Header + Frame + "3B")]
    [InlineData(Header + Frame + Frame)]
    [InlineData(Header + Frame + Frame + "3B00")]
    public async Task ExtensionCannotBypassGifValidation(string hex)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var storage = new CommunicationGifStorage(new SqliteDatabaseWorkspace(database.DatabasePath));
        using var source = new MemoryStream(Convert.FromHexString(hex));
        var upload = new FormFile(source, 0, source.Length, "file", "fake.gif");

        await Assert.ThrowsAsync<BadHttpRequestException>(() => storage.SaveAsync(upload, CancellationToken.None));
        Assert.False(Directory.Exists(Path.Combine(database.RootPath, "communication-gifs")));
    }

    [Fact]
    public async Task EmployeeGifAccessFollowsVisibilityAndDismissal()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var employee = TestDataFactory.CreateDalEmployee("Demo", "Employee");
        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        var service = new CommunicationService(new CommunicationRepository(context));
        var attachment = new CommunicationGifAttachment(Guid.NewGuid(), "demo.gif", 100);
        var now = DateTimeOffset.UtcNow;
        var active = await service.CreateAsync(new CreateCommunicationMessageRequest
        {
            Title = "Demo", Body = "Demo message", Gif = attachment,
            VisibleFromUtc = now.AddMinutes(-1), DeadlineAtUtc = now.AddHours(1),
        }, null, null);

        Assert.Equal(attachment, await service.GetGifAsync(active.Id, employee.Id));
        var edited = await service.UpdateAsync(active.Id, new UpdateCommunicationMessageRequest
        {
            Title = "Scheduled", Body = "Demo message",
            VisibleFromUtc = now.AddMinutes(5), DeadlineAtUtc = now.AddHours(1),
        });
        Assert.Equal(attachment, edited.Gif);
        Assert.Null(await service.GetGifAsync(active.Id, employee.Id));
        Assert.Equal(attachment, await service.GetGifAsync(active.Id, null));

        await service.UpdateAsync(active.Id, new UpdateCommunicationMessageRequest
        {
            Title = "Published", Body = "Demo message",
            VisibleFromUtc = now.AddMinutes(-1), DeadlineAtUtc = now.AddHours(1),
        });
        await service.DismissForEmployeeAsync(employee.Id, active.Id);
        Assert.Null(await service.GetGifAsync(active.Id, employee.Id));
        Assert.DoesNotContain(await service.GetPendingForEmployeeAsync(employee.Id), item => item.Id == active.Id);
        var removed = await service.UpdateAsync(active.Id, new UpdateCommunicationMessageRequest
        {
            Title = "Text only", Body = "Demo message", RemoveGif = true,
            VisibleFromUtc = now.AddMinutes(-1), DeadlineAtUtc = now.AddHours(1),
        });
        Assert.Null(removed.Gif);
        Assert.Null(await service.GetGifAsync(active.Id, null));
    }
}
