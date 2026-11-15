using System.Globalization;
using ClosedXML.Excel;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace WebApi.Services;

public sealed class ScheduleWorksheetPdfRenderer : IScheduleWorksheetPdfRenderer
{
    private const double Margin = 18;
    // Calibrated against the manager workbook after ClosedXML removes Excel's column padding.
    private const double WidthUnit = 5.75;
    private static readonly string[] Weekdays = ["niedz.", "pon.", "wt.", "śr.", "czw.", "pt.", "sob."];

    public byte[] Render(byte[] workbookBytes, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(workbookBytes);
        if (workbookBytes.Length == 0) throw new ArgumentException("Schedule workbook is empty.", nameof(workbookBytes));
        ct.ThrowIfCancellationRequested();
        using var input = new MemoryStream(workbookBytes, writable: false);
        using var workbook = new XLWorkbook(input);
        if (workbook.Worksheets.Count < 2) throw new InvalidOperationException("Schedule matrix and statistics sheets are required.");
        var sheet = workbook.Worksheet(1);
        if (sheet.Name.EndsWith("Statistic", StringComparison.OrdinalIgnoreCase)
            || !sheet.PageSetup.PrintAreas.Any(area => area.RangeAddress.FirstAddress.RowNumber <= 1
                && area.RangeAddress.FirstAddress.ColumnNumber <= 2
                && area.RangeAddress.LastAddress.RowNumber >= 32
                && area.RangeAddress.LastAddress.ColumnNumber >= 27))
            throw new InvalidOperationException("First worksheet must be the schedule matrix with print area B1:AA32.");

        var widths = Enumerable.Range(2, 26).Select(column => sheet.Column(column).Width * WidthUnit).ToArray();
        if (widths.Any(width => !double.IsFinite(width) || width <= 0))
            throw new InvalidOperationException("Schedule matrix has invalid column widths.");
        var headerHeight = Math.Clamp(sheet.Row(1).Height, 95, 105);
        var rowHeights = Enumerable.Range(2, 31).Select(row => sheet.Row(row).Height).ToArray();
        if (rowHeights.Any(height => !double.IsFinite(height) || height <= 0))
            throw new InvalidOperationException("Schedule matrix has invalid row heights.");
        var rowScale = Math.Min(1, (595.276 - 2 * Margin - headerHeight) / rowHeights.Sum());
        using var document = new PdfDocument();
        document.Info.Title = "GF3 Schedule";
        var font = new XFont("GF3Schedule", 10, XFontStyleEx.Regular);
        var firstDate = Enumerable.Range(2, 31).Select(row => ReadDate(sheet.Cell(row, 2)))
            .FirstOrDefault(date => date.HasValue);
        var column = 3;
        while (column <= 27)
        {
            ct.ThrowIfCancellationRequested();
            var page = document.AddPage();
            page.Size = PageSize.A4;
            page.Orientation = PageOrientation.Landscape;
            var available = page.Width.Point - 2 * Margin;
            var endColumn = column;
            var usedWidth = widths[0];
            while (endColumn <= 27 && usedWidth + widths[endColumn - 2] <= available + 0.01)
            {
                usedWidth += widths[endColumn - 2];
                endColumn++;
            }
            if (endColumn == column)
                throw new InvalidOperationException("Schedule column is wider than the printable page.");
            using var graphics = XGraphics.FromPdfPage(page);
            var headerY = Margin;
            var bodyY = headerY + headerHeight;
            DrawRotated(graphics, sheet.Cell("B1").GetFormattedString(), font,
                new XRect(Margin, headerY, widths[0], headerHeight), -45);
            var x = Margin + widths[0];
            for (var c = column; c < endColumn; c++)
            {
                DrawRotated(graphics, sheet.Cell(1, c).GetFormattedString(), font,
                    new XRect(x, headerY, widths[c - 2], headerHeight), -90);
                x += widths[c - 2];
            }
            var y = bodyY;
            for (var row = 2; row <= 32; row++)
            {
                ct.ThrowIfCancellationRequested();
                var height = rowHeights[row - 2] * rowScale;
                var date = ReadDate(sheet.Cell(row, 2));
                if (date.HasValue && firstDate.HasValue
                    && (date.Value.Year != firstDate.Value.Year || date.Value.Month != firstDate.Value.Month)) date = null;
                if (date?.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    DrawWeekendHatch(graphics, new XRect(Margin, y, usedWidth, height));
                var dateLabel = date is { } value
                    ? $"{Weekdays[(int)value.DayOfWeek]} {value.ToString("dd/MM", CultureInfo.InvariantCulture)}" : "";
                DrawCell(graphics, dateLabel, font, new XRect(Margin, y, widths[0], height), XStringFormats.CenterRight);
                x = Margin + widths[0];
                for (var c = column; c < endColumn; c++)
                {
                    DrawCell(graphics, sheet.Cell(row, c).GetFormattedString(), font,
                        new XRect(x, y, widths[c - 2], height), XStringFormats.Center);
                    x += widths[c - 2];
                }
                y += height;
            }
            column = endColumn;
        }
        ct.ThrowIfCancellationRequested();
        using var output = new MemoryStream();
        document.Save(output, closeStream: false);
        return output.ToArray();
    }

    private static DateTime? ReadDate(IXLCell cell)
        => cell.DataType == XLDataType.DateTime && cell.TryGetValue<DateTime>(out var date) ? date : null;

    private static void DrawCell(XGraphics graphics, string text, XFont font, XRect rectangle, XStringFormat format)
    {
        if (string.IsNullOrEmpty(text)) return;
        var state = graphics.Save();
        graphics.IntersectClip(rectangle);
        rectangle.Inflate(-2, 0);
        var textWidth = graphics.MeasureString(text, font).Width;
        var fittedSize = textWidth > rectangle.Width ? font.Size * rectangle.Width / textWidth : font.Size;
        if (fittedSize < 6 && text.Contains(','))
        {
            var parts = text.Split(',', StringSplitOptions.TrimEntries);
            var lines = parts.Select((part, index) => index < parts.Length - 1 ? part + "," : part).ToArray();
            var smallFont = new XFont("GF3Schedule", 6, XFontStyleEx.Regular);
            if (lines.Length * 6.5 <= rectangle.Height && lines.All(line => graphics.MeasureString(line, smallFont).Width <= rectangle.Width))
            {
                var top = rectangle.Y + (rectangle.Height - lines.Length * 6.5) / 2;
                foreach (var line in lines)
                {
                    graphics.DrawString(line, smallFont, XBrushes.Black,
                        new XRect(rectangle.X, top, rectangle.Width, 6.5), format);
                    top += 6.5;
                }
                graphics.Restore(state);
                return;
            }
        }
        var cellFont = fittedSize < font.Size ? new XFont("GF3Schedule", fittedSize, XFontStyleEx.Regular) : font;
        graphics.DrawString(text, cellFont, XBrushes.Black, rectangle, format);
        graphics.Restore(state);
    }

    private static void DrawRotated(XGraphics graphics, string text, XFont font, XRect rectangle, double angle)
    {
        var state = graphics.Save();
        graphics.IntersectClip(rectangle);
        graphics.TranslateTransform(rectangle.X + rectangle.Width / 2, rectangle.Bottom - 4);
        graphics.RotateTransform(angle);
        var available = angle == -90 ? rectangle.Height - 8 : Math.Min(rectangle.Height, rectangle.Width / 2) * Math.Sqrt(2);
        var measured = graphics.MeasureString(text, font).Width;
        var headerFont = measured > available ? new XFont("GF3Schedule", font.Size * available / measured, XFontStyleEx.Regular) : font;
        graphics.DrawString(text, headerFont, XBrushes.Black, 0, 0, XStringFormats.BaseLineLeft);
        graphics.Restore(state);
    }

    private static void DrawWeekendHatch(XGraphics graphics, XRect rectangle)
    {
        var state = graphics.Save();
        graphics.IntersectClip(rectangle);
        var pen = new XPen(XColor.FromArgb(220, 220, 220), 0.35);
        for (var x = rectangle.X - rectangle.Height; x < rectangle.Right; x += 4)
            graphics.DrawLine(pen, x, rectangle.Bottom, x + rectangle.Height, rectangle.Top);
        graphics.Restore(state);
    }
}
