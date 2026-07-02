using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace WebApi.ShiftSwaps;

internal static class GraphManualColumnLabelResolver
{
    private static readonly Regex GraphNoteMetaRegex = new(
        @"(?:\r?\n\r?\n)?(?:<!--GF3_GRAPH_META:([\s\S]*?)-->|\[\[GF3_GRAPH_META:([\s\S]*?)\]\])$",
        RegexOptions.Compiled);

    public static string Resolve(string? rawNote, int? manualColumnId)
    {
        if (manualColumnId is not > 0)
        {
            return "Custom column";
        }

        var fallback = $"Custom {manualColumnId.Value}";
        if (string.IsNullOrWhiteSpace(rawNote))
        {
            return fallback;
        }

        var match = GraphNoteMetaRegex.Match(rawNote);
        if (!match.Success)
        {
            return fallback;
        }

        var rawMeta = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[1].Value;
        var meta = ParseGraphNoteMeta(rawMeta);
        if (meta is null)
        {
            return fallback;
        }

        var label =
            ResolveCompactLabel(meta["m"] as JsonArray, manualColumnId.Value) ??
            ResolveLegacyLabel(meta["manualColumns"] as JsonArray, manualColumnId.Value);

        return string.IsNullOrWhiteSpace(label) ? fallback : label.Trim();
    }

    public static Dictionary<int, string> ResolveCells(string? rawNote, int? manualColumnId)
    {
        if (manualColumnId is not > 0 || string.IsNullOrWhiteSpace(rawNote))
        {
            return [];
        }

        var match = GraphNoteMetaRegex.Match(rawNote);
        if (!match.Success)
        {
            return [];
        }

        var rawMeta = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[1].Value;
        var meta = ParseGraphNoteMeta(rawMeta);
        if (meta is null)
        {
            return [];
        }

        return ResolveCompactCells(meta["m"] as JsonArray, manualColumnId.Value)
            ?? ResolveLegacyCells(meta["manualColumns"] as JsonArray, manualColumnId.Value)
            ?? [];
    }

    private static string? ResolveCompactLabel(JsonArray? manualColumns, int manualColumnId)
    {
        if (manualColumns is null)
        {
            return null;
        }

        foreach (var rawColumn in manualColumns)
        {
            if (rawColumn is not JsonArray column || GetJsonInt(column.ElementAtOrDefault(0)) != manualColumnId)
            {
                continue;
            }

            return column.ElementAtOrDefault(1)?.GetValue<string>();
        }

        return null;
    }

    private static string? ResolveLegacyLabel(JsonArray? manualColumns, int manualColumnId)
    {
        if (manualColumns is null)
        {
            return null;
        }

        foreach (var rawColumn in manualColumns)
        {
            if (rawColumn is not JsonObject column || GetJsonInt(column["id"]) != manualColumnId)
            {
                continue;
            }

            return column["label"]?.GetValue<string>();
        }

        return null;
    }

    private static Dictionary<int, string>? ResolveCompactCells(JsonArray? manualColumns, int manualColumnId)
    {
        if (manualColumns is null)
        {
            return null;
        }

        foreach (var rawColumn in manualColumns)
        {
            if (rawColumn is not JsonArray column || GetJsonInt(column.ElementAtOrDefault(0)) != manualColumnId)
            {
                continue;
            }

            return ReadCells(column.ElementAtOrDefault(2));
        }

        return null;
    }

    private static Dictionary<int, string>? ResolveLegacyCells(JsonArray? manualColumns, int manualColumnId)
    {
        if (manualColumns is null)
        {
            return null;
        }

        foreach (var rawColumn in manualColumns)
        {
            if (rawColumn is not JsonObject column || GetJsonInt(column["id"]) != manualColumnId)
            {
                continue;
            }

            return ReadCells(column["cells"]);
        }

        return null;
    }

    private static Dictionary<int, string> ReadCells(JsonNode? cellsNode)
    {
        if (cellsNode is not JsonObject cells)
        {
            return [];
        }

        var result = new Dictionary<int, string>();
        foreach (var cell in cells)
        {
            if (!int.TryParse(cell.Key, out var day) || day is < 1 or > 31)
            {
                continue;
            }

            var value = cell.Value?.GetValue<string>()?.Trim();
            if (!string.IsNullOrWhiteSpace(value))
            {
                result[day] = value;
            }
        }

        return result;
    }

    private static JsonObject? ParseGraphNoteMeta(string rawMeta)
    {
        var payload = rawMeta.Trim();
        if (payload.Length == 0)
        {
            return null;
        }

        if (payload.StartsWith("b64:", StringComparison.Ordinal))
        {
            return TryParseJsonObject(DecodeGraphNoteMetaValue(payload[4..]));
        }

        return TryParseJsonObject(payload) ?? TryParseJsonObject(Uri.UnescapeDataString(payload));
    }

    private static JsonObject? TryParseJsonObject(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(value) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string DecodeGraphNoteMetaValue(string value)
    {
        var normalized = value
            .Replace('-', '+')
            .Replace('_', '/')
            .PadRight((int)Math.Ceiling(value.Length / 4d) * 4, '=');

        return Encoding.UTF8.GetString(Convert.FromBase64String(normalized));
    }

    private static int? GetJsonInt(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        try
        {
            return node.GetValue<int>();
        }
        catch (FormatException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
