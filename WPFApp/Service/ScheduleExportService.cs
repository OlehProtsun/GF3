using ClosedXML.Excel;
using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WPFApp.Infrastructure;
using WPFApp.Infrastructure.ScheduleMatrix;
using WPFApp.ViewModel.Container.ScheduleEdit.Helpers;
using WPFApp.ViewModel.Container.ScheduleProfile;

namespace WPFApp.Service
{
    public sealed class ScheduleExportContext
    {
        public string ScheduleName { get; }
        public int ScheduleMonth { get; }
        public int ScheduleYear { get; }
        public string ShopName { get; }
        public string ShopAddress { get; }
        public string TotalHoursText { get; }
        public int TotalEmployees { get; }
        public int TotalDays { get; }
        public string Shift1 { get; }
        public string Shift2 { get; }
        public string TotalEmployeesListText { get; }
        public DataView ScheduleMatrix { get; }
        public IReadOnlyList<ContainerScheduleProfileViewModel.SummaryDayHeader> SummaryDayHeaders { get; }
        public IReadOnlyList<ContainerScheduleProfileViewModel.SummaryEmployeeRow> SummaryRows { get; }
        public IReadOnlyList<ContainerScheduleProfileViewModel.EmployeeWorkFreeStatRow> EmployeeWorkFreeStats { get; }
        public IScheduleMatrixStyleProvider StyleProvider { get; }

        public ScheduleExportContext(
            string scheduleName,
            int scheduleMonth,
            int scheduleYear,
            string shopName,
            string shopAddress,
            string totalHoursText,
            int totalEmployees,
            int totalDays,
            string shift1,
            string shift2,
            string totalEmployeesListText,
            DataView scheduleMatrix,
            IReadOnlyList<ContainerScheduleProfileViewModel.SummaryDayHeader> summaryDayHeaders,
            IReadOnlyList<ContainerScheduleProfileViewModel.SummaryEmployeeRow> summaryRows,
            IReadOnlyList<ContainerScheduleProfileViewModel.EmployeeWorkFreeStatRow> employeeWorkFreeStats,
            IScheduleMatrixStyleProvider styleProvider)
        {
            ScheduleName = scheduleName ?? string.Empty;
            ScheduleMonth = scheduleMonth;
            ScheduleYear = scheduleYear;
            ShopName = shopName ?? string.Empty;
            ShopAddress = shopAddress ?? string.Empty;
            TotalHoursText = totalHoursText ?? string.Empty;
            TotalEmployees = totalEmployees;
            TotalDays = totalDays;
            Shift1 = shift1 ?? string.Empty;
            Shift2 = shift2 ?? string.Empty;
            TotalEmployeesListText = totalEmployeesListText ?? string.Empty;

            ScheduleMatrix = scheduleMatrix ?? new DataView();
            SummaryDayHeaders = summaryDayHeaders ?? Array.Empty<ContainerScheduleProfileViewModel.SummaryDayHeader>();
            SummaryRows = summaryRows ?? Array.Empty<ContainerScheduleProfileViewModel.SummaryEmployeeRow>();
            EmployeeWorkFreeStats = employeeWorkFreeStats ?? Array.Empty<ContainerScheduleProfileViewModel.EmployeeWorkFreeStatRow>();

            StyleProvider = styleProvider ?? throw new ArgumentNullException(nameof(styleProvider));
        }
    }

    public sealed class AvailabilityGroupExportData
    {
        public AvailabilityGroupModel Group { get; }
        public IReadOnlyList<AvailabilityGroupMemberModel> Members { get; }
        public IReadOnlyList<AvailabilityGroupDayModel> Days { get; }

        public AvailabilityGroupExportData(
            AvailabilityGroupModel group,
            IReadOnlyList<AvailabilityGroupMemberModel> members,
            IReadOnlyList<AvailabilityGroupDayModel> days)
        {
            Group = group ?? throw new ArgumentNullException(nameof(group));
            Members = members ?? Array.Empty<AvailabilityGroupMemberModel>();
            Days = days ?? Array.Empty<AvailabilityGroupDayModel>();
        }
    }

