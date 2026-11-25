using PdfSharp.Fonts;

namespace WebApi.Services;

public sealed class SchedulePdfFontResolver : IFontResolver
{
    private const string FaceName = "GF3ScheduleRegular";
    private static readonly object Sync = new();
    private static SchedulePdfFontResolver? _configured;
    private readonly byte[] _font;

    private SchedulePdfFontResolver(string? configuredPath)
    {
        var path = !string.IsNullOrWhiteSpace(configuredPath)
            ? configuredPath
            : Path.Combine(AppContext.BaseDirectory, "Resources", "Fonts", "Carlito-Regular.ttf");
        if (!File.Exists(path))
            throw new InvalidOperationException("Schedule PDF font is missing. Install Carlito or configure PdfExport:FontFilePath.");
        _font = File.ReadAllBytes(path);
        if (_font.Length < 4 || !((_font[0] == 0 && _font[1] == 1 && _font[2] == 0 && _font[3] == 0)
            || System.Text.Encoding.ASCII.GetString(_font, 0, 4) == "OTTO"))
            throw new InvalidOperationException("Schedule PDF font must be a valid TrueType/OpenType font.");
    }

    public static void ConfigureOnce(string? fontFilePath = null)
    {
        lock (Sync)
        {
            if (_configured is not null)
            {
                if (!ReferenceEquals(GlobalFontSettings.FontResolver, _configured))
                    throw new InvalidOperationException("Schedule PDF font resolver was replaced.");
                return;
            }
            if (GlobalFontSettings.FontResolver is not null)
                throw new InvalidOperationException("A different PDFsharp font resolver is already configured.");
            var resolver = new SchedulePdfFontResolver(fontFilePath);
            GlobalFontSettings.FontResolver = resolver;
            _configured = resolver;
        }
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
        => familyName.Equals("GF3Schedule", StringComparison.OrdinalIgnoreCase)
            ? new FontResolverInfo(FaceName, isBold, isItalic) : null;

    public byte[] GetFont(string faceName)
        => faceName == FaceName ? _font : throw new InvalidOperationException("Unknown schedule PDF font face.");
}
