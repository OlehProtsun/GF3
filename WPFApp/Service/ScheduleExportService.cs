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

        public Task ExportToExcelAsync(ScheduleExportContext context, string filePath, CancellationToken ct = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));

            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                using var workbook = new XLWorkbook();

                var scheduleName = SanitizeWorksheetName(context.ScheduleName, DefaultSheetName);
                var matrixSheet = workbook.AddWorksheet(scheduleName);

                var statisticName = SanitizeWorksheetName($"{scheduleName} - Statistic", DefaultStatisticSheetName);
                if (string.Equals(statisticName, scheduleName, StringComparison.OrdinalIgnoreCase))
                    statisticName = SanitizeWorksheetName($"{scheduleName} Stats", DefaultStatisticSheetName);

                var statisticSheet = workbook.AddWorksheet(statisticName);

                ExportMatrixSheet(matrixSheet, context);
                ExportStatisticSheet(statisticSheet, context);

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

        private static void ExportMatrixSheet(IXLWorksheet sheet, ScheduleExportContext context)
        {
            var table = context.ScheduleMatrix?.Table;
            if (table is null)
                return;

            var columns = table.Columns
                .Cast<DataColumn>()
                .Where(c => c.ColumnName != ScheduleMatrixConstants.ConflictColumnName
                         && c.ColumnName != ScheduleMatrixConstants.WeekendColumnName)
                .ToList();

            var headerRow = 1;
            var dataStartRow = headerRow + 1;

            for (int colIndex = 0; colIndex < columns.Count; colIndex++)
            {
                var column = columns[colIndex];
                var cell = sheet.Cell(headerRow, colIndex + 1);
                cell.Value = string.IsNullOrWhiteSpace(column.Caption) ? column.ColumnName : column.Caption;
                ApplyHeaderCellStyle(cell);
            }

            var rowIndex = dataStartRow;
            foreach (DataRowView rowView in context.ScheduleMatrix)
            {
                var isWeekend = GetBoolValue(rowView, ScheduleMatrixConstants.WeekendColumnName);
                var hasConflict = GetBoolValue(rowView, ScheduleMatrixConstants.ConflictColumnName);

                for (int colIndex = 0; colIndex < columns.Count; colIndex++)
                {
                    var column = columns[colIndex];
                    var cell = sheet.Cell(rowIndex, colIndex + 1);

                    cell.Value = GetCellText(rowView[column.ColumnName]);
                    ApplyMatrixBodyStyle(cell, column.ColumnName == ScheduleMatrixConstants.DayColumnName);

                    if (isWeekend)
                        ApplyWeekendStyle(cell);

                    if (hasConflict && column.ColumnName == ScheduleMatrixConstants.DayColumnName)
                        ApplyConflictStyle(cell);

                    if (context.StyleProvider.TryBuildCellReference(rowView, column.ColumnName, out var cellRef)
                        && context.StyleProvider.TryGetCellStyle(cellRef, out var style))
                    {
                        ApplyCellStyle(cell, style);
                    }
                }

                rowIndex++;
            }

            sheet.SheetView.FreezeRows(1);
            sheet.SheetView.FreezeColumns(1);
            sheet.Columns().AdjustToContents();
            sheet.Rows().AdjustToContents();
        }

        private static void ExportStatisticSheet(IXLWorksheet sheet, ScheduleExportContext context)
        {
            var totalColumns = Math.Max(6, 2 + context.SummaryDayHeaders.Count * 3);
            var currentRow = 1;

            var titleCell = sheet.Cell(currentRow, 1);
            titleCell.Value = "Schedule Statistic";
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontSize = 16;
            titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            sheet.Range(currentRow, 1, currentRow, totalColumns).Merge();

            currentRow += 2;

            currentRow = WriteLabelValue(sheet, currentRow, "Name", context.ScheduleName);
            currentRow = WriteLabelValue(sheet, currentRow, "Month", context.ScheduleMonth.ToString("D2", CultureInfo.InvariantCulture));
            currentRow = WriteLabelValue(sheet, currentRow, "Year", context.ScheduleYear.ToString(CultureInfo.InvariantCulture));
            currentRow = WriteLabelValue(sheet, currentRow, "Shop Name", context.ShopName);
            currentRow = WriteLabelValue(sheet, currentRow, "Shop Address", context.ShopAddress);
            currentRow = WriteLabelValue(sheet, currentRow, "Total Hours", context.TotalHoursText);
            currentRow = WriteLabelValue(sheet, currentRow, "Total Employees", context.TotalEmployees.ToString(CultureInfo.InvariantCulture));
            currentRow = WriteLabelValue(sheet, currentRow, "Total Days", context.TotalDays.ToString(CultureInfo.InvariantCulture));
            currentRow = WriteLabelValue(sheet, currentRow, "Shift 1", context.Shift1);
            currentRow = WriteLabelValue(sheet, currentRow, "Shift 2", context.Shift2);

            currentRow += 1;
            var employeeListCell = sheet.Cell(currentRow, 1);
            employeeListCell.Value = $"Total employees ({context.TotalEmployees}): {context.TotalEmployeesListText}";
            employeeListCell.Style.Font.FontSize = 12;
            employeeListCell.Style.Alignment.WrapText = true;
            sheet.Range(currentRow, 1, currentRow, totalColumns).Merge();

            currentRow += 2;

            if (context.SummaryDayHeaders.Count > 0)
            {
                var summaryStartRow = currentRow;
                var employeeColumn = 1;
                var sumColumn = 2;
                var dayStartColumn = 3;

                ApplyHeaderCellStyle(sheet.Cell(summaryStartRow, employeeColumn));
                ApplyHeaderCellStyle(sheet.Cell(summaryStartRow, sumColumn));

                var dayColumn = dayStartColumn;
                foreach (var dayHeader in context.SummaryDayHeaders)
                {
                    var range = sheet.Range(summaryStartRow, dayColumn, summaryStartRow, dayColumn + 2);
                    range.Merge();
                    range.Value = dayHeader.Text;
                    ApplyHeaderRangeStyle(range);
                    dayColumn += 3;
                }

                var subHeaderRow = summaryStartRow + 1;
                ApplyHeaderCellStyle(sheet.Cell(subHeaderRow, employeeColumn), "Employee");
                ApplyHeaderCellStyle(sheet.Cell(subHeaderRow, sumColumn), "Sum");

                dayColumn = dayStartColumn;
                foreach (var _ in context.SummaryDayHeaders)
                {
                    ApplyHeaderCellStyle(sheet.Cell(subHeaderRow, dayColumn), "From");
                    ApplyHeaderCellStyle(sheet.Cell(subHeaderRow, dayColumn + 1), "To");
                    ApplyHeaderCellStyle(sheet.Cell(subHeaderRow, dayColumn + 2), "Hours");
                    dayColumn += 3;
                }

                var bodyRow = subHeaderRow + 1;
                foreach (var summaryRow in context.SummaryRows)
                {
                    ApplyBodyCellStyle(sheet.Cell(bodyRow, employeeColumn), summaryRow.Employee, XLAlignmentHorizontalValues.Left);
                    ApplyBodyCellStyle(sheet.Cell(bodyRow, sumColumn), summaryRow.Sum, XLAlignmentHorizontalValues.Center);

                    dayColumn = dayStartColumn;
                    var dayCells = summaryRow.Days ?? new List<ContainerScheduleProfileViewModel.SummaryDayCell>();
                    for (int i = 0; i < context.SummaryDayHeaders.Count; i++)
                    {
                        var dayCell = i < dayCells.Count ? dayCells[i] : null;
                        ApplyBodyCellStyle(sheet.Cell(bodyRow, dayColumn), dayCell?.From ?? string.Empty, XLAlignmentHorizontalValues.Center);
                        ApplyBodyCellStyle(sheet.Cell(bodyRow, dayColumn + 1), dayCell?.To ?? string.Empty, XLAlignmentHorizontalValues.Center);
                        ApplyBodyCellStyle(sheet.Cell(bodyRow, dayColumn + 2), dayCell?.Hours ?? string.Empty, XLAlignmentHorizontalValues.Center);
                        dayColumn += 3;
                    }

                    bodyRow++;
                }

                currentRow = bodyRow + 2;
            }

            if (context.EmployeeWorkFreeStats.Count > 0)
            {
                var headerRow = currentRow;
                ApplyHeaderCellStyle(sheet.Cell(headerRow, 1), "Employee");
                ApplyHeaderCellStyle(sheet.Cell(headerRow, 2), "Work Day");
                ApplyHeaderCellStyle(sheet.Cell(headerRow, 3), "Free Day");

                var row = headerRow + 1;
                foreach (var stat in context.EmployeeWorkFreeStats)
                {
                    ApplyBodyCellStyle(sheet.Cell(row, 1), stat.Employee, XLAlignmentHorizontalValues.Left);
                    ApplyBodyCellStyle(sheet.Cell(row, 2), stat.WorkDays.ToString(CultureInfo.InvariantCulture), XLAlignmentHorizontalValues.Center);
                    ApplyBodyCellStyle(sheet.Cell(row, 3), stat.FreeDays.ToString(CultureInfo.InvariantCulture), XLAlignmentHorizontalValues.Center);
                    row++;
                }
            }

            sheet.Columns().AdjustToContents();
            sheet.Rows().AdjustToContents();
        }

        private static int WriteLabelValue(IXLWorksheet sheet, int row, string label, string value)
        {
            var labelCell = sheet.Cell(row, 1);
            labelCell.Value = $"{label}:";
            labelCell.Style.Font.Bold = true;
            labelCell.Style.Font.FontSize = 12;
            labelCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            var valueCell = sheet.Cell(row, 2);
            valueCell.Value = value ?? string.Empty;
            valueCell.Style.Font.FontSize = 12;
            valueCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            valueCell.Style.Alignment.WrapText = true;

            return row + 1;
        }

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
            if (value is null)
                return "NULL";

            if (value is string s)
                return $"'{EscapeSqlString(s)}'";

            if (value is bool b)
                return b ? "1" : "0";

            if (value is DateTime dt)
                return $"'{dt:yyyy-MM-dd HH:mm:ss}'";

            if (value is Enum)
                return $"'{value}'";

            if (value is IFormattable formattable)
                return formattable.ToString(null, CultureInfo.InvariantCulture);

            return $"'{EscapeSqlString(value.ToString() ?? string.Empty)}'";
        }

        private static string EscapeSqlString(string value)
            => value.Replace("'", "''");

        private static string GetCellText(object? value)
        {
            if (value is null || value == DBNull.Value)
                return string.Empty;

            return value.ToString() ?? string.Empty;
        }

        private static bool GetBoolValue(DataRowView rowView, string columnName)
        {
            if (!rowView.Row.Table.Columns.Contains(columnName))
                return false;

            var value = rowView[columnName];
            if (value is bool b)
                return b;

            if (value is null || value == DBNull.Value)
                return false;

            if (bool.TryParse(value.ToString(), out var parsed))
                return parsed;

            return false;
        }

        private static void ApplyHeaderCellStyle(IXLCell cell, string? text = null)
        {
            if (text != null)
                cell.Value = text;

            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 12;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
            ApplyBorder(cell.Style.Border);
        }

        private static void ApplyHeaderRangeStyle(IXLRange range)
        {
            range.Style.Font.Bold = true;
            range.Style.Font.FontSize = 12;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            range.Style.Alignment.WrapText = true;
            range.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
            ApplyBorder(range.Style.Border);
        }

        private static void ApplyBodyCellStyle(IXLCell cell, string text, XLAlignmentHorizontalValues align)
        {
            cell.Value = text ?? string.Empty;
            cell.Style.Alignment.Horizontal = align;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText = true;
            ApplyBorder(cell.Style.Border);
        }

        private static void ApplyMatrixBodyStyle(IXLCell cell, bool isDayColumn)
        {
            cell.Style.Alignment.Horizontal = isDayColumn
                ? XLAlignmentHorizontalValues.Left
                : XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText = true;
            ApplyBorder(cell.Style.Border);
        }

        private static void ApplyWeekendStyle(IXLCell cell)
        {
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EDEDED");
        }

        private static void ApplyConflictStyle(IXLCell cell)
        {
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8D7DA");
        }

        private static void ApplyCellStyle(IXLCell cell, ScheduleCellStyleModel style)
        {
            if (style.BackgroundColorArgb.HasValue && style.BackgroundColorArgb.Value != 0)
            {
                var bg = ColorHelpers.FromArgb(style.BackgroundColorArgb.Value);
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(bg.A, bg.R, bg.G, bg.B);
            }

            if (style.TextColorArgb.HasValue && style.TextColorArgb.Value != 0)
            {
                var fg = ColorHelpers.FromArgb(style.TextColorArgb.Value);
                cell.Style.Font.FontColor = XLColor.FromArgb(fg.A, fg.R, fg.G, fg.B);
            }
        }

        private static void ApplyBorder(IXLBorder border)
        {
            border.TopBorder = XLBorderStyleValues.Thin;
            border.BottomBorder = XLBorderStyleValues.Thin;
            border.LeftBorder = XLBorderStyleValues.Thin;
            border.RightBorder = XLBorderStyleValues.Thin;
        }
    }
}