    public sealed class ScheduleSqlExportContext
    {
        public ScheduleModel Schedule { get; }
        public IReadOnlyList<ScheduleEmployeeModel> Employees { get; }
        public IReadOnlyList<ScheduleSlotModel> Slots { get; }
        public IReadOnlyList<ScheduleCellStyleModel> CellStyles { get; }
        public AvailabilityGroupExportData? AvailabilityGroupData { get; }

        public ScheduleSqlExportContext(
            ScheduleModel schedule,
            IReadOnlyList<ScheduleEmployeeModel> employees,
            IReadOnlyList<ScheduleSlotModel> slots,
            IReadOnlyList<ScheduleCellStyleModel> cellStyles,
            AvailabilityGroupExportData? availabilityGroupData)
        {
            Schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
            Employees = employees ?? Array.Empty<ScheduleEmployeeModel>();
            Slots = slots ?? Array.Empty<ScheduleSlotModel>();
            CellStyles = cellStyles ?? Array.Empty<ScheduleCellStyleModel>();
            AvailabilityGroupData = availabilityGroupData;
        }
    }

    public sealed class ScheduleExportService : IScheduleExportService
    {
        private const string DefaultSheetName = "Schedule";
        private const string DefaultStatisticSheetName = "Schedule - Statistic";

        // ===================== TEMPLATE CONFIG =====================
        // Put template here (Build Action: Content, Copy to Output Directory: Copy if newer/always)
        private const string ExcelTemplateRelativePath = @"Resources\Excel\ScheduleTemplate.xlsx";

        // Supported template sheet names (your template had R_FL_35 originally)
        private static readonly string[] MatrixTemplateSheetNames = { "ScheduleName", "R_FL_35", "MatrixTemplate" };
        private static readonly string[] StatisticTemplateSheetNames = { "ScheduleStatistic", "Schedule Statistic", "StatisticTemplate" };

        // ===================== MATRIX TEMPLATE LAYOUT =====================
        // Expected in template:
        //   B1  = ShopName
        //   C1..AA1 = employee headers (rotated + tall row)
        //   B2..B32 = dates
        //   C2..AA32 = matrix body
        private const string MatrixClearRange = "B1:AA32";
        private const int M_ShopRow = 1;
        private const int M_ShopCol = 2;         // B1
        private const int M_HeaderRow = 1;
        private const int M_DateCol = 2;         // B
        private const int M_FirstDataCol = 3;    // C
        private const int M_LastDataCol = 27;    // AA (25 cols)
        private const int M_FirstDayRow = 2;
        private const int M_DayCount = 31;

        // ===================== STAT TEMPLATE LAYOUT =====================
        private const int S_ValueCol = 2;                // B
        private const int S_EmployeesLineRow = 12;       // A12 merged in template typically
        private const int S_DayHeaderRow = 14;           // merged day header blocks
        private const int S_BodyFirstRow = 16;           // first employee row in summary
        private const int S_MaxSummaryRows = 10;         // template-styled rows (16..25)
        private const int S_MiniBodyFirstRow = 28;       // first row of mini-table body
        private const int S_MaxMiniRows = 10;            // template-styled rows (28..37)
        private const int S_FirstDayCol = 3;             // C
        private const int S_DaysCapacity = 31;           // 31 days * 3 columns (C..CQ)

        // ===================== PUBLIC API =====================
        public Task ExportToExcelAsync(ScheduleExportContext context, string filePath, CancellationToken ct = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));

            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                var templatePath = ResolveExcelTemplatePath();
                using var workbook = new XLWorkbook(templatePath);

                var matrixTemplate = FindTemplateSheet(workbook, MatrixTemplateSheetNames)
                    ?? throw new InvalidOperationException($"Matrix template sheet not found. Expected one of: {string.Join(", ", MatrixTemplateSheetNames)}");

