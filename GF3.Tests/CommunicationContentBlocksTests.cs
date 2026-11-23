using System.Security.Claims;
using System.Text.Json;
using BusinessLogicLayer.Services;
using DataAccessLayer.Administration;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp;
using WebApi.Auth;
using WebApi.Contracts.Communications;
using WebApi.Controllers;
using WebApi.Services;

namespace GF3.Tests;

public sealed class CommunicationContentBlocksTests
{
    private static readonly byte[] Gif = Convert.FromHexString("47494638396101000100800000000000FFFFFF" +
        "2C00000000010001000002024401002C00000000010001000002024401003B");

    [Fact]
    public async Task MultipartRoundTripKeepsOrderKeysAndVisibilityThroughReorderAndReplacement()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var employee = TestDataFactory.CreateDalEmployee("Fixture", "Employee");
        db.Add(employee);
        await db.SaveChangesAsync();
        var controller = Controller(db, database);
        var photo = new Block(Guid.NewGuid(), "photo");
        var first = new Block(Guid.NewGuid(), "text", " First text ");
        var gif = new Block(Guid.NewGuid(), "gif");
        var last = new Block(Guid.NewGuid(), "text", "Last text");
        SetFiles(controller, (photo.Id, Photo()), (gif.Id, Gif));
        var created = Dto(await controller.CreateWithGif(Form(photo, first, gif, last), default));
        Assert.Equal(new[] { "photo", "text", "gif", "text" }, created.Blocks!.Select(item => item.Type));
        Assert.Equal("First text\n\nLast text", created.Body);
        var photoKey = created.Blocks![0].Media!.Key;
        var gifKey = created.Blocks![2].Media!.Key;
        Assert.Equal("image/webp", created.Blocks![0].Media!.ContentType);
        Assert.Equal("image/gif", created.Blocks![2].Media!.ContentType);
        var stored = await db.Set<CommunicationMessageModel>().SingleAsync();
        using var json = JsonDocument.Parse(stored.ContentBlocksJson!);
        Assert.Equal(created.Blocks!.Select(item => item.Id), json.RootElement.EnumerateArray().Select(item => item.GetProperty("Id").GetGuid()));

        SetFiles(controller);
        var reordered = Dto(await controller.UpdateWithGif(created.Id, Form(first, gif with { MediaKey = gifKey }, photo with { MediaKey = photoKey }, last), default));
        Assert.Equal(new[] { first.Id, gif.Id, photo.Id, last.Id }, reordered.Blocks!.Select(item => item.Id));
        Assert.Equal(new[] { gifKey, photoKey }, reordered.Blocks!.Where(item => item.Media is not null).Select(item => item.Media!.Key));
        var legacyEdit = Dto(await controller.Update(created.Id, new UpdateCommunicationMessageRequest
        {
            Title = "Legacy title update", Body = reordered.Body,
            VisibleFromUtc = reordered.VisibleFromUtc, DeadlineAtUtc = reordered.DeadlineAtUtc,
        }, default));
        Assert.Equal("Legacy title update", legacyEdit.Title);
        Assert.Equal(reordered.Blocks, legacyEdit.Blocks);
        SetFiles(controller, (photo.Id, Photo()));
        var replaced = Dto(await controller.UpdateWithGif(created.Id, Form(first, gif with { MediaKey = gifKey }, photo), default));
        var newPhoto = replaced.Blocks!.Single(item => item.Id == photo.Id).Media!.Key;
        Assert.NotEqual(photoKey, newPhoto);
        Assert.Equal(gifKey, replaced.Blocks!.Single(item => item.Id == gif.Id).Media!.Key);
        var manager = Assert.Single(Messages(await controller.GetAll(default)));
        SetUser(controller, employee.Id);
        var visible = Assert.Single(Messages(await controller.GetPending(default)));
        Assert.Equal(manager.Blocks!.Select(item => item.Id), visible.Blocks!.Select(item => item.Id));
        Assert.Equal(new[] { first.Id, gif.Id, photo.Id }, visible.Blocks!.Select(item => item.Id));
        Assert.IsType<NotFoundResult>(await controller.GetPhoto(created.Id, photoKey, default));
        Assert.IsType<NotFoundResult>(await controller.GetPhoto(created.Id, gifKey, default));
        var served = Assert.IsType<FileStreamResult>(await controller.GetPhoto(created.Id, newPhoto, default));
        await using (served.FileStream)
        {
            Assert.Equal("image/webp", served.ContentType);
            Assert.Equal("private, no-store", controller.Response.Headers.CacheControl.ToString());
            Assert.Equal("nosniff", controller.Response.Headers["X-Content-Type-Options"].ToString());
        }
        Assert.IsType<NoContentResult>(await controller.Dismiss(created.Id, default));
        Assert.Empty(Messages(await controller.GetPending(default)));
        Assert.IsType<NotFoundResult>(await controller.GetGif(created.Id, gifKey, default));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RejectedDbWriteRemovesOnlyFreshUploads(bool update)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var controller = Controller(db, database);
        var retained = new Block(Guid.NewGuid(), "gif");
        var photo = new Block(Guid.NewGuid(), "photo");
        var text = new Block(Guid.NewGuid(), "text", "Fixture text");
        SetFiles(controller, (retained.Id, Gif));
        var prior = Dto(await controller.CreateWithGif(Form(retained, text), default));
        var key = prior.Blocks![0].Media!.Key;
        var filesBefore = MediaPaths(database);
        Assert.Single(filesBefore);
        if (update)
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_message AFTER UPDATE ON communication_message BEGIN SELECT RAISE(FAIL, 'fixture'); END;");
        else
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_message BEFORE INSERT ON communication_message BEGIN SELECT RAISE(ABORT, 'fixture'); END;");
        SetFiles(controller, (photo.Id, Photo()));

