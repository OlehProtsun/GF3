using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BusinessLogicLayer.Services.Export;

/// <summary>
/// Describes one manually injected export column stored inside graph note metadata.
/// </summary>
internal sealed record GraphNoteExportManualColumn(
    int Id,
    string Label,
    IReadOnlyDictionary<int, string> Cells);

/// <summary>
/// Describes one explicit text override for an employee/day cell in the export matrix.
/// </summary>
internal sealed record GraphNoteExportTextCell(
    int EmployeeId,
    int DayOfMonth,
    string Value);

/// <summary>
/// Parsed export metadata extracted from the graph note payload.
/// This structure keeps manual columns, custom ordering, and explicit text cells separate so
/// downstream export builders can apply each concern in a predictable order.
/// </summary>
internal sealed record GraphNoteExportMetadata(
    IReadOnlyList<GraphNoteExportManualColumn> ManualColumns,
    IReadOnlyList<int> ColumnOrder,
    IReadOnlyList<GraphNoteExportTextCell> TextCells)
{
    public static GraphNoteExportMetadata Empty { get; } = new([], [], []);
}

/// <summary>
/// Parses optional export metadata embedded into the human-readable graph note.
/// The parser supports both legacy and compact payload shapes so previously saved graphs remain
/// exportable after schema evolution.
/// </summary>
internal static class GraphNoteExportMetadataParser
{
    private static readonly Regex MetadataRegex = new(
        @"(?:\r?\n\r?\n)?(?:<!--GF3_GRAPH_META:(?<legacyMeta>[\s\S]*?)-->|\[\[GF3_GRAPH_META:(?<portableMeta>[\s\S]*?)\]\])$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private const string EmptyMark = "-";

    /// <summary>
    /// Extracts structured export metadata from the supplied note text.
    /// If the note contains no recognized payload or the payload is invalid, the parser returns
    /// <see cref="GraphNoteExportMetadata.Empty"/> instead of failing the export flow.
    /// </summary>
    public static GraphNoteExportMetadata Parse(string? rawNote)
    {
        if (string.IsNullOrWhiteSpace(rawNote))
            return GraphNoteExportMetadata.Empty;

        var match = MetadataRegex.Match(rawNote);
        if (!match.Success)
            return GraphNoteExportMetadata.Empty;

        var metadataPayload = match.Groups["portableMeta"].Success
            ? match.Groups["portableMeta"].Value
            : match.Groups["legacyMeta"].Value;

        using var document = TryParseMetadataDocument(metadataPayload);
        if (document is null)
            return GraphNoteExportMetadata.Empty;

        var root = document.RootElement;
        var manualColumns = ParseLegacyManualColumns(root);
        if (manualColumns.Count == 0)
            manualColumns = ParseCompactManualColumns(root);

        var columnOrder = ParseColumnOrder(root, "columnOrder");
        if (columnOrder.Count == 0)
            columnOrder = ParseColumnOrder(root, "o");

        var textCells = ParseLegacyTextCells(root);
        if (textCells.Count == 0)
            textCells = ParseCompactTextCells(root);

        if (manualColumns.Count == 0 && columnOrder.Count == 0 && textCells.Count == 0)
            return GraphNoteExportMetadata.Empty;

        return new GraphNoteExportMetadata(manualColumns, columnOrder, textCells);
    }

    private static JsonDocument? TryParseMetadataDocument(string rawMetadata)
    {
        var payload = rawMetadata.Trim();
        if (string.IsNullOrWhiteSpace(payload))
            return null;

        if (payload.StartsWith("b64:", StringComparison.OrdinalIgnoreCase))
        {
            var decoded = DecodeBase64Url(payload[4..]);
            return decoded is null ? null : TryParseJson(decoded);
        }

        var document = TryParseJson(payload);
        if (document is not null)
            return document;

        try
        {
            var decoded = Uri.UnescapeDataString(payload);
            return TryParseJson(decoded);
        }
        catch
        {
            return null;
        }
    }

    private static JsonDocument? TryParseJson(string payload)
    {
        try
        {
            return JsonDocument.Parse(payload);
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

            var bytes = Convert.FromBase64String(normalized);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return null;
        }
    }

    private static List<GraphNoteExportManualColumn> ParseLegacyManualColumns(JsonElement root)
    {
        if (!root.TryGetProperty("manualColumns", out var manualColumnsElement) || manualColumnsElement.ValueKind != JsonValueKind.Array)
            return [];

        var seenIds = new HashSet<int>();
        var result = new List<GraphNoteExportManualColumn>();

        foreach (var columnElement in manualColumnsElement.EnumerateArray())
        {
            if (!TryGetInt(columnElement, "id", out var id) || id <= 0 || !seenIds.Add(id))
                continue;

            var label = TryGetString(columnElement, "label") ?? string.Empty;
            result.Add(new GraphNoteExportManualColumn(id, label, ParseManualColumnCells(GetOptionalProperty(columnElement, "cells"))));
        }

        return result;
    }

    private static List<GraphNoteExportManualColumn> ParseCompactManualColumns(JsonElement root)
    {
        if (!root.TryGetProperty("m", out var manualColumnsElement) || manualColumnsElement.ValueKind != JsonValueKind.Array)
            return [];

        var seenIds = new HashSet<int>();
        var result = new List<GraphNoteExportManualColumn>();

        foreach (var columnElement in manualColumnsElement.EnumerateArray())
        {
            if (columnElement.ValueKind != JsonValueKind.Array)
                continue;

            var values = columnElement.EnumerateArray().ToList();
            if (values.Count == 0)
                continue;

            var id = GetInt(values[0]);
            if (id <= 0 || !seenIds.Add(id))
                continue;

            var label = values.Count > 1 && values[1].ValueKind == JsonValueKind.String
                ? values[1].GetString() ?? string.Empty
                : string.Empty;
            var cells = values.Count > 2 ? ParseManualColumnCells(values[2]) : new Dictionary<int, string>();

            result.Add(new GraphNoteExportManualColumn(id, label, cells));
        }

        return result;
    }

    private static Dictionary<int, string> ParseManualColumnCells(JsonElement? cellsElement)
    {
        if (cellsElement is not { ValueKind: JsonValueKind.Object })
            return [];

        var result = new Dictionary<int, string>();

        foreach (var property in cellsElement.Value.EnumerateObject())
        {
            if (!int.TryParse(property.Name, out var dayOfMonth) || dayOfMonth < 1 || dayOfMonth > 31)
                continue;

            if (property.Value.ValueKind != JsonValueKind.String)
                continue;

            var value = property.Value.GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(value))
                continue;

            result[dayOfMonth] = value;
        }

        return result;
    }

