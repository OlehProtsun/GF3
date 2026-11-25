using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace DataAccessLayer.Administration;

internal static class SqliteMediaBackup
{
    private const int MaxMediaBytes = 3 * 1024 * 1024;
    private const string DatabaseName = "SQLite.db";
    private const string MediaDirectoryName = "communication-gifs";
    private const string ManifestName = "backup-manifest.json";

    public static bool IsBundleDatabasePath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        return Path.GetFileName(fullPath).Equals(DatabaseName, StringComparison.OrdinalIgnoreCase) && directory is not null &&
            File.Exists(Path.Combine(directory, ManifestName)) && Directory.Exists(Path.Combine(directory, MediaDirectoryName));
    }

    public static async Task<bool> HasReferencedMediaAsync(string databasePath, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(ConnectionString(databasePath));
        await connection.OpenAsync(ct).ConfigureAwait(false);
        return (await ReadReferencedMediaKeysAsync(connection, ct).ConfigureAwait(false)).Count != 0;
    }

    public static async Task<string> CreateAsync(string sourceDbPath, string destinationDirectory, CancellationToken ct)
    {
        var destination = Path.GetFullPath(destinationDirectory);
        var parent = Path.GetDirectoryName(destination) ?? throw new InvalidOperationException("Backup destination must have a parent directory.");
        Directory.CreateDirectory(parent);
        if (Directory.Exists(destination) || File.Exists(destination)) throw new IOException("Backup destination already exists.");
        var staging = Path.Combine(parent, "." + Path.GetFileName(destination) + "." + Guid.NewGuid().ToString("N") + ".tmp");
        Directory.CreateDirectory(staging);
        try
        {
            var snapshotPath = Path.Combine(staging, DatabaseName);
            var media = new List<MediaEntry>();
            await using (var source = new SqliteConnection(ConnectionString(sourceDbPath)))
            await using (var snapshot = new SqliteConnection(ConnectionString(snapshotPath)))
            {
                await source.OpenAsync(ct).ConfigureAwait(false);
                await snapshot.OpenAsync(ct).ConfigureAwait(false);
                source.BackupDatabase(snapshot);
                await VerifyDatabaseAsync(snapshot, ct).ConfigureAwait(false);
                var references = await ReadReferencedMediaKeysAsync(snapshot, ct).ConfigureAwait(false);
                var sourceMedia = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(sourceDbPath))!, MediaDirectoryName);
                var targetMedia = Path.Combine(staging, MediaDirectoryName);
                if (Directory.Exists(sourceMedia)) EnsureDirectory(sourceMedia);
                Directory.CreateDirectory(targetMedia);
                foreach (var reference in references.OrderBy(item => item.Key, StringComparer.Ordinal))
                {
                    ct.ThrowIfCancellationRequested();
                    var sourcePath = Path.Combine(sourceMedia, reference.Key);
                    EnsureRegularFile(sourcePath);
                    var before = new FileInfo(sourcePath);
                    if (before.Length != reference.Size || before.Length > MaxMediaBytes) throw new InvalidDataException($"Referenced media '{reference.Key}' has an unexpected size.");
                    var sourceHashBefore = await HashFileAsync(sourcePath, ct).ConfigureAwait(false);
                    var targetPath = Path.Combine(targetMedia, reference.Key);
                    await using (var input = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan))
                    await using (var output = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan))
                    {
                        await input.CopyToAsync(output, ct).ConfigureAwait(false);
                        await output.FlushAsync(ct).ConfigureAwait(false);
                        output.Flush(true);
                    }
                    var after = new FileInfo(sourcePath);
                    var copied = new FileInfo(targetPath);
                    var hash = await HashFileAsync(targetPath, ct).ConfigureAwait(false);
                    var sourceHashAfter = await HashFileAsync(sourcePath, ct).ConfigureAwait(false);
                    if (after.Length != before.Length || copied.Length != reference.Size || sourceHashBefore != sourceHashAfter || sourceHashBefore != hash) throw new InvalidDataException($"Referenced media '{reference.Key}' changed during backup.");
                    media.Add(new MediaEntry(reference.Key, copied.Length, hash));
                }
            }

            var dbInfo = new FileInfo(snapshotPath);
            var dbHash = await HashFileAsync(snapshotPath, ct).ConfigureAwait(false);
            var manifest = new BackupManifest(1, new MediaEntry(DatabaseName, dbInfo.Length, dbHash), media, "referenced", DateTime.UtcNow);
            var manifestPath = Path.Combine(staging, ManifestName);
            await using (var stream = new FileStream(manifestPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 16 * 1024, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, manifest, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }, ct).ConfigureAwait(false);
                await stream.FlushAsync(ct).ConfigureAwait(false);
                stream.Flush(true);
            }
            await ValidateAsync(staging, ct).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            Directory.Move(staging, destination);
            return destination;
        }
        catch
        {
            if (Directory.Exists(staging)) Directory.Delete(staging, recursive: true);
            throw;
        }
    }

    public static async Task ValidateAsync(string bundle, CancellationToken ct)
    {
        var root = Path.GetFullPath(bundle);
        EnsureDirectory(root);
        var manifestPath = Path.Combine(root, ManifestName);
        EnsureRegularFile(manifestPath);
        var manifestBytes = await File.ReadAllBytesAsync(manifestPath, ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(manifestBytes);
        RejectDuplicateProperties(doc.RootElement);
        var manifest = JsonSerializer.Deserialize<BackupManifest>(manifestBytes, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidDataException("Backup manifest is empty.");
        if (manifest.Version != 1 || manifest.Mode is not ("referenced" or "fenced-full") || manifest.Database.Name != DatabaseName ||
            manifest.Database.Size < 0 || !IsHash(manifest.Database.Sha256) || manifest.CreatedAtUtc.Kind != DateTimeKind.Utc)
            throw new InvalidDataException($"Backup manifest is invalid (version {manifest.Version}, mode '{manifest.Mode}', database '{manifest.Database?.Name}', size {manifest.Database?.Size}, timestamp kind {manifest.CreatedAtUtc.Kind}).");
        EnsureExactFiles(root, [DatabaseName, ManifestName, MediaDirectoryName]);
        var mediaRoot = Path.Combine(root, MediaDirectoryName);
        EnsureDirectory(mediaRoot);
        if (!manifest.Media.Select(item => item.Name).SequenceEqual(manifest.Media.Select(item => item.Name).OrderBy(name => name, StringComparer.Ordinal), StringComparer.Ordinal))
            throw new InvalidDataException("Backup media manifest is not sorted by filename.");
        EnsureExactFiles(mediaRoot, manifest.Media.Select(item => item.Name));
        var dbPath = Path.Combine(root, DatabaseName);
        await VerifyFileAsync(dbPath, manifest.Database, ct).ConfigureAwait(false);
        foreach (var item in manifest.Media)
        {
            if (!IsSafeMediaName(item.Name) || item.Size < 0 || !IsHash(item.Sha256)) throw new InvalidDataException("Backup media manifest entry is invalid.");
            EnsureRegularFile(Path.Combine(mediaRoot, item.Name));
            await VerifyFileAsync(Path.Combine(mediaRoot, item.Name), item, ct).ConfigureAwait(false);
        }
        await using var connection = new SqliteConnection(ConnectionString(dbPath));
        await connection.OpenAsync(ct).ConfigureAwait(false);
        await VerifyDatabaseAsync(connection, ct).ConfigureAwait(false);
        var references = await ReadReferencedMediaKeysAsync(connection, ct).ConfigureAwait(false);
        var names = manifest.Media.Select(item => item.Name).ToHashSet(StringComparer.Ordinal);
        if (!references.Select(item => item.Key).ToHashSet(StringComparer.Ordinal).SetEquals(names)) throw new InvalidDataException("Backup media does not match the database references.");
        foreach (var reference in references)
        {
            var entry = manifest.Media.Single(item => item.Name == reference.Key);
            if (entry.Size != reference.Size) throw new InvalidDataException("Backup media size does not match the database reference.");
        }
    }

    public static async Task<string> MaterializeAsync(string bundle, string workingDirectory, CancellationToken ct)
    {
        await ValidateAsync(bundle, ct).ConfigureAwait(false);
        var manifest = JsonSerializer.Deserialize<BackupManifest>(await File.ReadAllBytesAsync(Path.Combine(bundle, ManifestName), ct).ConfigureAwait(false), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        var destination = Path.GetFullPath(workingDirectory);
        var parent = Path.GetDirectoryName(destination) ?? throw new InvalidOperationException("Working directory must have a parent.");
        Directory.CreateDirectory(parent);
        if (Directory.Exists(destination) || File.Exists(destination)) throw new IOException("Working directory already exists.");
        var staging = Path.Combine(parent, "." + Path.GetFileName(destination) + "." + Guid.NewGuid().ToString("N") + ".tmp");
        Directory.CreateDirectory(staging);
        try
        {
            File.Copy(Path.Combine(bundle, DatabaseName), Path.Combine(staging, DatabaseName));
            await VerifyFileAsync(Path.Combine(staging, DatabaseName), manifest.Database, ct).ConfigureAwait(false);
            var mediaDestination = Path.Combine(staging, MediaDirectoryName);
            Directory.CreateDirectory(mediaDestination);
            foreach (var item in manifest.Media)
            {
                ct.ThrowIfCancellationRequested();
                var path = Path.Combine(mediaDestination, item.Name);
                File.Copy(Path.Combine(bundle, MediaDirectoryName, item.Name), path);
                await VerifyFileAsync(path, item, ct).ConfigureAwait(false);
            }
            await using (var connection = new SqliteConnection(ConnectionString(Path.Combine(staging, DatabaseName))))
            {
                await connection.OpenAsync(ct).ConfigureAwait(false);
                await VerifyDatabaseAsync(connection, ct).ConfigureAwait(false);
                var refs = await ReadReferencedMediaKeysAsync(connection, ct).ConfigureAwait(false);
                if (!refs.OrderBy(item => item.Key, StringComparer.Ordinal).SequenceEqual(manifest.Media.Select(item => new MediaReference(item.Name, item.Size)).OrderBy(item => item.Key, StringComparer.Ordinal)))
                    throw new InvalidDataException("Materialized media does not match the database references.");
            }
            Directory.Move(staging, destination);
            return Path.Combine(destination, DatabaseName);
        }
        catch
        {
            if (Directory.Exists(staging)) Directory.Delete(staging, recursive: true);
            throw;
        }
    }

    private static async Task<List<MediaReference>> ReadReferencedMediaKeysAsync(SqliteConnection connection, CancellationToken ct)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA table_info(communication_message);";
            await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false)) columns.Add(reader.GetString(1));
        }
        var selected = new[] { "gif_attachment_json", "photo_attachment_json", "content_blocks_json" }.Where(columns.Contains).ToArray();
        if (selected.Length == 0) return [];
        var output = new Dictionary<string, long>(StringComparer.Ordinal);
        await using var rows = connection.CreateCommand();
        rows.CommandText = $"SELECT {string.Join(",", selected.Select(name => "\"" + name + "\""))} FROM communication_message;";
        await using var data = await rows.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await data.ReadAsync(ct).ConfigureAwait(false))
        {
            for (var i = 0; i < selected.Length; i++)
            {
                if (data.IsDBNull(i)) continue;
                using var json = JsonDocument.Parse(data.GetString(i));
                RejectDuplicateProperties(json.RootElement);
                if (selected[i] == "content_blocks_json")
                {
                    if (json.RootElement.ValueKind != JsonValueKind.Array) throw new InvalidDataException("Communication content blocks are malformed.");
                    foreach (var block in json.RootElement.EnumerateArray())
                    {
                        if (GetString(block, "Type") is "photo" or "gif") AddAttachment(GetProperty(block, "Media"), output);
                    }
                }
                else AddAttachment(json.RootElement, output);
            }
        }
        return output.Select(pair => new MediaReference(pair.Key, pair.Value)).ToList();
    }

    private static void AddAttachment(JsonElement element, IDictionary<string, long> output)
    {
        if (element.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Communication attachment is malformed.");
        var keyText = GetString(element, "Key");
        var sizeElement = GetProperty(element, "Size");
        if (!Guid.TryParse(keyText, out var key) || key == Guid.Empty || sizeElement.ValueKind != JsonValueKind.Number || !sizeElement.TryGetInt64(out var size) || size <= 0 || size > MaxMediaBytes)
            throw new InvalidDataException("Communication attachment key or size is invalid.");
        var name = key.ToString("N").ToLowerInvariant() + ".gif";
        if (output.TryGetValue(name, out var oldSize) && oldSize != size) throw new InvalidDataException("Duplicate communication media has contradictory sizes.");
        output[name] = size;
    }

    private static JsonElement GetProperty(JsonElement item, string name)
    {
        if (item.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Communication JSON is malformed.");
        JsonElement found = default;
        foreach (var property in item.EnumerateObject())
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)) found = property.Value;
        return found;
    }

    private static string? GetString(JsonElement item, string name)
    {
        var value = GetProperty(item, name);
        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static void RejectDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new InvalidDataException("Backup JSON contains duplicate keys.");
                RejectDuplicateProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) RejectDuplicateProperties(item);
    }

    private static async Task VerifyDatabaseAsync(SqliteConnection connection, CancellationToken ct)
    {
        await using var integrity = connection.CreateCommand();
        integrity.CommandText = "PRAGMA integrity_check;";
        if (!string.Equals(Convert.ToString(await integrity.ExecuteScalarAsync(ct).ConfigureAwait(false)), "ok", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("SQLite integrity check failed.");
        await using var foreignKeys = connection.CreateCommand();
        foreignKeys.CommandText = "PRAGMA foreign_key_check;";
        await using var reader = await foreignKeys.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (await reader.ReadAsync(ct).ConfigureAwait(false)) throw new InvalidDataException("SQLite foreign key check failed.");
    }

    private static async Task VerifyFileAsync(string path, MediaEntry expected, CancellationToken ct)
    {
        var info = new FileInfo(path);
        if (info.Length != expected.Size || !string.Equals(await HashFileAsync(path, ct).ConfigureAwait(false), expected.Sha256, StringComparison.Ordinal)) throw new InvalidDataException($"Backup file '{expected.Name}' failed verification.");
    }

    private static async Task<string> HashFileAsync(string path, CancellationToken ct)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Convert.ToHexString(await SHA256.HashDataAsync(stream, ct).ConfigureAwait(false)).ToLowerInvariant();
    }

    private static void EnsureExactFiles(string directory, IEnumerable<string> expected)
    {
        var set = expected.ToHashSet(StringComparer.Ordinal);
        if (set.Count != expected.Count()) throw new InvalidDataException("Backup manifest contains duplicate filenames.");
        foreach (var path in Directory.EnumerateFileSystemEntries(directory))
        {
            if (!set.Remove(Path.GetFileName(path))) throw new InvalidDataException("Backup contains an unlisted or unexpected entry.");
            if (Directory.Exists(path)) EnsureDirectory(path);
            else EnsureRegularFile(path);
        }
        if (set.Count != 0) throw new InvalidDataException("Backup is missing a declared file.");
    }

    private static void EnsureDirectory(string path)
    {
        var info = new DirectoryInfo(path);
        if (!info.Exists || (info.Attributes & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Backup directory is missing or unsafe.");
    }

    private static void EnsureRegularFile(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists || (info.Attributes & FileAttributes.ReparsePoint) != 0 || (info.Attributes & FileAttributes.Directory) != 0) throw new InvalidDataException("Backup file is missing or unsafe.");
    }

    private static bool IsSafeMediaName(string name) => name.Length == 36 && name.EndsWith(".gif", StringComparison.Ordinal) && Guid.TryParseExact(name[..32], "N", out _);
    private static bool IsHash(string hash) => hash.Length == 64 && hash.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');
    private static string ConnectionString(string path) => new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString();

    private sealed record BackupManifest(int Version, MediaEntry Database, List<MediaEntry> Media, string Mode, DateTime CreatedAtUtc);
    private sealed record MediaEntry(string Name, long Size, string Sha256);
    private sealed record MediaReference(string Key, long Size);
}