        if (update)
            await Assert.ThrowsAsync<DbUpdateException>(() => controller.UpdateWithGif(prior.Id, Form(text, retained with { MediaKey = key }, photo), default));
        else
            await Assert.ThrowsAsync<DbUpdateException>(() => controller.CreateWithGif(Form(text, photo), default));

        db.ChangeTracker.Clear();
        Assert.Equal(filesBefore, MediaPaths(database));
        Assert.Equal(key, Assert.Single(Messages(await controller.GetAll(default))).Blocks![0].Media!.Key);
    }

    [Theory]
    [InlineData("empty-id")]
    [InlineData("duplicate")]
    [InlineData("unknown-type")]
    [InlineData("unmatched-file")]
    [InlineData("limit")]
    [InlineData("file-limit")]
    [InlineData("upload-limit")]
    [InlineData("fake-image")]
    [InlineData("image-only")]
    public async Task InvalidBlocksLeaveNeitherRowsNorPartiallyUploadedFiles(string scenario)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var controller = Controller(db, database);
        var gif = new Block(Guid.NewGuid(), "gif");
        var text = new Block(Guid.NewGuid(), "text", "Fixture text");
        var second = new Block(Guid.NewGuid(), "photo");
        SetFiles(controller, (gif.Id, Gif));
        var form = Form(gif, text);
        if (scenario == "empty-id") form = Form(gif, text with { Id = Guid.Empty });
        if (scenario == "duplicate") form = Form(gif, text with { Id = gif.Id });
        if (scenario == "unknown-type") form = Form(gif, text with { Type = "video" });
        if (scenario == "unmatched-file") SetFiles(controller, (gif.Id, Gif), (second.Id, Photo()));
        if (scenario == "limit") form = Form(Enumerable.Range(0, 21).Select(_ => text with { Id = Guid.NewGuid() }).ToArray());
        if (scenario == "file-limit") SetFiles(controller, (gif.Id, new byte[CommunicationGifStorage.MaxGifBytes + 1]));
        if (scenario == "upload-limit") SetFiles(controller, (gif.Id, new byte[3 * 1024 * 1024]),
            (second.Id, new byte[3 * 1024 * 1024]), (Guid.NewGuid(), new byte[3 * 1024 * 1024]));
        if (scenario == "fake-image")
        {
            SetFiles(controller, (gif.Id, Gif), (second.Id, "fixture"u8.ToArray()));
            form = Form(gif, second, text);
        }
        if (scenario == "image-only") form = Form(gif);

        var error = await Record.ExceptionAsync(() => controller.CreateWithGif(form, default));
        Assert.True(error is BadHttpRequestException or BusinessLogicLayer.Common.ValidationException, error?.ToString());
        Assert.Empty(await db.Set<CommunicationMessageModel>().ToListAsync());
        Assert.Empty(MediaPaths(database));
    }

    private static CommunicationGifForm Form(params Block[] blocks) => new()
    {
        Title = "Fixture message", VisibleFromUtc = DateTimeOffset.UtcNow.AddMinutes(-1), DeadlineAtUtc = DateTimeOffset.UtcNow.AddHours(2),
        Blocks = JsonSerializer.Serialize(blocks, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
    };

    private static CommunicationsController Controller(AppDbContext db, SqliteTestDatabase database)
    {
        var controller = new CommunicationsController(new CommunicationService(new CommunicationRepository(db)), new NoopWorkflowLogService(),
            new CommunicationGifStorage(new SqliteDatabaseWorkspace(database.DatabasePath)), NullLogger<CommunicationsController>.Instance);
        SetUser(controller);
        return controller;
    }

    private static void SetUser(CommunicationsController controller, int? employeeId = null)
    {
        var claims = employeeId is int id ? new[] { new Claim(ClaimTypes.Role, AuthRoles.Employee), new Claim("employee_id", id.ToString()) }
            : [new Claim(ClaimTypes.Role, AuthRoles.Manager)];
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) },
        };
    }

    private static void SetFiles(CommunicationsController controller, params (Guid Id, byte[] Bytes)[] uploads)
    {
        var files = new FormFileCollection();
        foreach (var (id, bytes) in uploads)
            files.Add(new FormFile(new MemoryStream(bytes), 0, bytes.Length, $"Block-{id:D}", "fixture"));
        controller.HttpContext.Features.Set<IFormFeature>(new FormFeature(new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(), files)));
    }

    private static string[] MediaPaths(SqliteTestDatabase database)
    {
        var media = Path.Combine(database.RootPath, "communication-gifs");
        return Directory.Exists(media) ? Directory.GetFiles(media).OrderBy(path => path).ToArray() : [];
    }

    private static byte[] Photo()
    {
        using var bitmap = new SKBitmap(4, 4);
        bitmap.Erase(SKColors.CornflowerBlue);
        using var png = bitmap.Encode(SKEncodedImageFormat.Png, 90);
        return png.ToArray();
    }

    private static CommunicationMessageDto Dto(ActionResult<CommunicationMessageDto> result)
        => Assert.IsType<CommunicationMessageDto>(Assert.IsType<OkObjectResult>(result.Result).Value);

    private static IReadOnlyList<CommunicationMessageDto> Messages(ActionResult<IReadOnlyList<CommunicationMessageDto>> result)
        => Assert.IsAssignableFrom<IReadOnlyList<CommunicationMessageDto>>(Assert.IsType<OkObjectResult>(result.Result).Value);

    private sealed record Block(Guid Id, string Type, string? Text = null, Guid? MediaKey = null);
}
