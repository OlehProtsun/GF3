using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace BusinessLogicLayer.Services.Export;

internal static class GraphNoteSqlExportBuilder
{
    private const string PortableGraphNoteMetaPrefix = "[[GF3_GRAPH_META:";
    private const string PortableGraphNoteMetaSuffix = "]]";

    private static readonly Regex MetadataRegex = new(
        @"(?:\r?\n\r?\n)?(?:<!--GF3_GRAPH_META:(?<legacyMeta>[\s\S]*?)-->|\[\[GF3_GRAPH_META:(?<portableMeta>[\s\S]*?)\]\])$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string? BuildPortableNote(string? note, IReadOnlyList<GraphNoteExportTextCell> textCells)
    {
        if (!TryExtractMetadata(note, out var visibleNote, out var rawMetadata, out var hasLegacyMetadata))
        {
            if (textCells.Count == 0)
                return note;

            return ComposeNote(visibleNote, CreateMetadataWithTextCells(textCells));
        }

        if (textCells.Count == 0)
            return hasLegacyMetadata ? ComposePortableRawMetadata(visibleNote, rawMetadata) : note;

        var metadata = TryParseMetadataObject(rawMetadata);
        if (metadata is null)
            return hasLegacyMetadata ? ComposePortableRawMetadata(visibleNote, rawMetadata) : note;

        metadata["t"] = BuildCompactTextCells(textCells);
        metadata.Remove("textCells");

        return ComposeNote(visibleNote, metadata);
    }

    private static JsonObject CreateMetadataWithTextCells(IReadOnlyList<GraphNoteExportTextCell> textCells)
    {
        var metadata = new JsonObject
        {
            ["t"] = BuildCompactTextCells(textCells),
        };

        return metadata;
    }

    private static JsonArray BuildCompactTextCells(IReadOnlyList<GraphNoteExportTextCell> textCells)
    {
        var compactTextCells = new JsonArray();

        foreach (var textCell in textCells)
        {
            compactTextCells.Add(new JsonArray(textCell.EmployeeId, textCell.DayOfMonth, textCell.Value));
        }

        return compactTextCells;
    }

    private static bool TryExtractMetadata(
        string? note,
        out string visibleNote,
        out string rawMetadata,
        out bool hasLegacyMetadata)
    {
        visibleNote = note?.TrimEnd() ?? string.Empty;
        rawMetadata = string.Empty;
        hasLegacyMetadata = false;

        if (string.IsNullOrWhiteSpace(note))
            return false;

        var match = MetadataRegex.Match(note);
        if (!match.Success)
            return false;

        visibleNote = note[..match.Index].TrimEnd();
        hasLegacyMetadata = match.Groups["legacyMeta"].Success;
        rawMetadata = hasLegacyMetadata
            ? match.Groups["legacyMeta"].Value
            : match.Groups["portableMeta"].Value;
        return true;
    }

    private static JsonObject? TryParseMetadataObject(string rawMetadata)
    {
        var payload = rawMetadata.Trim();
        if (string.IsNullOrWhiteSpace(payload))
            return new JsonObject();

        if (payload.StartsWith("b64:", StringComparison.OrdinalIgnoreCase))
        {
            var decodedPayload = DecodeBase64Url(payload[4..]);
            return decodedPayload is null ? null : TryParseJsonObject(decodedPayload);
        }

        var parsedPayload = TryParseJsonObject(payload);
        if (parsedPayload is not null)
            return parsedPayload;

        try
        {
            return TryParseJsonObject(Uri.UnescapeDataString(payload));
        }
        catch
        {
            return null;
        }
    }

    private static JsonObject? TryParseJsonObject(string payload)
    {
        try
        {
            return JsonNode.Parse(payload) as JsonObject;
        }
        catch
        {
            return null;
        }
    }

    private static string? DecodeBase64Url(string payload)
    {
        try
        {
            var normalized = payload
                .Replace('-', '+')
                .Replace('_', '/')
                .PadRight(((payload.Length + 3) / 4) * 4, '=');

            return Encoding.UTF8.GetString(Convert.FromBase64String(normalized));
        }
        catch
        {
            return null;
        }
    }

    private static string ComposeNote(string visibleNote, JsonObject metadata)
    {
        if (metadata.Count == 0)
            return visibleNote;

        var encodedMetadata = EncodeMetadata(metadata);
        var metadataBlock = $"{PortableGraphNoteMetaPrefix}{encodedMetadata}{PortableGraphNoteMetaSuffix}";
        return string.IsNullOrWhiteSpace(visibleNote) ? metadataBlock : $"{visibleNote}\n\n{metadataBlock}";
    }

    private static string ComposePortableRawMetadata(string visibleNote, string rawMetadata)
    {
        var metadataBlock = $"{PortableGraphNoteMetaPrefix}{rawMetadata}{PortableGraphNoteMetaSuffix}";
        return string.IsNullOrWhiteSpace(visibleNote) ? metadataBlock : $"{visibleNote}\n\n{metadataBlock}";
    }

    private static string EncodeMetadata(JsonObject metadata)
    {
        var payload = JsonSerializer.Serialize(metadata);
        var bytes = Encoding.UTF8.GetBytes(payload);
        return "b64:" + Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
