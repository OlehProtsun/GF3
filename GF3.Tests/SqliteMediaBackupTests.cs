using System.Text.Json;
using DataAccessLayer.Administration;
using DataAccessLayer.Models;
using GF3.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GF3.Tests;

public sealed class SqliteMediaBackupTests
{
    [Fact]
    public async Task BackupCopiesOnlyReferencedMediaAndMaterializesIndependentWorkingCopy()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        using var vectors = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "communication-media-vectors.json")));
        var key = vectors.RootElement.GetProperty("legacy").GetProperty("Key").GetGuid();
        var bytes = new byte[] { 1, 2, 3, 4 };
        var mediaDirectory = Path.Combine(database.RootPath, "communication-gifs");
        Directory.CreateDirectory(mediaDirectory);
        File.WriteAllBytes(Path.Combine(mediaDirectory, key.ToString("N") + ".gif"), bytes);
        File.WriteAllBytes(Path.Combine(mediaDirectory, Guid.NewGuid().ToString("N") + ".gif"), [9]);
        await using (var db = database.CreateContext())
        {
            db.CommunicationMessages.Add(new CommunicationMessageModel
            {
                Title = "snapshot", Body = "text", CreatedByManagerName = "manager",
                CreatedAtUtc = DateTimeOffset.UtcNow, DeadlineAtUtc = DateTimeOffset.UtcNow.AddDays(1),
                GifAttachmentJson = vectors.RootElement.GetProperty("legacy").GetRawText(),
                PhotoAttachmentJson = vectors.RootElement.GetProperty("photo").GetRawText(),
                ContentBlocksJson = vectors.RootElement.GetProperty("blocks").GetRawText(),
            });
            await db.SaveChangesAsync();
        }

        var workspace = new SqliteDatabaseWorkspace(database.DatabasePath);
        var backup = await workspace.CreateManualCopyAsync(default);
        var bundle = Path.GetDirectoryName(backup.Path)!;
        var restoredDir = Path.Combine(database.RootPath, "restored", Guid.NewGuid().ToString("N"));
        var restoredDatabase = await workspace.MaterializeBackupAsync(backup.Path, restoredDir, default);

        Assert.True(File.Exists(restoredDatabase));
        Assert.Equal(bytes, File.ReadAllBytes(Path.Combine(restoredDir, "communication-gifs", key.ToString("N") + ".gif")));
        Assert.Single(Directory.EnumerateFiles(Path.Combine(bundle, "communication-gifs")));
        Assert.Equal(bytes, File.ReadAllBytes(Path.Combine(bundle, "communication-gifs", key.ToString("N") + ".gif")));
        Assert.Matches("^[a-f0-9]{64}$", JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle, "backup-manifest.json"))).RootElement.GetProperty("database").GetProperty("sha256").GetString());
    }

    [Fact]
    public async Task BackupFailsWhenReferencedMediaIsMissingAndPublishesNothing()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using (var db = database.CreateContext())
        {
            db.CommunicationMessages.Add(new CommunicationMessageModel
            {
                Title = "broken", Body = "text", CreatedByManagerName = "manager",
                CreatedAtUtc = DateTimeOffset.UtcNow, DeadlineAtUtc = DateTimeOffset.UtcNow.AddDays(1),
                GifAttachmentJson = JsonSerializer.Serialize(new { Key = Guid.NewGuid(), Size = 5 }),
            });
            await db.SaveChangesAsync();
        }
        var workspace = new SqliteDatabaseWorkspace(database.DatabasePath);
        await Assert.ThrowsAsync<InvalidDataException>(() => workspace.CreateAutomaticBackupAsync(default));
        Assert.Empty(Directory.EnumerateFileSystemEntries(workspace.AutomaticBackupDirectoryPath));
    }
}
