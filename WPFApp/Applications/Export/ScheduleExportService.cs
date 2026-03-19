/*
  Опис файлу: цей модуль містить реалізацію компонента ScheduleExportService у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using ClosedXML.Excel;
using BusinessLogicLayer.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WPFApp.ViewModel.Container.ScheduleEdit.Helpers;
using WPFApp.ViewModel.Container.Profile;
using WPFApp.ViewModel.Container.ScheduleProfile;
using WPFApp.Applications.Matrix.Schedule;

namespace WPFApp.Applications.Export
{
    #region DTO / CONTEXT OBJECTS (дані для експорту)

    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class ScheduleExportContext` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ScheduleExportContext
    {
        /// <summary>
        /// Визначає публічний елемент `public string ScheduleName { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ScheduleName { get; }
        /// <summary>
        /// Визначає публічний елемент `public int ScheduleMonth { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int ScheduleMonth { get; }
        /// <summary>
        /// Визначає публічний елемент `public int ScheduleYear { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int ScheduleYear { get; }
        /// <summary>
        /// Визначає публічний елемент `public string ShopName { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ShopName { get; }
        /// <summary>
        /// Визначає публічний елемент `public string ShopAddress { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ShopAddress { get; }
        /// <summary>
        /// Визначає публічний елемент `public string TotalHoursText { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string TotalHoursText { get; }
        /// <summary>
        /// Визначає публічний елемент `public int TotalEmployees { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int TotalEmployees { get; }
        /// <summary>
        /// Визначає публічний елемент `public int TotalDays { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int TotalDays { get; }
        /// <summary>
        /// Визначає публічний елемент `public string Shift1 { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Shift1 { get; }
        /// <summary>
        /// Визначає публічний елемент `public string Shift2 { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Shift2 { get; }
        /// <summary>
        /// Визначає публічний елемент `public string TotalEmployeesListText { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string TotalEmployeesListText { get; }

        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public DataView ScheduleMatrix { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public DataView ScheduleMatrix { get; }

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public IReadOnlyList<ContainerScheduleProfileViewModel.SummaryDayHeader> SummaryDayHeaders { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public IReadOnlyList<ContainerScheduleProfileViewModel.SummaryDayHeader> SummaryDayHeaders { get; }

        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public IReadOnlyList<ContainerScheduleProfileViewModel.SummaryEmployeeRow> SummaryRows { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public IReadOnlyList<ContainerScheduleProfileViewModel.SummaryEmployeeRow> SummaryRows { get; }

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public IReadOnlyList<ContainerScheduleProfileViewModel.EmployeeWorkFreeStatRow> EmployeeWorkFreeStats { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public IReadOnlyList<ContainerScheduleProfileViewModel.EmployeeWorkFreeStatRow> EmployeeWorkFreeStats { get; }

        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public IScheduleMatrixStyleProvider StyleProvider { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public IScheduleMatrixStyleProvider StyleProvider { get; }

        /// <summary>
        /// Визначає публічний елемент `public ScheduleExportContext(` та контракт його використання у шарі WPFApp.
        /// </summary>
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

    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class AvailabilityGroupExportData` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class AvailabilityGroupExportData
    {
        /// <summary>
        /// Визначає публічний елемент `public AvailabilityGroupModel Group { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AvailabilityGroupModel Group { get; }
        /// <summary>
        /// Визначає публічний елемент `public IReadOnlyList<AvailabilityGroupMemberModel> Members { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public IReadOnlyList<AvailabilityGroupMemberModel> Members { get; }
        /// <summary>
        /// Визначає публічний елемент `public IReadOnlyList<AvailabilityGroupDayModel> Days { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public IReadOnlyList<AvailabilityGroupDayModel> Days { get; }

        /// <summary>
        /// Визначає публічний елемент `public AvailabilityGroupExportData(` та контракт його використання у шарі WPFApp.
        /// </summary>
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

    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class ContainerExcelExportChartContext` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ContainerExcelExportChartContext
    {
        /// <summary>
        /// Визначає публічний елемент `public string ChartName { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ChartName { get; }
        /// <summary>
        /// Визначає публічний елемент `public ScheduleExportContext ScheduleContext { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ScheduleExportContext ScheduleContext { get; }

        /// <summary>
        /// Визначає публічний елемент `public ContainerExcelExportChartContext(string chartName, ScheduleExportContext scheduleContext)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ContainerExcelExportChartContext(string chartName, ScheduleExportContext scheduleContext)
        {
            ChartName = chartName ?? string.Empty;
            ScheduleContext = scheduleContext ?? throw new ArgumentNullException(nameof(scheduleContext));
        }
    }

    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class ContainerExcelExportContext` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ContainerExcelExportContext
    {
        /// <summary>
        /// Визначає публічний елемент `public int ContainerId { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int ContainerId { get; }

        /// <summary>
        /// Визначає публічний елемент `public string ContainerName { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ContainerName { get; }
        /// <summary>
        /// Визначає публічний елемент `public string ContainerNote { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ContainerNote { get; }
        /// <summary>
        /// Визначає публічний елемент `public int TotalEmployees { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int TotalEmployees { get; }
        /// <summary>
        /// Визначає публічний елемент `public int TotalShops { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int TotalShops { get; }
        /// <summary>
        /// Визначає публічний елемент `public string TotalEmployeesListText { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string TotalEmployeesListText { get; }
        /// <summary>
        /// Визначає публічний елемент `public string TotalShopsListText { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string TotalShopsListText { get; }
        /// <summary>
        /// Визначає публічний елемент `public string TotalHoursText { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string TotalHoursText { get; }
        /// <summary>
        /// Визначає публічний елемент `public IReadOnlyList<ContainerProfileViewModel.ShopHeader> ShopHeaders { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public IReadOnlyList<ContainerProfileViewModel.ShopHeader> ShopHeaders { get; }
        /// <summary>
        /// Визначає публічний елемент `public IReadOnlyList<ContainerProfileViewModel.EmployeeShopHoursRow> EmployeeShopHoursRows { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public IReadOnlyList<ContainerProfileViewModel.EmployeeShopHoursRow> EmployeeShopHoursRows { get; }
        /// <summary>
        /// Визначає публічний елемент `public IReadOnlyList<ContainerProfileViewModel.EmployeeWorkFreeStatRow> EmployeeWorkFreeStats { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public IReadOnlyList<ContainerProfileViewModel.EmployeeWorkFreeStatRow> EmployeeWorkFreeStats { get; }
        /// <summary>
        /// Визначає публічний елемент `public IReadOnlyList<ContainerExcelExportChartContext> Charts { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public IReadOnlyList<ContainerExcelExportChartContext> Charts { get; }

        /// <summary>
        /// Визначає публічний елемент `public ContainerExcelExportContext(` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ContainerExcelExportContext(
            int containerId,
            string containerName,
            string containerNote,
            int totalEmployees,
            int totalShops,
            string totalEmployeesListText,
            string totalShopsListText,
            string totalHoursText,
            IReadOnlyList<ContainerProfileViewModel.ShopHeader> shopHeaders,
            IReadOnlyList<ContainerProfileViewModel.EmployeeShopHoursRow> employeeShopHoursRows,
            IReadOnlyList<ContainerProfileViewModel.EmployeeWorkFreeStatRow> employeeWorkFreeStats,
            IReadOnlyList<ContainerExcelExportChartContext> charts)
        {
            ContainerId = containerId;
            ContainerName = containerName ?? string.Empty;
            ContainerNote = containerNote ?? string.Empty;
            TotalEmployees = totalEmployees;
            TotalShops = totalShops;
            TotalEmployeesListText = totalEmployeesListText ?? string.Empty;
            TotalShopsListText = totalShopsListText ?? string.Empty;
            TotalHoursText = totalHoursText ?? string.Empty;
            ShopHeaders = shopHeaders ?? Array.Empty<ContainerProfileViewModel.ShopHeader>();
            EmployeeShopHoursRows = employeeShopHoursRows ?? Array.Empty<ContainerProfileViewModel.EmployeeShopHoursRow>();
            EmployeeWorkFreeStats = employeeWorkFreeStats ?? Array.Empty<ContainerProfileViewModel.EmployeeWorkFreeStatRow>();
            Charts = charts ?? Array.Empty<ContainerExcelExportChartContext>();
        }
    }

    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class ContainerSqlExportScheduleContext` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ContainerSqlExportScheduleContext
    {
        /// <summary>
        /// Визначає публічний елемент `public ScheduleSqlExportContext Schedule { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ScheduleSqlExportContext Schedule { get; }

        /// <summary>
        /// Визначає публічний елемент `public ContainerSqlExportScheduleContext(ScheduleSqlExportContext schedule)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ContainerSqlExportScheduleContext(ScheduleSqlExportContext schedule)
        {
            Schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
        }
    }

    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class ContainerSqlExportContext` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ContainerSqlExportContext
    {
        /// <summary>
        /// Визначає публічний елемент `public ContainerModel Container { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ContainerModel Container { get; }
        /// <summary>
        /// Визначає публічний елемент `public IReadOnlyList<ContainerSqlExportScheduleContext> Charts { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public IReadOnlyList<ContainerSqlExportScheduleContext> Charts { get; }

        /// <summary>
        /// Визначає публічний елемент `public ContainerSqlExportContext(ContainerModel container, IReadOnlyList<ContainerSqlExportScheduleContext> charts)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ContainerSqlExportContext(ContainerModel container, IReadOnlyList<ContainerSqlExportScheduleContext> charts)
        {
            Container = container ?? throw new ArgumentNullException(nameof(container));
            Charts = charts ?? Array.Empty<ContainerSqlExportScheduleContext>();
        }
    }

    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class ScheduleSqlExportContext` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ScheduleSqlExportContext
    {
        /// <summary>
        /// Визначає публічний елемент `public ScheduleModel Schedule { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ScheduleModel Schedule { get; }
        /// <summary>
        /// Визначає публічний елемент `public IReadOnlyList<ScheduleEmployeeModel> Employees { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public IReadOnlyList<ScheduleEmployeeModel> Employees { get; }
        /// <summary>
        /// Визначає публічний елемент `public IReadOnlyList<ScheduleSlotModel> Slots { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public IReadOnlyList<ScheduleSlotModel> Slots { get; }
        /// <summary>
        /// Визначає публічний елемент `public IReadOnlyList<ScheduleCellStyleModel> CellStyles { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public IReadOnlyList<ScheduleCellStyleModel> CellStyles { get; }
        /// <summary>
        /// Визначає публічний елемент `public AvailabilityGroupExportData? AvailabilityGroupData { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AvailabilityGroupExportData? AvailabilityGroupData { get; }

        /// <summary>
        /// Визначає публічний елемент `public ScheduleSqlExportContext(` та контракт його використання у шарі WPFApp.
        /// </summary>
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

    #endregion

    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class ScheduleExportService : IScheduleExportService` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ScheduleExportService : IScheduleExportService
    {
        
        
        
        
        private static readonly string[] ContainerTemplateSheetNames = { "ContainerTemplate", "Sheet1", "Container" };

        
        
        
        private const string DefaultSheetName = "Schedule";
        private const string DefaultStatisticSheetName = "Schedule - Statistic";

        

        
        
        
        
        
        private const string ExcelTemplateRelativePath = @"Resources\Excel\ScheduleTemplate.xlsx";

        
        
        
        private const string ContainerExcelTemplateRelativePath = @"Resources\Excel\ContainerTemplate.xlsx";

        
        
        
        
        private static readonly string[] MatrixTemplateSheetNames = { "ScheduleName", "R_FL_35", "MatrixTemplate" };

        
        
        
        private static readonly string[] StatisticTemplateSheetNames = { "ScheduleStatistic", "Schedule Statistic", "StatisticTemplate" };

        
        
        
        
        
        
        
        
        
        

        private const string MatrixClearRange = "B1:AA32";
        private const int M_ShopRow = 1;
        private const int M_ShopCol = 2;         
        private const int M_HeaderRow = 1;
        private const int M_DateCol = 2;         
        private const int M_FirstDataCol = 3;    
        private const int M_LastDataCol = 27;    
        private const int M_FirstDayRow = 2;
        private const int M_DayCount = 31;

        
        
        
        
        
        

        private const int S_ValueCol = 2;                
        private const int S_EmployeesLineRow = 12;       
        private const int S_DayHeaderRow = 14;           
        private const int S_BodyFirstRow = 16;           
        private const int S_MaxSummaryRows = 10;         

        
        private const int S_EmployeeCol = 1;            
        private const int S_WorkDaysCol = 2;            
        private const int S_FreeDaysCol = 3;            
        private const int S_SumCol = 4;                 

        
        private const int S_FirstDayCol = 5;
        private const int S_DaysCapacity = 31;          

        

        
        
        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public Task ExportToExcelAsync(ScheduleExportContext context, string filePath, CancellationToken ct = default)` та контракт його використання у шарі WPFApp.
        /// </summary>
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

                
                var scheduleName = SanitizeWorksheetName(context.ScheduleName, DefaultSheetName);

                
                var matrixSheet = matrixTemplate.CopyTo(scheduleName);

                
                var statisticNameBase = SanitizeWorksheetName($"{scheduleName} - Statistic", DefaultStatisticSheetName);
                var statisticName = MakeUniqueSheetName(workbook, statisticNameBase);
                var statSheet = statTemplate.CopyTo(statisticName);

                
                matrixTemplate.Delete();
                statTemplate.Delete();

                
                FillMatrixSheetFromTemplate(matrixSheet, context);
                FillStatisticSheetFromTemplate(statSheet, context);

                
                FixGray125Fills(matrixSheet, "B2:AA32");
                FixGray125Fills(statSheet);

                
                workbook.SaveAs(filePath);
            }, ct);
        }

        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public async Task ExportToSqlAsync(ScheduleSqlExportContext context, string filePath, CancellationToken ct = default)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public async Task ExportToSqlAsync(ScheduleSqlExportContext context, string filePath, CancellationToken ct = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));

            var script = BuildSqlScript(context);
            await File.WriteAllTextAsync(filePath, script, Encoding.UTF8, ct).ConfigureAwait(false);
        }

        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public Task ExportContainerToExcelAsync(ContainerExcelExportContext context, string filePath, CancellationToken ct = def` та контракт його використання у шарі WPFApp.
        /// </summary>
        public Task ExportContainerToExcelAsync(ContainerExcelExportContext context, string filePath, CancellationToken ct = default)
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

                
                var containerTemplatePath = ResolveContainerTemplatePath();
                using var containerWb = new XLWorkbook(containerTemplatePath);

                var containerSrc = FindTemplateSheet(containerWb, ContainerTemplateSheetNames)
                    ?? throw new InvalidOperationException($"Container template sheet not found in ContainerTemplate.xlsx. Expected one of: {string.Join(", ", ContainerTemplateSheetNames)}");

                
                var containerTemplateName = MakeUniqueSheetName(workbook, "__ContainerTemplate__");

                
                
                var containerTemplate = containerSrc.CopyTo(workbook, containerTemplateName);

                
                var containerSheetName = MakeUniqueSheetName(workbook, SanitizeWorksheetName("Container", "Container"));
                var containerSheet = containerTemplate.CopyTo(containerSheetName);
                FillContainerTemplateSheetFromTemplate(containerSheet, context);

                
                foreach (var chart in context.Charts)
                {
                    ct.ThrowIfCancellationRequested();

                    
                    var scheduleName = MakeUniqueSheetName(workbook, SanitizeWorksheetName(chart.ChartName, DefaultSheetName));

                    
                    var matrixSheet = matrixTemplate.CopyTo(scheduleName);
                    FillMatrixSheetFromTemplate(matrixSheet, chart.ScheduleContext);
                    FixGray125Fills(matrixSheet, "B2:AA32");

                    
                    var statNameBase = SanitizeWorksheetName($"{scheduleName} - Statistic", DefaultStatisticSheetName);
                    var statName = MakeUniqueSheetName(workbook, statNameBase);
                    var statSheet = statTemplate.CopyTo(statName);
                    FillStatisticSheetFromTemplate(statSheet, chart.ScheduleContext);
                    FixGray125Fills(statSheet);
                }

                
                matrixTemplate.Delete();
                statTemplate.Delete();
                containerTemplate.Delete();

                workbook.SaveAs(filePath);
            }, ct);
        }

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public async Task ExportContainerToSqlAsync(ContainerSqlExportContext context, string filePath, CancellationToken ct = d` та контракт його використання у шарі WPFApp.
        /// </summary>
        public async Task ExportContainerToSqlAsync(ContainerSqlExportContext context, string filePath, CancellationToken ct = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));

            var script = BuildContainerSqlScript(context);
            await File.WriteAllTextAsync(filePath, script, Encoding.UTF8, ct).ConfigureAwait(false);
        }

        

        
        
        
        
        
        private static string ResolveExcelTemplatePath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var path = Path.Combine(baseDir, ExcelTemplateRelativePath);

            if (!File.Exists(path))
                throw new FileNotFoundException("Excel template not found. Ensure it is copied to output directory.", path);

            return path;
        }

        
        
        
        private static string ResolveContainerTemplatePath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var path = Path.Combine(baseDir, ContainerExcelTemplateRelativePath);

            if (!File.Exists(path))
                throw new FileNotFoundException("Container Excel template not found.", path);

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

        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static string SanitizeFileName(string? name, string fallback)` та контракт його використання у шарі WPFApp.
        /// </summary>
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

        
        
        
        
        
        
        
        
        
        
        
        
        private static void FixGray125Fills(IXLWorksheet sheet, string? rangeAddress = null)
        {
            if (sheet is null) return;

            
            var range = !string.IsNullOrWhiteSpace(rangeAddress) ? sheet.Range(rangeAddress) : sheet.RangeUsed();
            if (range != null)
            {
                foreach (var cell in range.Cells())
                {
                    if (cell.Style.Fill.PatternType == XLFillPatternValues.Gray125)
                    {
                        
                        cell.Style.Fill.PatternType = XLFillPatternValues.Gray125;
                        cell.Style.Fill.BackgroundColor = XLColor.White;
                        cell.Style.Fill.PatternColor = XLColor.Black; 
                    }
                }
            }

            
            foreach (var cf in sheet.ConditionalFormats)
            {
                if (cf.Style.Fill.PatternType == XLFillPatternValues.Gray125)
                {
                    cf.Style.Fill.PatternType = XLFillPatternValues.Gray125;
                    cf.Style.Fill.BackgroundColor = XLColor.White;
                    cf.Style.Fill.PatternColor = XLColor.Black;
                }
            }
        }

        

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        private static void FillMatrixSheetFromTemplate(IXLWorksheet sheet, ScheduleExportContext context)
        {
            
            sheet.Range(MatrixClearRange).Clear(XLClearOptions.Contents);

            
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

            
            var capacity = M_LastDataCol - M_FirstDataCol + 1;
            if (dataCols.Count > capacity)
                throw new InvalidOperationException($"Template supports max {capacity} employee columns (C..AA). Current: {dataCols.Count}.");

            
            
            for (int i = 0; i < dataCols.Count; i++)
            {
                var col = dataCols[i];
                sheet.Cell(M_HeaderRow, M_FirstDataCol + i).Value =
                    string.IsNullOrWhiteSpace(col.Caption) ? col.ColumnName : col.Caption;
            }

            
            
            for (int c = M_FirstDataCol + dataCols.Count; c <= M_LastDataCol; c++)
                sheet.Cell(M_HeaderRow, c).Value = "0";

            
            var daysInMonth = DateTime.DaysInMonth(context.ScheduleYear, context.ScheduleMonth);

            
            var maxRows = Math.Min(M_DayCount, context.ScheduleMatrix.Count);

            
            for (int r = 0; r < M_DayCount; r++)
            {
                var excelRow = M_FirstDayRow + r;
                DataRowView? rowView = r < maxRows ? (DataRowView)context.ScheduleMatrix[r] : null;

                
                
                
                
                var dateCell = sheet.Cell(excelRow, M_DateCol);

                if (r < daysInMonth)
                {
                    
                    
                    var date = rowView != null ? TryBuildDate(context, dayCol, rowView) : null;
                    date ??= SafeDate(context.ScheduleYear, context.ScheduleMonth, r + 1);

                    if (date.HasValue)
                        dateCell.Value = date.Value;
                    else
                        dateCell.Value = " ";
                }
                else
                {
                    dateCell.Value = " ";
                }

                
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

                
                for (int c = M_FirstDataCol + dataCols.Count; c <= M_LastDataCol; c++)
                    sheet.Cell(excelRow, c).Value = "-";
            }

            
            
            
            sheet.PageSetup.SetRowsToRepeatAtTop(M_HeaderRow, M_HeaderRow);
            sheet.PageSetup.SetColumnsToRepeatAtLeft(M_DateCol, M_DateCol);

            
            sheet.PageSetup.PrintAreas.Clear();
            sheet.PageSetup.PrintAreas.Add(MatrixClearRange); 
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

        

        
        
        
        
        
        
        
        
        
        
        private static DateTime? TryBuildDate(ScheduleExportContext context, DataColumn? dayCol, DataRowView rowView)
        {
            if (dayCol is null) return null;

            var raw = rowView[dayCol.ColumnName];
            if (raw is null || raw == DBNull.Value) return null;

            
            if (raw is DateTime dt) return dt.Date;

            
            if (raw is int d && d >= 1 && d <= 31)
                return SafeDate(context.ScheduleYear, context.ScheduleMonth, d);

            
            var s = raw.ToString()?.Trim();
            if (int.TryParse(s, out var parsed) && parsed >= 1 && parsed <= 31)
                return SafeDate(context.ScheduleYear, context.ScheduleMonth, parsed);

            
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

            
            
            
            var wfByEmployee = (context.EmployeeWorkFreeStats ?? Array.Empty<ContainerScheduleProfileViewModel.EmployeeWorkFreeStatRow>())
                .Where(x => !string.IsNullOrWhiteSpace(x.Employee))
                .GroupBy(x => x.Employee!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            
            
            var summaryNeeded = context.SummaryRows.Count;
            var summaryExtra = Math.Max(0, summaryNeeded - S_MaxSummaryRows);

            
            
            var summaryLastCol = S_FirstDayCol + (S_DaysCapacity - 1) * 3 + 2;

            if (summaryExtra > 0)
            {
                
                var insertAfter = S_BodyFirstRow + S_MaxSummaryRows - 1;
                sheet.Row(insertAfter).InsertRowsBelow(summaryExtra);

                
                var templateRange = sheet.Range(insertAfter, 1, insertAfter, summaryLastCol);
                var templateHeight = sheet.Row(insertAfter).Height;

                for (int k = 1; k <= summaryExtra; k++)
                {
                    templateRange.CopyTo(sheet.Range(insertAfter + k, 1, insertAfter + k, summaryLastCol));
                    sheet.Row(insertAfter + k).Height = templateHeight;
                }
            }

            
            
            
            
            var summaryTotalRows = Math.Max(S_MaxSummaryRows, summaryNeeded);

            for (int r = 0; r < summaryTotalRows; r++)
            {
                var row = S_BodyFirstRow + r;

                
                if (r >= summaryNeeded)
                {
                    sheet.Cell(row, S_EmployeeCol).Clear(XLClearOptions.Contents);
                    sheet.Cell(row, S_WorkDaysCol).Clear(XLClearOptions.Contents);
                    sheet.Cell(row, S_FreeDaysCol).Clear(XLClearOptions.Contents);
                    sheet.Cell(row, S_SumCol).Clear(XLClearOptions.Contents);

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
                var employeeName = summaryRow.Employee ?? string.Empty;

                
                sheet.Cell(row, S_EmployeeCol).Value = employeeName;

                
                if (wfByEmployee.TryGetValue(employeeName, out var wf))
                {
                    sheet.Cell(row, S_WorkDaysCol).Value = wf.WorkDays.ToString(CultureInfo.InvariantCulture);
                    sheet.Cell(row, S_FreeDaysCol).Value = wf.FreeDays.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    
                    sheet.Cell(row, S_WorkDaysCol).Clear(XLClearOptions.Contents);
                    sheet.Cell(row, S_FreeDaysCol).Clear(XLClearOptions.Contents);
                }

                
                sheet.Cell(row, S_SumCol).Value = summaryRow.Sum ?? string.Empty;

                
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

            
            
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        private static void FillContainerTemplateSheetFromTemplate(IXLWorksheet sheet, ContainerExcelExportContext context)
        {
            
            
            var map = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["{Id}"] = context.ContainerId.ToString(CultureInfo.InvariantCulture),
                ["{Name}"] = context.ContainerName ?? string.Empty,
                ["{Note}"] = context.ContainerNote ?? string.Empty,

                ["{Container Name}"] = context.ContainerName ?? string.Empty,
                ["{Total Hours}"] = context.TotalHoursText ?? string.Empty,
                ["{Total Employees Count}"] = context.TotalEmployees.ToString(CultureInfo.InvariantCulture),
                ["{Total Shops Count}"] = context.TotalShops.ToString(CultureInfo.InvariantCulture),
                ["{Total Employees}"] = context.TotalEmployeesListText ?? string.Empty,
                ["{Total Shops}"] = context.TotalShopsListText ?? string.Empty,
            };

            
            
            foreach (var cell in sheet.CellsUsed())
            {
                var s = cell.GetString();
                if (string.IsNullOrWhiteSpace(s) || !s.Contains('{')) continue;

                foreach (var kv in map)
                    if (s.Contains(kv.Key, StringComparison.Ordinal))
                        s = s.Replace(kv.Key, kv.Value);

                cell.Value = s;
            }

            
            
            var headerRow = sheet.RowsUsed()
                .Select(r => r.RowNumber())
                .FirstOrDefault(r =>
                    string.Equals(sheet.Cell(r, 1).GetString(), "Employee", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(sheet.Cell(r, 4).GetString(), "Hours Sum", StringComparison.OrdinalIgnoreCase));

            if (headerRow <= 0) return;

            
            
            var templateRow = sheet.RowsUsed()
                .Select(r => r.RowNumber())
                .FirstOrDefault(r => sheet.Cell(r, 1).GetString().Contains("{Employee}", StringComparison.OrdinalIgnoreCase));

            if (templateRow <= 0) templateRow = headerRow + 1;

            
            
            int firstShopCol = 0, lastShopCol = 0;
            var lastUsedCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 50;

            for (int c = 1; c <= lastUsedCol; c++)
            {
                if (sheet.Cell(headerRow, c).GetString().Contains("{Shop}", StringComparison.OrdinalIgnoreCase))
                {
                    firstShopCol = c;
                    break;
                }
            }
            if (firstShopCol <= 0) return;

            
            lastShopCol = firstShopCol;
            while (lastShopCol <= lastUsedCol &&
                   sheet.Cell(headerRow, lastShopCol).GetString().Contains("{Shop}", StringComparison.OrdinalIgnoreCase))
                lastShopCol++;
            lastShopCol--;

            var shopCapacity = lastShopCol - firstShopCol + 1;
            var shops = context.ShopHeaders?.ToList() ?? new List<ContainerProfileViewModel.ShopHeader>();

            
            if (shops.Count > shopCapacity)
                throw new InvalidOperationException($"ContainerTemplate supports max {shopCapacity} shops. Add more {{Shop}} columns to the template.");

            
            
            for (int i = 0; i < shopCapacity; i++)
            {
                sheet.Cell(headerRow, firstShopCol + i).Value = i < shops.Count ? shops[i].Name : string.Empty;
            }

            
            var rows = context.EmployeeShopHoursRows?.ToList() ?? new List<ContainerProfileViewModel.EmployeeShopHoursRow>();

            
            
            if (rows.Count > 1)
            {
                rows = rows
                    .OrderBy(x => string.Equals(x.Employee, "TOTAL", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                    .ToList();
            }

            
            if (rows.Count == 0)
            {
                sheet.Range(templateRow, 1, templateRow, lastShopCol).Clear(XLClearOptions.Contents);
                return;
            }

            
            
            if (rows.Count > 1)
            {
                sheet.Row(templateRow).InsertRowsBelow(rows.Count - 1);

                var templateRange = sheet.Range(templateRow, 1, templateRow, lastShopCol);
                for (int i = 1; i < rows.Count; i++)
                    templateRange.CopyTo(sheet.Range(templateRow + i, 1, templateRow + i, lastShopCol));
            }

            
            for (int i = 0; i < rows.Count; i++)
            {
                var r = templateRow + i;
                var item = rows[i];

                
                sheet.Cell(r, 1).Value = item.Employee ?? string.Empty;

                
                
                sheet.Cell(r, 2).Value = item.WorkDays;
                sheet.Cell(r, 3).Value = item.FreeDays;

                
                sheet.Cell(r, 4).Value = item.HoursSum ?? "0";

                
                for (int s = 0; s < shopCapacity; s++)
                {
                    if (s >= shops.Count)
                    {
                        
                        sheet.Cell(r, firstShopCol + s).Value = string.Empty;
                        continue;
                    }

                    var shopKey = shops[s].Key;

                    
                    item.HoursByShop.TryGetValue(shopKey, out var val);

                    
                    sheet.Cell(r, firstShopCol + s).Value = string.IsNullOrWhiteSpace(val) ? "0" : val;
                }
            }

            
            var lastDataRow = templateRow + rows.Count - 1;

            
            
            var endCol = firstShopCol + Math.Max(shops.Count, 1) - 1;
            var tableRange = sheet.Range(headerRow, 1, lastDataRow, endCol);

            
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            
            
            
            var totalIndex = rows.FindIndex(x => string.Equals(x.Employee, "TOTAL", StringComparison.OrdinalIgnoreCase));
            if (totalIndex >= 0)
            {
                var totalRow = templateRow + totalIndex;

                
                sheet.Row(totalRow).Style.Font.Bold = true;

                
                tableRange.Worksheet.Range(totalRow, 1, totalRow, tableRange.LastColumn().ColumnNumber())
                    .Style.Border.TopBorder = XLBorderStyleValues.Medium; 
            }
        }

        

        
        
        
        
        
        
        
        
        
        
        
        private static string BuildContainerSqlScript(ContainerSqlExportContext context)
        {
            var sb = new StringBuilder(8192);
            sb.AppendLine("-- GF3 Container export");
            sb.AppendLine($"-- Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("BEGIN TRANSACTION;");
            sb.AppendLine();

            var container = context.Container;

            
            sb.AppendLine(SqlInsert("container",
                ("id", container.Id),
                ("name", container.Name),
                ("note", container.Note)));

            
            var shops = new Dictionary<int, ShopModel>();
            var employees = new Dictionary<int, EmployeeModel>();
            var availabilityGroups = new Dictionary<int, AvailabilityGroupModel>();
            var availabilityMembers = new Dictionary<int, AvailabilityGroupMemberModel>();
            var availabilityDays = new Dictionary<int, AvailabilityGroupDayModel>();
            var schedules = new List<ScheduleModel>();
            var scheduleEmployees = new List<ScheduleEmployeeModel>();
            var slots = new List<ScheduleSlotModel>();
            var styles = new List<ScheduleCellStyleModel>();

            
            foreach (var chart in context.Charts)
            {
                var scheduleCtx = chart.Schedule;
                var schedule = scheduleCtx.Schedule;
                schedules.Add(schedule);

                
                if (schedule.Shop != null && !shops.ContainsKey(schedule.Shop.Id))
                    shops.Add(schedule.Shop.Id, schedule.Shop);

                
                foreach (var employee in CollectEmployees(scheduleCtx))
                {
                    if (!employees.ContainsKey(employee.Id))
                        employees.Add(employee.Id, employee);
                }

                
                if (scheduleCtx.AvailabilityGroupData is not null)
                {
                    var grp = scheduleCtx.AvailabilityGroupData.Group;
                    if (!availabilityGroups.ContainsKey(grp.Id))
                        availabilityGroups.Add(grp.Id, grp);

                    foreach (var member in scheduleCtx.AvailabilityGroupData.Members)
                    {
                        if (!availabilityMembers.ContainsKey(member.Id))
                            availabilityMembers.Add(member.Id, member);
                    }

                    foreach (var day in scheduleCtx.AvailabilityGroupData.Days)
                    {
                        if (!availabilityDays.ContainsKey(day.Id))
                            availabilityDays.Add(day.Id, day);
                    }
                }

                
                scheduleEmployees.AddRange(scheduleCtx.Employees ?? Array.Empty<ScheduleEmployeeModel>());
                slots.AddRange(scheduleCtx.Slots ?? Array.Empty<ScheduleSlotModel>());
                styles.AddRange(scheduleCtx.CellStyles ?? Array.Empty<ScheduleCellStyleModel>());
            }

            
            foreach (var shop in shops.Values.OrderBy(s => s.Id))
                sb.AppendLine(SqlInsert("shop", ("id", shop.Id), ("name", shop.Name), ("address", shop.Address), ("description", shop.Description)));

            foreach (var employee in employees.Values.OrderBy(e => e.Id))
                sb.AppendLine(SqlInsert("employee", ("id", employee.Id), ("first_name", employee.FirstName), ("last_name", employee.LastName), ("phone", employee.Phone), ("email", employee.Email)));

            foreach (var group in availabilityGroups.Values.OrderBy(g => g.Id))
                sb.AppendLine(SqlInsert("availability_group", ("id", group.Id), ("name", group.Name), ("year", group.Year), ("month", group.Month)));

            foreach (var member in availabilityMembers.Values.OrderBy(m => m.Id))
                sb.AppendLine(SqlInsert("availability_group_member", ("id", member.Id), ("availability_group_id", member.AvailabilityGroupId), ("employee_id", member.EmployeeId)));

            foreach (var day in availabilityDays.Values.OrderBy(d => d.Id))
                sb.AppendLine(SqlInsert("availability_group_day", ("id", day.Id), ("availability_group_member_id", day.AvailabilityGroupMemberId), ("day_of_month", day.DayOfMonth), ("kind", day.Kind.ToString()), ("interval_str", day.IntervalStr)));

            
            foreach (var schedule in schedules.OrderBy(s => s.Id))
                sb.AppendLine(SqlInsert("schedule", ("id", schedule.Id), ("container_id", schedule.ContainerId), ("shop_id", schedule.ShopId), ("name", schedule.Name), ("year", schedule.Year), ("month", schedule.Month), ("people_per_shift", schedule.PeoplePerShift), ("shift1_time", schedule.Shift1Time), ("shift2_time", schedule.Shift2Time), ("max_hours_per_emp_month", schedule.MaxHoursPerEmpMonth), ("max_consecutive_days", schedule.MaxConsecutiveDays), ("max_consecutive_full", schedule.MaxConsecutiveFull), ("max_full_per_month", schedule.MaxFullPerMonth), ("note", schedule.Note), ("availability_group_id", schedule.AvailabilityGroupId)));

            
            foreach (var se in scheduleEmployees.OrderBy(e => e.Id))
                sb.AppendLine(SqlInsert("schedule_employee", ("id", se.Id), ("schedule_id", se.ScheduleId), ("employee_id", se.EmployeeId), ("min_hours_month", se.MinHoursMonth)));

            
            foreach (var slot in slots.OrderBy(s => s.Id))
                sb.AppendLine(SqlInsert("schedule_slot", ("id", slot.Id), ("schedule_id", slot.ScheduleId), ("day_of_month", slot.DayOfMonth), ("slot_no", slot.SlotNo), ("employee_id", slot.EmployeeId), ("status", slot.Status.ToString()), ("from_time", slot.FromTime), ("to_time", slot.ToTime)));

            
            foreach (var style in styles.OrderBy(s => s.Id))
                sb.AppendLine(SqlInsert("schedule_cell_style", ("id", style.Id), ("schedule_id", style.ScheduleId), ("day_of_month", style.DayOfMonth), ("employee_id", style.EmployeeId), ("background_color_argb", style.BackgroundColorArgb), ("text_color_argb", style.TextColorArgb)));

            sb.AppendLine();
            sb.AppendLine("COMMIT;");
            return sb.ToString();
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

            return $"INSERT OR IGNORE INTO {table} ({columns}) VALUES ({vals});";
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