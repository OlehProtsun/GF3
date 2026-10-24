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

            return ReadString(column.ElementAtOrDefault(1));
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

            return ReadString(column["label"]);
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

            var value = ReadString(cell.Value)?.Trim();
            if (!string.IsNullOrWhiteSpace(value))
            {
                result[day] = value;
            }
        }

        return result;
    }

    private static JsonObject? ParseGraphNoteMeta(string rawMeta)
        => ShiftSwapRules.ParseGraphNoteMeta(rawMeta);

    private static string? ReadString(JsonNode? node)
        => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

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
