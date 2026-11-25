using BusinessLogicLayer.Contracts.Communications;
using BusinessLogicLayer.Services;
using DataAccessLayer.Administration;
using DataAccessLayer.Repositories;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using SkiaSharp;
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

    [Theory]
    [InlineData(SKEncodedImageFormat.Jpeg)]
    [InlineData(SKEncodedImageFormat.Png)]
    [InlineData(SKEncodedImageFormat.Webp)]
    public async Task PhotoFormatsAreDecodedByContentAndStoredAsWebp(SKEncodedImageFormat format)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var storage = new CommunicationGifStorage(new SqliteDatabaseWorkspace(database.DatabasePath));
        using var bitmap = new SKBitmap(32, 20);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        using var paint = new SKPaint { Color = SKColors.Orange };
        canvas.DrawRect(0, 0, 16, 20, paint);
        using var source = new MemoryStream(bitmap.Encode(format, 90).ToArray());

        var saved = await storage.SavePhotoAsync(new FormFile(source, 0, source.Length, "file", "../photo"), default);
        await using var stream = storage.Open(saved.Key);
        Assert.NotNull(stream);
        using var codec = SKCodec.Create(stream);
        using var decoded = SKBitmap.Decode(codec);

        Assert.Equal(SKEncodedImageFormat.Webp, codec.EncodedFormat);
        Assert.Equal((32, 20), (decoded.Width, decoded.Height));
        Assert.Equal("image/webp", saved.ContentType);
        Assert.Equal("photo", saved.FileName);
        Assert.NotEqual(Guid.Empty, saved.Key);
        Assert.Equal(stream.Length, saved.Size);
        if (format != SKEncodedImageFormat.Jpeg) Assert.Equal(0, decoded.GetPixel(25, 10).Alpha);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(3, 3)]
    [InlineData(4, 2)]
    [InlineData(5, 0)]
    [InlineData(6, 2)]
    [InlineData(7, 3)]
    [InlineData(8, 1)]
    public async Task PhotoHonorsExifOrientationAndStripsExifAndComments(byte orientation, int expectedTopLeft)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var storage = new CommunicationGifStorage(new SqliteDatabaseWorkspace(database.DatabasePath));
        using var bitmap = new SKBitmap(48, 32);
        using var canvas = new SKCanvas(bitmap);
        var colors = new[] { SKColors.Red, SKColors.Lime, SKColors.Blue, SKColors.Yellow };
        for (var i = 0; i < colors.Length; i++)
        {
            using var paint = new SKPaint { Color = colors[i] };
            canvas.DrawRect(i % 2 * 24, i / 2 * 16, 24, 16, paint);
        }
        using var jpegData = bitmap.Encode(SKEncodedImageFormat.Jpeg, 95);
        var jpeg = jpegData.ToArray();
        var exif = Convert.FromHexString("FFE1002245786966000049492A0008000000010012010300010000000100000000000000");
        exif[28] = orientation;
        using var source = new MemoryStream([.. jpeg[..2], .. exif, .. Convert.FromHexString("FFFE0006"), .. "Test"u8, .. jpeg[2..]]);

        var saved = await storage.SavePhotoAsync(new FormFile(source, 0, source.Length, "file", "photo.jpg"), default);
        await using var stream = storage.Open(saved.Key);
        using var output = new MemoryStream();
        await stream!.CopyToAsync(output);
        var bytes = output.ToArray();
        using var codec = SKCodec.Create(new MemoryStream(bytes));
        using var decoded = SKBitmap.Decode(codec);

        Assert.Equal(SKEncodedOrigin.TopLeft, codec.EncodedOrigin);
        Assert.Equal(orientation >= 5 ? (32, 48) : (48, 32), (decoded.Width, decoded.Height));
        var expected = colors[expectedTopLeft];
        var actual = decoded.GetPixel(6, 6);
        Assert.InRange(Math.Abs(actual.Red - expected.Red), 0, 25);
        Assert.InRange(Math.Abs(actual.Green - expected.Green), 0, 25);
        Assert.InRange(Math.Abs(actual.Blue - expected.Blue), 0, 25);
        Assert.Equal(-1, bytes.AsSpan().IndexOf("EXIF"u8));
        Assert.Equal(-1, bytes.AsSpan().IndexOf("Test"u8));
    }

    [Theory]
    [InlineData(SKEncodedImageFormat.Jpeg)]
    [InlineData(SKEncodedImageFormat.Png)]
    [InlineData(SKEncodedImageFormat.Webp)]
    public async Task CorruptPhotosNeverCreateAnAttachment(SKEncodedImageFormat format)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var storage = new CommunicationGifStorage(new SqliteDatabaseWorkspace(database.DatabasePath));
        using var bitmap = new SKBitmap(128, 128);
        bitmap.Erase(SKColors.Purple);
        using var encoded = bitmap.Encode(format, 90);
        using var source = new MemoryStream(encoded.ToArray()[..((int)encoded.Size / 2)]);

        var error = await Assert.ThrowsAsync<BadHttpRequestException>(() =>
            storage.SavePhotoAsync(new FormFile(source, 0, source.Length, "file", "fake.jpg"), default));

        Assert.Equal("Unsupported file format", error.Message);
        Assert.False(Directory.Exists(Path.Combine(database.RootPath, "communication-gifs")));
    }

    [Fact]
    public async Task PhotoRejectsOversizedFilesAndDecodesNeitherOverLimitNorGif()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var storage = new CommunicationGifStorage(new SqliteDatabaseWorkspace(database.DatabasePath));
        // A PNG signature and IHDR declaring 4001 × 4000 pixels. The following IDAT is deliberately empty.
        using var oversizedCanvas = new MemoryStream(Convert.FromHexString(
            "89504E470D0A1A0A0000000D4948445200000FA100000FA0080600000001BD883C000000004944415435AF061E0000000049454E44AE426082"));
        using var gif = new MemoryStream(Convert.FromHexString(Header + Frame + Frame + "3B"));
        using var bytes = new MemoryStream(new byte[CommunicationGifStorage.MaxGifBytes + 1]);

        await Reject(oversizedCanvas, "File too large");
        await Reject(gif, "Unsupported file format");
        await Reject(bytes, "File too large");
        Assert.False(Directory.Exists(Path.Combine(database.RootPath, "communication-gifs")));

        async Task Reject(Stream source, string message)
        {
            var error = await Assert.ThrowsAsync<BadHttpRequestException>(() =>
                storage.SavePhotoAsync(new FormFile(source, 0, source.Length, "file", "photo.png"), default));
            Assert.Equal(message, error.Message);
        }
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
