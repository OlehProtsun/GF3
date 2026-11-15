using System.Text;
using ClosedXML.Excel;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.Content;
using PdfSharp.Pdf.Content.Objects;
using WebApi.Services;

namespace GF3.Tests;

public sealed class SchedulePdfFontFixture
{
    public SchedulePdfFontFixture() => SchedulePdfFontResolver.ConfigureOnce();
}

public sealed class ScheduleWorksheetPdfRendererTests : IClassFixture<SchedulePdfFontFixture>
{
    public ScheduleWorksheetPdfRendererTests(SchedulePdfFontFixture fixture) { }

    [Theory]
    [InlineData(2026, 10)]
    [InlineData(2026, 2)]
    [InlineData(2028, 2)]
    [InlineData(2026, 4)]
    public void Render_ProducesOnlyMatrixOnThreeLandscapePages(int year, int month)
    {
        var bytes = new ScheduleWorksheetPdfRenderer().Render(CreateWorkbook(year, month));
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(bytes));
        using var pdf = PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.Import);
        Assert.Equal(3, pdf.PageCount);
        var days = DateTime.DaysInMonth(year, month);
        var texts = pdf.Pages.Cast<PdfSharp.Pdf.PdfPage>().Select(page => ContentReader.ReadContent(page).OfType<COperator>()
            .Where(operation => operation.OpCode.Name == "Tj").Select(operation => operation.Operands[0].ToString()).ToArray()).ToArray();
        for (var index = 0; index < pdf.PageCount; index++)
        {
            var page = pdf.Pages[index];
            Assert.InRange(page.Width.Point, 841, 843);
            Assert.InRange(page.Height.Point, 594, 596);
            var columns = index < 2 ? 10 : 5;
            // Title, employee headers, repeated dates and all values, allowing two lines for long intervals.
            Assert.InRange(texts[index].Length, 1 + columns + days * (1 + columns), 1 + columns + days * (1 + columns) + columns);
            Assert.Equal(texts[0][11], texts[index][1 + columns]);
        }
    }

    [Fact]
    public void Render_PaginatesByWidthsAndPreservesPlaceholderColumns()
    {
        using var workbook = new XLWorkbook(new MemoryStream(CreateWorkbook(2026, 10)));
        workbook.Worksheet(1).Columns(3, 27).Width = 5;
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var bytes = new ScheduleWorksheetPdfRenderer().Render(stream.ToArray());
        using var pdf = PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.Import);
        Assert.Equal(1, pdf.PageCount);
    }

    [Fact]
    public void Render_RejectsEmptyMalformedWrongSheetAndOversizedColumns()
    {
        var renderer = new ScheduleWorksheetPdfRenderer();
        Assert.Throws<ArgumentException>(() => renderer.Render([]));
        Assert.ThrowsAny<Exception>(() => renderer.Render([1, 2, 3]));
        using var workbook = new XLWorkbook(new MemoryStream(CreateWorkbook(2026, 10)));
        workbook.Worksheet(1).PageSetup.PrintAreas.Clear();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        Assert.Throws<InvalidOperationException>(() => renderer.Render(stream.ToArray()));
        workbook.Worksheet(1).PageSetup.PrintAreas.Add("B1:AA32");
        workbook.Worksheet(1).Column(3).Width = 200;
        using var oversizedStream = new MemoryStream();
        workbook.SaveAs(oversizedStream);
        Assert.Throws<InvalidOperationException>(() => renderer.Render(oversizedStream.ToArray()));
        Assert.Throws<OperationCanceledException>(() => renderer.Render(CreateWorkbook(2026, 10), new CancellationToken(true)));
    }

    internal static byte[] CreateWorkbook(int year, int month)
    {
        using var workbook = new XLWorkbook();
        var matrix = workbook.AddWorksheet("Synthetic schedule");
        matrix.PageSetup.PrintAreas.Add("B1:AA32");
        matrix.Column(2).Width = 9.715156;
        matrix.Columns(3, 9).Width = 11.142857;
        matrix.Columns(10, 11).Width = 15.142857;
        matrix.Column(12).Width = 15.428571;
        matrix.Columns(13, 15).Width = 11.142857;
        matrix.Column(16).Width = 15.428571;
        matrix.Column(17).Width = 15.142857;
        matrix.Column(18).Width = 20;
        matrix.Columns(19, 27).Width = 9.574531;
        matrix.Row(1).Height = 100;
        matrix.Rows(2, 32).Height = 14.52;
        matrix.Cell("B1").Value = "GF3";
        for (var column = 3; column <= 27; column++)
        {
            matrix.Cell(1, column).Value = column < 25 ? $"Łąś Employee {column - 2}" : "0";
            for (var day = 1; day <= DateTime.DaysInMonth(year, month); day++)
            {
                matrix.Cell(day + 1, 2).Value = new DateTime(year, month, day);
                matrix.Cell(day + 1, column).Value = day == 2 ? "08:00 - 12:00, 14:00 - 18:00" : "-";
            }
        }
        workbook.AddWorksheet("Synthetic - Statistic").Cell("A1").Value = "NEVER PRINT STATISTICS";
        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return output.ToArray();
    }
}
