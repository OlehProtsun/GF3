using BusinessLogicLayer.Contracts.Communications;
using DataAccessLayer.Administration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace WebApi.Services;

public sealed class CommunicationGifStorage(ISqliteDatabaseWorkspace workspace)
{
    public const int MaxGifBytes = 3 * 1024 * 1024;
    public const int MaxBlockUploadBytes = 8 * 1024 * 1024;
    public const int MaxRequestBytes = MaxBlockUploadBytes + 64 * 1024;
    private const int MaxFrames = 120;
    private const long MaxCanvasPixelsAcrossFrames = 60_000_000;
    private static readonly byte[] LoopExtension = [0x21, 0xff, 11, .. "NETSCAPE2.0"u8, 3, 1, 0, 0, 0];

    private string DirectoryPath => Path.Combine(Path.GetDirectoryName(workspace.DatabasePath)!, "communication-gifs");

    public async Task<CommunicationGifAttachment> SaveAsync(IFormFile file, CancellationToken ct)
    {
        if (file.Length is <= 0 or > MaxGifBytes)
            throw new BadHttpRequestException("Choose a GIF no larger than 3 MB.");

        var input = new byte[(int)file.Length];
        await using (var stream = file.OpenReadStream())
        {
            await stream.ReadExactlyAsync(input, ct).ConfigureAwait(false);
        }

        return await StoreAsync(file.FileName, NormalizeGif(input), "image/gif", ct);
    }

    public async Task<CommunicationGifAttachment> SavePhotoAsync(IFormFile file, CancellationToken ct)
    {
        if (file.Length is <= 0 or > MaxGifBytes) throw new BadHttpRequestException("File too large");
        try
        {
            using var input = file.OpenReadStream();
            var format = Image.DetectFormat(input);
            if (format.DefaultMimeType is not ("image/jpeg" or "image/png" or "image/webp"))
                throw new BadHttpRequestException("Unsupported file format");
            input.Position = 0;
            var info = Image.Identify(input);
            if ((long)info.Width * info.Height > 16_000_000) throw new BadHttpRequestException("File too large");
            input.Position = 0;
            using var photo = Image.Load(new DecoderOptions { MaxFrames = 1 }, input);
            photo.Mutate(image => image.AutoOrient());
            using var output = new MemoryStream();
            await photo.SaveAsync(output, new WebpEncoder { SkipMetadata = true }, ct);
            if (output.Length > MaxGifBytes) throw new BadHttpRequestException("File too large");
            return await StoreAsync(file.FileName, output.ToArray(), "image/webp", ct);
        }
        catch (Exception error) when (error is UnknownImageFormatException or InvalidImageContentException)
        {
            throw new BadHttpRequestException("Unsupported file format");
        }
    }

    private async Task<CommunicationGifAttachment> StoreAsync(string originalName, byte[] bytes, string contentType, CancellationToken ct)
    {
        var key = Guid.NewGuid();
        Directory.CreateDirectory(DirectoryPath);
        var path = GetPath(key);
        var temporaryPath = path + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temporaryPath, bytes, ct).ConfigureAwait(false);
            File.Move(temporaryPath, path);
        }
        finally
        {
            File.Delete(temporaryPath);
        }

        var fileName = Path.GetFileName(originalName.Replace('\\', '/'));
        fileName = new string(fileName.Where(character => !char.IsControl(character)).Take(160).ToArray()).Trim();
        var fallbackName = contentType == "image/gif" ? "animation.gif" : "photo.webp";
        return new CommunicationGifAttachment(key, fileName.Length > 0 ? fileName : fallbackName, bytes.Length) { ContentType = contentType };
    }

    public Stream? Open(Guid key)
    {
        try
        {
            return new FileStream(GetPath(key), FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete,
                16 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
    }

    public void Remove(Guid key) => File.Delete(GetPath(key));

    private string GetPath(Guid key) => Path.Combine(DirectoryPath, key.ToString("N") + ".gif");

    // Read the complete GIF container before storage. Replace existing loop instructions
    // so every accepted animation repeats without a player or a client-side timer.
    private static byte[] NormalizeGif(byte[] gif)
    {
        if (gif.Length < 14 || !(gif.AsSpan(0, 6).SequenceEqual("GIF87a"u8) || gif.AsSpan(0, 6).SequenceEqual("GIF89a"u8)))
            throw InvalidGif();

        var width = ReadWord(gif, 6);
        var height = ReadWord(gif, 8);
        if (width is <= 0 or > 900 || height is <= 0 or > 1600)
            throw InvalidGif();

        var position = 13 + ColorTableSize(gif[10]);
        if (position >= gif.Length)
            throw InvalidGif();

        using var output = new MemoryStream(gif.Length + LoopExtension.Length);
        output.Write(gif.AsSpan(0, position));
        output.Write(LoopExtension);
        var frames = 0;
        while (position < gif.Length)
        {
            var start = position;
            var marker = gif[position++];
            if (marker == 0x3b)
            {
                if (position != gif.Length || frames < 2 || output.Length + 1 > MaxGifBytes)
                    throw InvalidGif();
                output.WriteByte(marker);
                var result = output.ToArray();
                "GIF89a"u8.CopyTo(result);
                return result;
            }

            if (marker == 0x2c)
            {
                if (position + 9 > gif.Length || ++frames > MaxFrames || (long)width * height * frames > MaxCanvasPixelsAcrossFrames)
                    throw InvalidGif();
                var frameWidth = ReadWord(gif, position + 4);
                var frameHeight = ReadWord(gif, position + 6);
                if (frameWidth == 0 || frameHeight == 0 || ReadWord(gif, position) + frameWidth > width ||
                    ReadWord(gif, position + 2) + frameHeight > height ||
                    ((gif[position + 8] | gif[10]) & 0x80) == 0)
                    throw InvalidGif();
                position += 9 + ColorTableSize(gif[position + 8]);
                if (position + 1 >= gif.Length || gif[position++] is < 2 or > 8 || gif[position] == 0)
                    throw InvalidGif();
                position = SkipBlocks(gif, position);
            }
            else if (marker == 0x21 && position < gif.Length)
            {
                var type = gif[position++];
                if (type == 0xf9 && (position + 5 >= gif.Length || gif[position] != 4 || gif[position + 5] != 0))
                    throw InvalidGif();
                if (type == 0xff && (position + 11 >= gif.Length || gif[position] != 11))
                    throw InvalidGif();
                // Copy only graphic control blocks. Application and comment metadata
                // are unnecessary for playback and can contain identifying data.
                position = SkipBlocks(gif, position);
                if (type != 0xf9)
                    continue;
            }
            else
            {
                throw InvalidGif();
            }

            output.Write(gif.AsSpan(start, position - start));
        }
        throw InvalidGif();
    }

    private static int ReadWord(byte[] gif, int position) => gif[position] | (gif[position + 1] << 8);

    private static int ColorTableSize(byte packed) => (packed & 0x80) == 0 ? 0 : 3 << ((packed & 7) + 1);

    private static int SkipBlocks(byte[] gif, int position)
    {
        while (position < gif.Length)
        {
            var length = gif[position++];
            if (length == 0)
                return position;
            position += length;
        }
        throw InvalidGif();
    }

    private static BadHttpRequestException InvalidGif()
        => new("Choose a valid animated GIF up to 900 × 1600 pixels and 3 MB.");
}