    private static List<int> ParseColumnOrder(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var columnOrderElement) || columnOrderElement.ValueKind != JsonValueKind.Array)
            return [];

        var seenEntries = new HashSet<int>();
        var result = new List<int>();

        foreach (var entryElement in columnOrderElement.EnumerateArray())
        {
            var entry = GetInt(entryElement);
            if (entry == 0 || !seenEntries.Add(entry))
                continue;

            result.Add(entry);
        }

        return result;
    }

    private static List<GraphNoteExportTextCell> ParseLegacyTextCells(JsonElement root)
    {
        if (!root.TryGetProperty("textCells", out var textCellsElement) || textCellsElement.ValueKind != JsonValueKind.Array)
            return [];

        var seenEntries = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<GraphNoteExportTextCell>();

        foreach (var textCellElement in textCellsElement.EnumerateArray())
        {
            if (!TryGetInt(textCellElement, "employeeId", out var employeeId) || employeeId <= 0)
                continue;
            if (!TryGetInt(textCellElement, "dayOfMonth", out var dayOfMonth) || dayOfMonth < 1 || dayOfMonth > 31)
                continue;

            var value = TryGetString(textCellElement, "value")?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, EmptyMark, StringComparison.Ordinal))
                continue;

            var key = $"{employeeId}:{dayOfMonth}";
            if (!seenEntries.Add(key))
                continue;

            result.Add(new GraphNoteExportTextCell(employeeId, dayOfMonth, value));
        }

        return result;
    }

    private static List<GraphNoteExportTextCell> ParseCompactTextCells(JsonElement root)
    {
        if (!root.TryGetProperty("t", out var textCellsElement) || textCellsElement.ValueKind != JsonValueKind.Array)
            return [];

        var seenEntries = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<GraphNoteExportTextCell>();

        foreach (var textCellElement in textCellsElement.EnumerateArray())
        {
            if (textCellElement.ValueKind != JsonValueKind.Array)
                continue;

            var values = textCellElement.EnumerateArray().ToList();
            if (values.Count < 3)
                continue;

            var employeeId = GetInt(values[0]);
            var dayOfMonth = GetInt(values[1]);
            var value = values[2].ValueKind == JsonValueKind.String ? values[2].GetString()?.Trim() ?? string.Empty : string.Empty;

            if (employeeId <= 0 || dayOfMonth < 1 || dayOfMonth > 31 || string.IsNullOrWhiteSpace(value) || string.Equals(value, EmptyMark, StringComparison.Ordinal))
                continue;

            var key = $"{employeeId}:{dayOfMonth}";
            if (!seenEntries.Add(key))
                continue;

            result.Add(new GraphNoteExportTextCell(employeeId, dayOfMonth, value));
        }

        return result;
    }

    private static JsonElement? GetOptionalProperty(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var property) ? property : null;

    private static bool TryGetInt(JsonElement element, string propertyName, out int value)
    {
        value = 0;
        if (!element.TryGetProperty(propertyName, out var property))
            return false;

        value = GetInt(property);
        return value != 0 || (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out _));
    }

    private static int GetInt(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var value))
            return value;

        if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out value))
            return value;

        return 0;
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
            return null;

        return property.GetString();
    }
}
