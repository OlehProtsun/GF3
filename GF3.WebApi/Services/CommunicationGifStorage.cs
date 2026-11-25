using BusinessLogicLayer.Contracts.Communications;
using DataAccessLayer.Administration;
using SkiaSharp;

namespace WebApi.Services;

public sealed class CommunicationGifStorage(ISqliteDatabaseWorkspace workspace)
{
    public const int MaxGifBytes = 3 * 1024 * 1024;
    public const int MaxBlockUploadBytes = 8 * 1024 * 1024;
    public const int MaxRequestBytes = MaxBlockUploadBytes + 64 * 1024;
    private const int MaxFrames = 120;
    private const long MaxCanvasPixelsAcrossFrames = 60_000_000;
    private const int MaxPhotoPixels = 16_000_000;
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
        var bytes = new byte[(int)file.Length];
        await using (var input = file.OpenReadStream())
            await input.ReadExactlyAsync(bytes, ct);

        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data);
        if (codec is null || codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp))
            throw new BadHttpRequestException("Unsupported file format");

        var (width, height) = (codec.Info.Width, codec.Info.Height);
        if (width <= 0 || height <= 0 || (long)width * height > MaxPhotoPixels)
            throw new BadHttpRequestException("File too large");

        ct.ThrowIfCancellationRequested();
        using var colorSpace = SKColorSpace.CreateSrgb();
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul, colorSpace);
        using var bitmap = new SKBitmap(info);
        if (codec.GetPixels(info, bitmap.GetPixels()) != SKCodecResult.Success)
            throw new BadHttpRequestException("Unsupported file format");

        using var oriented = OrientPhoto(bitmap, codec.EncodedOrigin);
        // Encode only the decoded pixels. The input metadata never reaches the encoder.
        using var output = (oriented ?? bitmap).Encode(SKEncodedImageFormat.Webp, 75)
            ?? throw new BadHttpRequestException("Unsupported file format");
        if (output.Size > MaxGifBytes) throw new BadHttpRequestException("File too large");
        return await StoreAsync(file.FileName, output.ToArray(), "image/webp", ct);
    }

    private static SKBitmap? OrientPhoto(SKBitmap bitmap, SKEncodedOrigin origin)
    {
        if (origin == SKEncodedOrigin.TopLeft) return null;
        var (width, height) = (bitmap.Width, bitmap.Height);
        var transform = origin switch
        {
            SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, width, 0, 1, 0, 0, 0, 1),
            SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, width, 0, -1, height, 0, 0, 1),
            SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, height, 0, 0, 1),
            SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightTop => new SKMatrix(0, -1, height, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, height, -1, 0, width, 0, 0, 1),
            SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, width, 0, 0, 1),
            _ => SKMatrix.Identity,
        };
        var rotated = origin is >= SKEncodedOrigin.LeftTop and <= SKEncodedOrigin.LeftBottom;
        var oriented = new SKBitmap(rotated ? bitmap.Info.WithSize(height, width) : bitmap.Info);
        using var canvas = new SKCanvas(oriented);
        canvas.Clear(SKColors.Transparent);
        canvas.SetMatrix(transform);
        canvas.DrawBitmap(bitmap, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest));
        return oriented;
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