                var statTemplate = FindTemplateSheet(workbook, StatisticTemplateSheetNames)
                    ?? throw new InvalidOperationException($"Statistic template sheet not found. Expected one of: {string.Join(", ", StatisticTemplateSheetNames)}");

                // Copy template sheets to new names
                var scheduleName = SanitizeWorksheetName(context.ScheduleName, DefaultSheetName);

                // Important: CopyTo keeps 1:1 styling (row heights, column widths, CF, patterns, rotated text)
                var matrixSheet = matrixTemplate.CopyTo(scheduleName);

                var statisticNameBase = SanitizeWorksheetName($"{scheduleName} - Statistic", DefaultStatisticSheetName);
                var statisticName = MakeUniqueSheetName(workbook, statisticNameBase);
                var statSheet = statTemplate.CopyTo(statisticName);

                // Remove original template sheets from output
                matrixTemplate.Delete();
                statTemplate.Delete();

                // Fill ONLY values (do not modify styling)
                FillMatrixSheetFromTemplate(matrixSheet, context);
                FillStatisticSheetFromTemplate(statSheet, context);

                // Normalize problematic Gray125 pattern fills (ClosedXML can roundtrip them as black)
                FixGray125Fills(matrixSheet, "B2:AA32");
                FixGray125Fills(statSheet);

                workbook.SaveAs(filePath);
            }, ct);
        }

        public async Task ExportToSqlAsync(ScheduleSqlExportContext context, string filePath, CancellationToken ct = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));

            var script = BuildSqlScript(context);
            await File.WriteAllTextAsync(filePath, script, Encoding.UTF8, ct).ConfigureAwait(false);
        }

        // ===================== TEMPLATE HELPERS =====================
        private static string ResolveExcelTemplatePath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var path = Path.Combine(baseDir, ExcelTemplateRelativePath);

            if (!File.Exists(path))
                throw new FileNotFoundException("Excel template not found. Ensure it is copied to output directory.", path);

            return path;
        }

        private static IXLWorksheet? FindTemplateSheet(XLWorkbook wb, IEnumerable<string> names)
        {
            foreach (var n in names)
            {
                var ws = wb.Worksheets.FirstOrDefault(s => string.Equals(s.Name, n, StringComparison.OrdinalIgnoreCase));
                if (ws != null) return ws;
            }
            return null;
        }

        private static string MakeUniqueSheetName(XLWorkbook wb, string baseName)
        {
            var name = baseName;
            var i = 1;
            while (wb.Worksheets.Any(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                var suffix = $" ({i++})";
                var trimmed = baseName;
                if (trimmed.Length + suffix.Length > 31)
                    trimmed = trimmed.Substring(0, 31 - suffix.Length);
                name = trimmed + suffix;
            }
            return name;
        }

        public static string SanitizeFileName(string? name, string fallback)
        {
            var safe = string.IsNullOrWhiteSpace(name) ? fallback : name.Trim();
            foreach (var c in Path.GetInvalidFileNameChars())
                safe = safe.Replace(c, '_');
            return string.IsNullOrWhiteSpace(safe) ? fallback : safe;
        }

        private static string SanitizeWorksheetName(string? name, string fallback)
        {
            var safe = string.IsNullOrWhiteSpace(name) ? fallback : name.Trim();

            var invalid = new[] { '[', ']', '*', '?', '/', '\\', ':' };
            foreach (var c in invalid)
                safe = safe.Replace(c, '_');

            if (safe.Length > 31)
                safe = safe.Substring(0, 31);

            return string.IsNullOrWhiteSpace(safe) ? fallback : safe;
        }

        /// <summary>
        /// ClosedXML (and some other OOXML writers) can roundtrip PatternType=Gray125 incorrectly
        /// (often ending up as solid black in Excel). The safest workaround is to replace Gray125
        /// with an explicit solid fill (white) both for direct cell styles and for conditional formats.
        /// </summary>
        private static void FixGray125Fills(IXLWorksheet sheet, string? rangeAddress = null)
        {
            if (sheet is null) return;

            // 1) Direct cell styles (only where template uses Gray125)
            var range = !string.IsNullOrWhiteSpace(rangeAddress) ? sheet.Range(rangeAddress) : sheet.RangeUsed();
            if (range != null)
            {
                foreach (var cell in range.Cells())
                {
                    if (cell.Style.Fill.PatternType == XLFillPatternValues.Gray125)
                    {
                        // Keep the 12.5% gray pattern, but force explicit colors (avoid Excel showing it as black)
                        cell.Style.Fill.PatternType = XLFillPatternValues.Gray125;
                        cell.Style.Fill.BackgroundColor = XLColor.White;
                        cell.Style.Fill.PatternColor = XLColor.Black; // pattern (foreground)
                    }
                }
            }

            // 2) Conditional formats (template may have weekend rule using Gray125)
            foreach (var cf in sheet.ConditionalFormats)
            {
                if (cf.Style.Fill.PatternType == XLFillPatternValues.Gray125)
                {
                    // Keep the 12.5% gray pattern, but force explicit colors (avoid Excel showing it as black)
                    cf.Style.Fill.PatternType = XLFillPatternValues.Gray125;
                    cf.Style.Fill.BackgroundColor = XLColor.White;
                    cf.Style.Fill.PatternColor = XLColor.Black; // pattern (foreground)
                }
            }
        }

        // ===================== EXCEL FILL: MATRIX (1:1 style) =====================
        private static void FillMatrixSheetFromTemplate(IXLWorksheet sheet, ScheduleExportContext context)
        {
            // Clear ONLY contents (keep template styles + column widths + row heights + conditional formatting)
            sheet.Range(MatrixClearRange).Clear(XLClearOptions.Contents);

            // B1 = ShopName
            sheet.Cell(M_ShopRow, M_ShopCol).Value = context.ShopName;

            var table = context.ScheduleMatrix?.Table;
            if (table is null) return;

            var visibleCols = table.Columns.Cast<DataColumn>()
                .Where(c => c.ColumnName != ScheduleMatrixConstants.ConflictColumnName
                         && c.ColumnName != ScheduleMatrixConstants.WeekendColumnName)
                .ToList();

            var dayCol = visibleCols.FirstOrDefault(c => c.ColumnName == ScheduleMatrixConstants.DayColumnName)
                         ?? visibleCols.FirstOrDefault();

            var dataCols = visibleCols.Where(c => !ReferenceEquals(c, dayCol)).ToList();

            // Capacity = template width: C..AA
            var capacity = M_LastDataCol - M_FirstDataCol + 1;
            if (dataCols.Count > capacity)
                throw new InvalidOperationException($"Template supports max {capacity} employee columns (C..AA). Current: {dataCols.Count}.");

            // Header: employee names
            for (int i = 0; i < dataCols.Count; i++)
            {
                var col = dataCols[i];
                sheet.Cell(M_HeaderRow, M_FirstDataCol + i).Value =
                    string.IsNullOrWhiteSpace(col.Caption) ? col.ColumnName : col.Caption;
            }

            // Header placeholders (no employee) -> "0"
            for (int c = M_FirstDataCol + dataCols.Count; c <= M_LastDataCol; c++)
                sheet.Cell(M_HeaderRow, c).Value = "0";

            var daysInMonth = DateTime.DaysInMonth(context.ScheduleYear, context.ScheduleMonth);
            var maxRows = Math.Min(M_DayCount, context.ScheduleMatrix.Count);

            // Fill days (rows 2..32)
            for (int r = 0; r < M_DayCount; r++)
            {
                var excelRow = M_FirstDayRow + r;
                DataRowView? rowView = r < maxRows ? (DataRowView)context.ScheduleMatrix[r] : null;

                // === KEY FIX ===
                // For real month days -> write DateTime (template keeps its own date format like "pon., 02/02").
                // For non-existing days (e.g. 29-31 in Feb) -> write TEXT (" ") so Excel CF WEEKDAY() doesn't treat blank as 0 (Sunday).
                var dateCell = sheet.Cell(excelRow, M_DateCol);

                if (r < daysInMonth)
                {
                    // Prefer any explicit date/day in data (if present), otherwise fall back to calendar day index.
                    var date = rowView != null ? TryBuildDate(context, dayCol, rowView) : null;
                    date ??= SafeDate(context.ScheduleYear, context.ScheduleMonth, r + 1);

                    if (date.HasValue)
                        dateCell.Value = date.Value;   // do NOT touch styles/number formats
                    else
                        dateCell.Value = " ";          // safety fallback (non-numeric)
                }
                else
                {
                    dateCell.Value = " ";              // IMPORTANT: non-numeric placeholder, visually empty
                }

                // Real employee columns
                for (int i = 0; i < dataCols.Count; i++)
                {
                    var col = dataCols[i];
                    var cell = sheet.Cell(excelRow, M_FirstDataCol + i);

                    if (rowView == null)
                    {
                        cell.Value = "-";
                        continue;
                    }

                    SetDashIfEmpty(cell, rowView[col.ColumnName]);
                }

                // Placeholder columns (no employee) -> "-"
                for (int c = M_FirstDataCol + dataCols.Count; c <= M_LastDataCol; c++)
                    sheet.Cell(excelRow, c).Value = "-";
            }
        }

        private static void SetDashIfEmpty(IXLCell cell, object? value)
        {
            if (value is null || value == DBNull.Value)
            {
                cell.Value = "-";
                return;
            }

            var text = value.ToString()?.Trim() ?? string.Empty;
            cell.Value = string.IsNullOrWhiteSpace(text) ? "-" : text;
        }

        // ===== helpers =====
        private static DateTime? TryBuildDate(ScheduleExportContext context, DataColumn? dayCol, DataRowView rowView)
        {
            if (dayCol is null) return null;

            var raw = rowView[dayCol.ColumnName];
            if (raw is null || raw == DBNull.Value) return null;

            // already a date
            if (raw is DateTime dt) return dt.Date;

            // day-of-month as int / string
            if (raw is int d && d >= 1 && d <= 31)
                return SafeDate(context.ScheduleYear, context.ScheduleMonth, d);

            var s = raw.ToString()?.Trim();
            if (int.TryParse(s, out var parsed) && parsed >= 1 && parsed <= 31)
                return SafeDate(context.ScheduleYear, context.ScheduleMonth, parsed);

            // full date string in current culture (optional)
            if (!string.IsNullOrWhiteSpace(s) &&
                DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.None, out var parsedDt))
                return parsedDt.Date;

            return null;
        }

        private static DateTime? SafeDate(int year, int month, int day)
        {
            try { return new DateTime(year, month, day); }
            catch { return null; }
        }

        private static void SetExcelValuePreservingFormat(IXLCell cell, object? value)
        {
            if (value is null || value == DBNull.Value)
            {
                cell.Clear(XLClearOptions.Contents);
                return;
            }

            // If already typed:
            if (value is TimeSpan ts)
            {
                cell.Value = ts;
                return;
            }
            if (value is DateTime dt)
            {
                cell.Value = dt;
                return;
            }
            if (value is int or long or double or decimal)
            {
                cell.Value = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                return;
            }

            var text = value.ToString()?.Trim() ?? string.Empty;

            // Try parse HH:mm (helps if template uses time format)
            if (TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out var t1))
            {
                cell.Value = t1;
                return;
            }

            cell.Value = text;
        }

        // ===================== EXCEL FILL: STATISTIC (1:1 style) =====================
        private static void FillStatisticSheetFromTemplate(IXLWorksheet sheet, ScheduleExportContext context)
        {
            sheet.Cell(2, S_ValueCol).Value = context.ScheduleName;
            sheet.Cell(3, S_ValueCol).Value = context.ScheduleMonth.ToString("D2", CultureInfo.InvariantCulture);
            sheet.Cell(4, S_ValueCol).Value = context.ScheduleYear.ToString(CultureInfo.InvariantCulture);
            sheet.Cell(5, S_ValueCol).Value = context.ShopName;
            sheet.Cell(6, S_ValueCol).Value = context.ShopAddress;
            sheet.Cell(7, S_ValueCol).Value = context.TotalHoursText;
            sheet.Cell(8, S_ValueCol).Value = context.TotalEmployees.ToString(CultureInfo.InvariantCulture);
            sheet.Cell(9, S_ValueCol).Value = context.TotalDays.ToString(CultureInfo.InvariantCulture);
            sheet.Cell(10, S_ValueCol).Value = context.Shift1;
            sheet.Cell(11, S_ValueCol).Value = context.Shift2;

            sheet.Cell(S_EmployeesLineRow, 1).Value =
                $"Total employees ({context.TotalEmployees}): {context.TotalEmployeesListText}";

            for (int i = 0; i < S_DaysCapacity; i++)
            {
                var col = S_FirstDayCol + i * 3;
                var text = i < context.SummaryDayHeaders.Count ? context.SummaryDayHeaders[i].Text : string.Empty;
                sheet.Cell(S_DayHeaderRow, col).Value = text;
            }

            if (context.SummaryRows.Count > S_MaxSummaryRows)
                throw new InvalidOperationException($"Statistic template supports max {S_MaxSummaryRows} summary rows. Current: {context.SummaryRows.Count}. Extend the template styling below row {S_BodyFirstRow + S_MaxSummaryRows - 1}.");

            for (int r = 0; r < S_MaxSummaryRows; r++)
            {
                var row = S_BodyFirstRow + r;

                if (r >= context.SummaryRows.Count)
                {
                    sheet.Cell(row, 1).Clear(XLClearOptions.Contents);
                    sheet.Cell(row, 2).Clear(XLClearOptions.Contents);
                    for (int d = 0; d < S_DaysCapacity; d++)
                    {
                        var c = S_FirstDayCol + d * 3;
                        sheet.Cell(row, c).Clear(XLClearOptions.Contents);
                        sheet.Cell(row, c + 1).Clear(XLClearOptions.Contents);
                        sheet.Cell(row, c + 2).Clear(XLClearOptions.Contents);
                    }
                    continue;
                }

                var summaryRow = context.SummaryRows[r];
                sheet.Cell(row, 1).Value = summaryRow.Employee ?? string.Empty;
                sheet.Cell(row, 2).Value = summaryRow.Sum ?? string.Empty;

                var dayCells = summaryRow.Days?.ToList()
                              ?? new List<ContainerScheduleProfileViewModel.SummaryDayCell>();

                for (int d = 0; d < S_DaysCapacity; d++)
                {
                    var c = S_FirstDayCol + d * 3;
                    var day = d < dayCells.Count ? dayCells[d] : null;

                    sheet.Cell(row, c).Value = day?.From ?? string.Empty;
                    sheet.Cell(row, c + 1).Value = day?.To ?? string.Empty;
                    sheet.Cell(row, c + 2).Value = day?.Hours ?? string.Empty;
                }
            }

            if (context.EmployeeWorkFreeStats.Count > S_MaxMiniRows)
                throw new InvalidOperationException($"Statistic template supports max {S_MaxMiniRows} mini-table rows. Current: {context.EmployeeWorkFreeStats.Count}. Extend the template styling below row {S_MiniBodyFirstRow + S_MaxMiniRows - 1}.");

            for (int r = 0; r < S_MaxMiniRows; r++)
            {
                var row = S_MiniBodyFirstRow + r;

                if (r >= context.EmployeeWorkFreeStats.Count)
                {
                    sheet.Cell(row, 1).Clear(XLClearOptions.Contents);
                    sheet.Cell(row, 2).Clear(XLClearOptions.Contents);
                    sheet.Cell(row, 3).Clear(XLClearOptions.Contents);
                    continue;
                }

                var stat = context.EmployeeWorkFreeStats[r];
                sheet.Cell(row, 1).Value = stat.Employee ?? string.Empty;
                sheet.Cell(row, 2).Value = stat.WorkDays.ToString(CultureInfo.InvariantCulture);
                sheet.Cell(row, 3).Value = stat.FreeDays.ToString(CultureInfo.InvariantCulture);
            }
        }

        // ===================== SQL EXPORT (unchanged logic, just kept) =====================
        private static string BuildSqlScript(ScheduleSqlExportContext context)
        {
            var schedule = context.Schedule;
            var container = schedule.Container;
            var shop = schedule.Shop;

            var employees = CollectEmployees(context);
            var slots = context.Slots?.ToList() ?? new List<ScheduleSlotModel>();
            var cellStyles = context.CellStyles?.ToList() ?? new List<ScheduleCellStyleModel>();
            var scheduleEmployees = context.Employees?.ToList() ?? new List<ScheduleEmployeeModel>();

            var sb = new StringBuilder(8192);
            sb.AppendLine("-- GF3 Schedule export");
            sb.AppendLine($"-- Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("BEGIN TRANSACTION;");
            sb.AppendLine();

            if (container != null)
            {
                sb.AppendLine(SqlInsert("container",
                    ("id", container.Id),
                    ("name", container.Name),
                    ("note", container.Note)));
            }

            if (shop != null)
            {
                sb.AppendLine(SqlInsert("shop",
                    ("id", shop.Id),
                    ("name", shop.Name),
                    ("address", shop.Address),
                    ("description", shop.Description)));
            }

            foreach (var employee in employees.OrderBy(e => e.Id))
            {
                sb.AppendLine(SqlInsert("employee",
                    ("id", employee.Id),
                    ("first_name", employee.FirstName),
                    ("last_name", employee.LastName),
                    ("phone", employee.Phone),
                    ("email", employee.Email)));
            }

            if (context.AvailabilityGroupData is not null)
            {
                var group = context.AvailabilityGroupData.Group;
                sb.AppendLine(SqlInsert("availability_group",
                    ("id", group.Id),
                    ("name", group.Name),
                    ("year", group.Year),
                    ("month", group.Month)));

                foreach (var member in context.AvailabilityGroupData.Members.OrderBy(m => m.Id))
                {
                    sb.AppendLine(SqlInsert("availability_group_member",
                        ("id", member.Id),
                        ("availability_group_id", member.AvailabilityGroupId),
                        ("employee_id", member.EmployeeId)));
                }

                foreach (var day in context.AvailabilityGroupData.Days.OrderBy(d => d.Id))
                {
                    sb.AppendLine(SqlInsert("availability_group_day",
                        ("id", day.Id),
                        ("availability_group_member_id", day.AvailabilityGroupMemberId),
                        ("day_of_month", day.DayOfMonth),
                        ("kind", day.Kind.ToString()),
                        ("interval_str", day.IntervalStr)));
                }
            }

            sb.AppendLine(SqlInsert("schedule",
                ("id", schedule.Id),
                ("container_id", schedule.ContainerId),
                ("shop_id", schedule.ShopId),
                ("name", schedule.Name),
                ("year", schedule.Year),
                ("month", schedule.Month),
                ("people_per_shift", schedule.PeoplePerShift),
                ("shift1_time", schedule.Shift1Time),
                ("shift2_time", schedule.Shift2Time),
                ("max_hours_per_emp_month", schedule.MaxHoursPerEmpMonth),
                ("max_consecutive_days", schedule.MaxConsecutiveDays),
                ("max_consecutive_full", schedule.MaxConsecutiveFull),
                ("max_full_per_month", schedule.MaxFullPerMonth),
                ("note", schedule.Note),
                ("availability_group_id", schedule.AvailabilityGroupId)));

            foreach (var scheduleEmployee in scheduleEmployees.OrderBy(e => e.Id))
            {
                sb.AppendLine(SqlInsert("schedule_employee",
                    ("id", scheduleEmployee.Id),
                    ("schedule_id", scheduleEmployee.ScheduleId),
                    ("employee_id", scheduleEmployee.EmployeeId),
                    ("min_hours_month", scheduleEmployee.MinHoursMonth)));
            }

            foreach (var slot in slots.OrderBy(s => s.Id))
            {
                sb.AppendLine(SqlInsert("schedule_slot",
                    ("id", slot.Id),
                    ("schedule_id", slot.ScheduleId),
                    ("day_of_month", slot.DayOfMonth),
                    ("slot_no", slot.SlotNo),
                    ("employee_id", slot.EmployeeId),
                    ("status", slot.Status.ToString()),
                    ("from_time", slot.FromTime),
                    ("to_time", slot.ToTime)));
            }

            foreach (var style in cellStyles.OrderBy(s => s.Id))
            {
                sb.AppendLine(SqlInsert("schedule_cell_style",
                    ("id", style.Id),
                    ("schedule_id", style.ScheduleId),
                    ("day_of_month", style.DayOfMonth),
                    ("employee_id", style.EmployeeId),
                    ("background_color_argb", style.BackgroundColorArgb),
                    ("text_color_argb", style.TextColorArgb)));
            }

            sb.AppendLine();
            sb.AppendLine("COMMIT;");
            return sb.ToString();
        }

        private static List<EmployeeModel> CollectEmployees(ScheduleSqlExportContext context)
        {
            var employees = new Dictionary<int, EmployeeModel>();

            foreach (var scheduleEmployee in context.Employees ?? Array.Empty<ScheduleEmployeeModel>())
            {
                var employee = scheduleEmployee.Employee;
                if (employee != null && !employees.ContainsKey(employee.Id))
                    employees.Add(employee.Id, employee);
            }

            if (context.AvailabilityGroupData is not null)
            {
                foreach (var member in context.AvailabilityGroupData.Members)
                {
                    var employee = member.Employee;
                    if (employee != null && !employees.ContainsKey(employee.Id))
                        employees.Add(employee.Id, employee);
                }
            }

            return employees.Values.ToList();
        }

        private static string SqlInsert(string table, params (string Column, object? Value)[] values)
        {
            var columns = string.Join(", ", values.Select(v => v.Column));
            var vals = string.Join(", ", values.Select(v => ToSqlLiteral(v.Value)));
            return $"INSERT INTO {table} ({columns}) VALUES ({vals});";
        }

        private static string ToSqlLiteral(object? value)
        {
            if (value is null) return "NULL";

            if (value is string s) return $"'{EscapeSqlString(s)}'";
            if (value is bool b) return b ? "1" : "0";
            if (value is DateTime dt) return $"'{dt:yyyy-MM-dd HH:mm:ss}'";
            if (value is Enum) return $"'{value}'";

            if (value is IFormattable formattable)
                return formattable.ToString(null, CultureInfo.InvariantCulture) ?? "NULL";

            return $"'{EscapeSqlString(value.ToString() ?? string.Empty)}'";
        }

        private static string EscapeSqlString(string value) => value.Replace("'", "''");

        // ===================== EXISTING HELPERS (kept) =====================
        private static bool GetBoolValue(DataRowView rowView, string columnName)
        {
            if (rowView?.Row?.Table == null) return false;
            if (!rowView.Row.Table.Columns.Contains(columnName)) return false;

            var value = rowView[columnName];
            if (value is bool b) return b;
            if (value is null || value == DBNull.Value) return false;

            if (bool.TryParse(value.ToString(), out var parsed))
                return parsed;

            return false;
        }
    }
}
