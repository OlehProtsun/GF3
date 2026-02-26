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
    /// Контекст експорту ОДНОГО розкладу (schedule) у Excel.
    ///
    /// Ідея: сервіс експорту НЕ повинен лазити у ViewModel/DB напряму — йому передають
    /// "готовий пакет даних" у вигляді цього контексту.
    ///
    /// Вміст:
    /// - Метадані розкладу (назва, місяць/рік, магазин, адреса, зміни, підсумки)
    /// - Матриця розкладу (DataView) — таблиця "День x Працівник" (або навпаки)
    /// - Summary дані для статистичного листа (заголовки днів, рядки працівників, work/free stats)
    /// - StyleProvider — постачальник стилів (поки тут зберігається як dependency)
    ///
    /// Цей клас — immutable-подібний: тільки get; і ініціалізація в конструкторі.
    /// Це зменшує ризик "підміни" даних під час виконання експорту.
    /// </summary>
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

        /// <summary>
        /// Матриця розкладу у вигляді DataView (джерело даних для "Matrix" листа).
        /// Очікується, що DataView містить колонки:
        /// - Day (або іншу колонку дати/дня)
        /// - колонки працівників/слотів
        /// Також можуть бути службові колонки типу Conflict/Weekend — вони відфільтровуються.
        /// </summary>
        public DataView ScheduleMatrix { get; }

        /// <summary>
        /// Заголовки днів для статистичного листа (наприклад: "Mon 01", "Tue 02"...).
        /// Важливо: у шаблоні кожен день займає 3 колонки (From/To/Hours).
        /// </summary>
        public IReadOnlyList<ContainerScheduleProfileViewModel.SummaryDayHeader> SummaryDayHeaders { get; }

        /// <summary>
        /// Рядки "Summary" по працівниках:
        /// - Employee (ім'я)
        /// - Sum (сума годин/інший підсумок)
        /// - Days (масив із клітинок From/To/Hours по кожному дню)
        /// </summary>
        public IReadOnlyList<ContainerScheduleProfileViewModel.SummaryEmployeeRow> SummaryRows { get; }

        /// <summary>
        /// Додаткова статистика: скільки робочих/вільних днів у працівника.
        /// Використовується для заповнення колонок WorkDays / FreeDays у статистичному листі.
        /// </summary>
        public IReadOnlyList<ContainerScheduleProfileViewModel.EmployeeWorkFreeStatRow> EmployeeWorkFreeStats { get; }

        /// <summary>
        /// Провайдер стилів для матриці (наприклад: кольори/заливки/текстові стилі).
        /// У цьому файлі він не застосовується напряму у FillMatrixSheetFromTemplate (бо тут
        /// принцип "не чіпати стилі шаблону"), але dependency залишили для сумісності/розширень.
        /// </summary>
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
            // Нормалізація null -> "", щоб код експорту не робив постійно null-checks.
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

            // Навіть якщо scheduleMatrix не передали — даємо пустий DataView, щоб уникнути NRE.
            ScheduleMatrix = scheduleMatrix ?? new DataView();

            // Для колекцій теж робимо safe fallback: порожні масиви.
            SummaryDayHeaders = summaryDayHeaders ?? Array.Empty<ContainerScheduleProfileViewModel.SummaryDayHeader>();
            SummaryRows = summaryRows ?? Array.Empty<ContainerScheduleProfileViewModel.SummaryEmployeeRow>();
            EmployeeWorkFreeStats = employeeWorkFreeStats ?? Array.Empty<ContainerScheduleProfileViewModel.EmployeeWorkFreeStatRow>();

            // Провайдер стилів тут критичний (dependency): якщо його немає — це помилка конфігурації.
            StyleProvider = styleProvider ?? throw new ArgumentNullException(nameof(styleProvider));
        }
    }

    /// <summary>
    /// Дані про "групу доступності" (availability group), які можуть додатково
    /// експортуватись у SQL (і/або використовуватись для аналітики).
    ///
    /// Містить:
    /// - Group: метадані групи (місяць/рік/назва)
    /// - Members: учасники (зв'язок employee->group)
    /// - Days: дні доступності (тип: work/free/interval тощо)
    /// </summary>
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

    /// <summary>
    /// Опис одного "графіку/чарту" для контейнерного експорту в Excel.
    ///
    /// Тут "ChartName" — ім'я, яке піде в назву листа Excel.
    /// ScheduleContext — повний контекст одного розкладу для заповнення Matrix/Statistic листів.
    /// </summary>
    public sealed class ContainerExcelExportChartContext
    {
        public string ChartName { get; }
        public ScheduleExportContext ScheduleContext { get; }

        public ContainerExcelExportChartContext(string chartName, ScheduleExportContext scheduleContext)
        {
            ChartName = chartName ?? string.Empty;
            ScheduleContext = scheduleContext ?? throw new ArgumentNullException(nameof(scheduleContext));
        }
    }

    /// <summary>
    /// Контекст експорту КОНТЕЙНЕРА в Excel:
    /// - Загальна статистика по контейнеру (employees/shops/hours)
    /// - Дані для "Container" листа (shop headers, employee rows, work/free stats)
    /// - Набір Charts (кожен chart -> окремий schedule -> окремі листи Matrix + Statistic)
    ///
    /// Важливо: Container export використовує ДВА шаблони:
    /// - ScheduleTemplate.xlsx (матриця + статистика)
    /// - ContainerTemplate.xlsx (лист Container)
    /// </summary>
    public sealed class ContainerExcelExportContext
    {
        public int ContainerId { get; }

        public string ContainerName { get; }
        public string ContainerNote { get; }
        public int TotalEmployees { get; }
        public int TotalShops { get; }
        public string TotalEmployeesListText { get; }
        public string TotalShopsListText { get; }
        public string TotalHoursText { get; }
        public IReadOnlyList<ContainerProfileViewModel.ShopHeader> ShopHeaders { get; }
        public IReadOnlyList<ContainerProfileViewModel.EmployeeShopHoursRow> EmployeeShopHoursRows { get; }
        public IReadOnlyList<ContainerProfileViewModel.EmployeeWorkFreeStatRow> EmployeeWorkFreeStats { get; }
        public IReadOnlyList<ContainerExcelExportChartContext> Charts { get; }

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
    /// Контекст експорту ОДНОГО schedule у SQL в рамках контейнера.
    /// Тут просто обгортка, щоб у ContainerSqlExportContext тримати список schedules ("Charts").
    /// </summary>
    public sealed class ContainerSqlExportScheduleContext
    {
        public ScheduleSqlExportContext Schedule { get; }

        public ContainerSqlExportScheduleContext(ScheduleSqlExportContext schedule)
        {
            Schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
        }
    }

    /// <summary>
    /// Контекст експорту контейнера в SQL.
    /// Містить:
    /// - ContainerModel (контейнер)
    /// - Charts (набір schedule контекстів, які треба також інсертити)
    ///
    /// Реалізація BuildContainerSqlScript далі збирає всі сутності (shops/employees/availability/slots/styles)
    /// і пише INSERT OR IGNORE, щоб скрипт був ідемпотентний.
    /// </summary>
    public sealed class ContainerSqlExportContext
    {
        public ContainerModel Container { get; }
        public IReadOnlyList<ContainerSqlExportScheduleContext> Charts { get; }

        public ContainerSqlExportContext(ContainerModel container, IReadOnlyList<ContainerSqlExportScheduleContext> charts)
        {
            Container = container ?? throw new ArgumentNullException(nameof(container));
            Charts = charts ?? Array.Empty<ContainerSqlExportScheduleContext>();
        }
    }

    /// <summary>
    /// Контекст експорту розкладу в SQL.
    ///
    /// Містить основні сутності, які потрібні, щоб відновити schedule у БД:
    /// - Schedule
    /// - Employees (зв'язки schedule_employee)
    /// - Slots (schedule_slot — призначення по днях/слотах)
    /// - CellStyles (schedule_cell_style — кольори/оформлення клітинок)
    /// - AvailabilityGroupData (необов’язково) — дані доступності
    /// </summary>
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

    #endregion

    /// <summary>
    /// Основний сервіс експорту:
    /// - ExportToExcelAsync: один schedule -> Excel (Matrix + Statistic)
    /// - ExportToSqlAsync: один schedule -> SQL-скрипт (INSERT OR IGNORE)
    /// - ExportContainerToExcelAsync: container -> Excel (Container + багато schedules)
    /// - ExportContainerToSqlAsync: container -> SQL-скрипт
    ///
    /// Дизайн-принципи:
    /// 1) Використовує Excel-шаблони (template workbooks) для збереження 1:1 стилів:
    ///    - ширини колонок/висоти рядків
    ///    - merge cells
    ///    - conditional formatting
    ///    - повернуті (rotated) заголовки
    /// 2) Код заповнює ПЕРЕВАЖНО тільки значення (values), не чіпаючи стилі.
    /// 3) Є "санітизація" імен листів/файлів (Excel має обмеження 31 символ + заборонені символи).
    /// 4) Є workaround для PatternType=Gray125 (ClosedXML інколи робить roundtrip у чорний).
    /// </summary>
    public sealed class ScheduleExportService : IScheduleExportService
    {
        /// <summary>
        /// Імена листів, які можуть бути у ContainerTemplate.xlsx.
        /// Це робить код стійким до того, що шаблон назвали по-різному.
        /// </summary>
        private static readonly string[] ContainerTemplateSheetNames = { "ContainerTemplate", "Sheet1", "Container" };

        /// <summary>
        /// Дефолтні назви листів, якщо ScheduleName порожній або невалідний.
        /// </summary>
        private const string DefaultSheetName = "Schedule";
        private const string DefaultStatisticSheetName = "Schedule - Statistic";

        // ===================== TEMPLATE CONFIG =====================

        /// <summary>
        /// Відносний шлях до шаблону schedule (Matrix + Statistic).
        /// У проєкті шаблон має бути з Build Action: Content і Copy to Output Directory: Copy if newer/always,
        /// щоб файл був у вихідній директорії разом з exe.
        /// </summary>
        private const string ExcelTemplateRelativePath = @"Resources\Excel\ScheduleTemplate.xlsx";

        /// <summary>
        /// Окремий шаблон для контейнера (лист "Container").
        /// </summary>
        private const string ContainerExcelTemplateRelativePath = @"Resources\Excel\ContainerTemplate.xlsx";

        /// <summary>
        /// Перелік можливих назв листа-матриці в шаблоні.
        /// Підтримує варіації імен, щоб не падати при перейменуванні шаблону.
        /// </summary>
        private static readonly string[] MatrixTemplateSheetNames = { "ScheduleName", "R_FL_35", "MatrixTemplate" };

        /// <summary>
        /// Перелік можливих назв листа статистики в шаблоні.
        /// </summary>
        private static readonly string[] StatisticTemplateSheetNames = { "ScheduleStatistic", "Schedule Statistic", "StatisticTemplate" };

        // ===================== MATRIX TEMPLATE LAYOUT =====================
        // Тут "мапа" очікуваного layout'у шаблону.
        //
        // Очікування:
        // - B1  = ShopName
        // - Row 1: C1..AA1 = заголовки працівників (часто повернуті/злиті/високі)
        // - Col B: B2..B32 = дати
        // - Matrix body: C2..AA32 = значення розкладу (слоти/позначки)
        //
        // Важливо: код очищає ТІЛЬКИ значення (Contents), зберігаючи стилі.

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
        // Лист статистики також має фіксований layout:
        // - B2..B11: основні поля (ScheduleName, Month, Year, ShopName, Address, Hours, Employees, Days, Shifts)
        // - A12: рядок "Total employees (...) : list"
        // - Row 14: заголовки днів (кожен день займає 3 колонки)
        // - Row 16..: таблиця по працівниках

        private const int S_ValueCol = 2;                // B
        private const int S_EmployeesLineRow = 12;       // рядок з Total employees list (в A12)
        private const int S_DayHeaderRow = 14;           // заголовки днів (кожен день у 3 колонки)
        private const int S_BodyFirstRow = 16;           // перший рядок працівника
        private const int S_MaxSummaryRows = 10;         // скільки рядків у шаблоні стилізовано "з коробки" (16..25)

        // Колонки в summary-таблиці:
        private const int S_EmployeeCol = 1;            // A: Employee
        private const int S_WorkDaysCol = 2;            // B: WorkDays
        private const int S_FreeDaysCol = 3;            // C: FreeDays
        private const int S_SumCol = 4;                 // D: Sum

        // День починається з колонки E (5), кожен день = 3 колонки: From, To, Hours.
        private const int S_FirstDayCol = 5;
        private const int S_DaysCapacity = 31;          // 31 day blocks

        // ===================== PUBLIC API =====================

        /// <summary>
        /// Експорт одного розкладу в Excel:
        /// - завантажує шаблон ScheduleTemplate.xlsx
        /// - знаходить листи MatrixTemplate та StatisticTemplate
        /// - копіює їх у нові листи під назвою розкладу
        /// - видаляє оригінальні шаблонні листи (щоб у фінальному файлі не було "Template")
        /// - заповнює значення
        /// - виконує FixGray125Fills (workaround)
        /// - SaveAs(filePath)
        ///
        /// Виконання відбувається у Task.Run (CPU/IO-bound + ClosedXML не асинхронний).
        /// CancellationToken перевіряється до початку і в контейнерному циклі.
        /// </summary>
        public Task ExportToExcelAsync(ScheduleExportContext context, string filePath, CancellationToken ct = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));

            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                // 1) Знаходимо фізичний файл шаблону у output directory.
                var templatePath = ResolveExcelTemplatePath();

                // 2) Відкриваємо workbook на основі шаблону.
                using var workbook = new XLWorkbook(templatePath);

                // 3) Знаходимо аркуші, які виступають як шаблони для копіювання.
                var matrixTemplate = FindTemplateSheet(workbook, MatrixTemplateSheetNames)
                    ?? throw new InvalidOperationException($"Matrix template sheet not found. Expected one of: {string.Join(", ", MatrixTemplateSheetNames)}");

                var statTemplate = FindTemplateSheet(workbook, StatisticTemplateSheetNames)
                    ?? throw new InvalidOperationException($"Statistic template sheet not found. Expected one of: {string.Join(", ", StatisticTemplateSheetNames)}");

                // 4) Ім'я листа для матриці = назва розкладу (але sanitized).
                var scheduleName = SanitizeWorksheetName(context.ScheduleName, DefaultSheetName);

                // CopyTo важливий: переносить стилі 1:1 (включаючи row height, column width, CF, pattern fills).
                var matrixSheet = matrixTemplate.CopyTo(scheduleName);

                // 5) Лист статистики: базове ім'я + унікалізація, якщо збігається з існуючим.
                var statisticNameBase = SanitizeWorksheetName($"{scheduleName} - Statistic", DefaultStatisticSheetName);
                var statisticName = MakeUniqueSheetName(workbook, statisticNameBase);
                var statSheet = statTemplate.CopyTo(statisticName);

                // 6) Видаляємо шаблонні листи, щоб у фінальному файлі залишилися лише готові листи.
                matrixTemplate.Delete();
                statTemplate.Delete();

                // 7) Заповнюємо тільки значення, не ламаючи оформлення шаблону.
                FillMatrixSheetFromTemplate(matrixSheet, context);
                FillStatisticSheetFromTemplate(statSheet, context);

                // 8) Workaround для Gray125, щоб Excel не показував як чорний.
                FixGray125Fills(matrixSheet, "B2:AA32");
                FixGray125Fills(statSheet);

                // 9) Зберігаємо результат.
                workbook.SaveAs(filePath);
            }, ct);
        }

        /// <summary>
        /// Експорт одного розкладу у SQL-скрипт (SQLite-стиль, INSERT OR IGNORE).
        /// Реалізація:
        /// - будує текст скрипта BuildSqlScript(context)
        /// - пише його в файл як UTF-8
        /// </summary>
        public async Task ExportToSqlAsync(ScheduleSqlExportContext context, string filePath, CancellationToken ct = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));

            var script = BuildSqlScript(context);
            await File.WriteAllTextAsync(filePath, script, Encoding.UTF8, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Експорт контейнера в Excel.
        ///
        /// Відмінність від ExportToExcelAsync:
        /// - Використовує ScheduleTemplate.xlsx для matrix/stat, як і одиночний export
        /// - ДОДАТКОВО відкриває ContainerTemplate.xlsx та копіює з нього лист "ContainerTemplate"
        ///   у поточний workbook (залежить від версії ClosedXML, але тут використано CopyTo(workbook,...))
        /// - Створює лист "Container" і заповнює агреговану таблицю
        /// - Далі для кожного Chart: створює пару листів Matrix+Statistic
        /// - Видаляє всі template листи
        /// </summary>
        public Task ExportContainerToExcelAsync(ContainerExcelExportContext context, string filePath, CancellationToken ct = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));

            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                // 1) Шаблон schedule (Matrix + Statistic)
                var templatePath = ResolveExcelTemplatePath();
                using var workbook = new XLWorkbook(templatePath);

                var matrixTemplate = FindTemplateSheet(workbook, MatrixTemplateSheetNames)
                    ?? throw new InvalidOperationException($"Matrix template sheet not found. Expected one of: {string.Join(", ", MatrixTemplateSheetNames)}");

                var statTemplate = FindTemplateSheet(workbook, StatisticTemplateSheetNames)
                    ?? throw new InvalidOperationException($"Statistic template sheet not found. Expected one of: {string.Join(", ", StatisticTemplateSheetNames)}");

                // 2) Шаблон контейнера (інший файл!)
                var containerTemplatePath = ResolveContainerTemplatePath();
                using var containerWb = new XLWorkbook(containerTemplatePath);

                var containerSrc = FindTemplateSheet(containerWb, ContainerTemplateSheetNames)
                    ?? throw new InvalidOperationException($"Container template sheet not found in ContainerTemplate.xlsx. Expected one of: {string.Join(", ", ContainerTemplateSheetNames)}");

                // 3) Копіюємо container template sheet у workbook, щоб далі робити CopyTo вже всередині одного workbook.
                var containerTemplateName = MakeUniqueSheetName(workbook, "__ContainerTemplate__");

                // NOTE: залежить від версії ClosedXML.
                // У новіших версіях є CopyTo(XLWorkbook, string). Якщо раптом не компілюється — потрібно інший overload.
                var containerTemplate = containerSrc.CopyTo(workbook, containerTemplateName);

                // 4) Створюємо реальний лист "Container" з шаблону
                var containerSheetName = MakeUniqueSheetName(workbook, SanitizeWorksheetName("Container", "Container"));
                var containerSheet = containerTemplate.CopyTo(containerSheetName);
                FillContainerTemplateSheetFromTemplate(containerSheet, context);

                // 5) Для кожного chart: матриця + статистика
                foreach (var chart in context.Charts)
                {
                    ct.ThrowIfCancellationRequested();

                    // Унікалізуємо назву (щоб не було колізій).
                    var scheduleName = MakeUniqueSheetName(workbook, SanitizeWorksheetName(chart.ChartName, DefaultSheetName));

                    // Matrix
                    var matrixSheet = matrixTemplate.CopyTo(scheduleName);
                    FillMatrixSheetFromTemplate(matrixSheet, chart.ScheduleContext);
                    FixGray125Fills(matrixSheet, "B2:AA32");

                    // Statistic
                    var statNameBase = SanitizeWorksheetName($"{scheduleName} - Statistic", DefaultStatisticSheetName);
                    var statName = MakeUniqueSheetName(workbook, statNameBase);
                    var statSheet = statTemplate.CopyTo(statName);
                    FillStatisticSheetFromTemplate(statSheet, chart.ScheduleContext);
                    FixGray125Fills(statSheet);
                }

                // 6) Видаляємо всі template sheets (щоб у фінальному файлі їх не було)
                matrixTemplate.Delete();
                statTemplate.Delete();
                containerTemplate.Delete();

                workbook.SaveAs(filePath);
            }, ct);
        }

        /// <summary>
        /// Експорт контейнера у SQL-скрипт.
        /// Скрипт включає контейнер + всі сутності з усіх schedules (shops, employees, availability, slots, styles).
        /// </summary>
        public async Task ExportContainerToSqlAsync(ContainerSqlExportContext context, string filePath, CancellationToken ct = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));

            var script = BuildContainerSqlScript(context);
            await File.WriteAllTextAsync(filePath, script, Encoding.UTF8, ct).ConfigureAwait(false);
        }

        // ===================== TEMPLATE HELPERS =====================

        /// <summary>
        /// Обчислює абсолютний шлях до ScheduleTemplate.xlsx у output-папці додатка.
        /// Якщо файл не знайдено — кидає FileNotFoundException з поясненням,
        /// щоб відразу було зрозуміло, що проблема в deployment/copy-to-output.
        /// </summary>
        private static string ResolveExcelTemplatePath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var path = Path.Combine(baseDir, ExcelTemplateRelativePath);

            if (!File.Exists(path))
                throw new FileNotFoundException("Excel template not found. Ensure it is copied to output directory.", path);

            return path;
        }

        /// <summary>
        /// Абсолютний шлях до ContainerTemplate.xlsx у output-папці.
        /// </summary>
        private static string ResolveContainerTemplatePath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var path = Path.Combine(baseDir, ContainerExcelTemplateRelativePath);

            if (!File.Exists(path))
                throw new FileNotFoundException("Container Excel template not found.", path);

            return path;
        }

        /// <summary>
        /// Шукає перший аркуш у workbook, назва якого відповідає одному з варіантів "names"
        /// (case-insensitive). Повертає null, якщо нічого не знайдено.
        /// </summary>
        private static IXLWorksheet? FindTemplateSheet(XLWorkbook wb, IEnumerable<string> names)
        {
            foreach (var n in names)
            {
                var ws = wb.Worksheets.FirstOrDefault(s => string.Equals(s.Name, n, StringComparison.OrdinalIgnoreCase));
                if (ws != null) return ws;
            }
            return null;
        }

        /// <summary>
        /// Робить унікальне ім'я листа в рамках workbook:
        /// - якщо baseName вже існує, додає " (1)", " (2)", ...
        /// - враховує обмеження Excel: максимум 31 символ
        /// </summary>
        private static string MakeUniqueSheetName(XLWorkbook wb, string baseName)
        {
            var name = baseName;
            var i = 1;
            while (wb.Worksheets.Any(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                var suffix = $" ({i++})";
                var trimmed = baseName;

                // Excel sheet name length <= 31, тому при додаванні суфікса підрізаємо.
                if (trimmed.Length + suffix.Length > 31)
                    trimmed = trimmed.Substring(0, 31 - suffix.Length);

                name = trimmed + suffix;
            }
            return name;
        }

        /// <summary>
        /// "Санітизація" назви файлу під файлову систему:
        /// - якщо name порожній -> fallback
        /// - замінює недопустимі символи на '_'
        /// </summary>
        public static string SanitizeFileName(string? name, string fallback)
        {
            var safe = string.IsNullOrWhiteSpace(name) ? fallback : name.Trim();
            foreach (var c in Path.GetInvalidFileNameChars())
                safe = safe.Replace(c, '_');
            return string.IsNullOrWhiteSpace(safe) ? fallback : safe;
        }

        /// <summary>
        /// "Санітизація" назви листа Excel:
        /// - Excel забороняє: [ ] * ? / \ :
        /// - довжина максимум 31 символ
        /// - якщо порожньо -> fallback
        /// </summary>
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
        /// Workaround: ClosedXML (та інші writers) інколи неправильно "roundtrip"-лять
        /// PatternType=Gray125 (12.5% сірий патерн), і Excel показує його як чорний.
        ///
        /// Тому тут:
        /// - для клітинок (direct style) та для conditional formats:
        ///   якщо PatternType == Gray125, примусово задаємо BackgroundColor/PatternColor.
        ///
        /// rangeAddress:
        /// - якщо задано: працюємо лише по цьому діапазону (швидше)
        /// - якщо null: по всьому used range (може бути повільніше)
        /// </summary>
        private static void FixGray125Fills(IXLWorksheet sheet, string? rangeAddress = null)
        {
            if (sheet is null) return;

            // 1) Direct cell styles
            var range = !string.IsNullOrWhiteSpace(rangeAddress) ? sheet.Range(rangeAddress) : sheet.RangeUsed();
            if (range != null)
            {
                foreach (var cell in range.Cells())
                {
                    if (cell.Style.Fill.PatternType == XLFillPatternValues.Gray125)
                    {
                        // Зберігаємо Gray125, але фіксуємо explicit кольори.
                        cell.Style.Fill.PatternType = XLFillPatternValues.Gray125;
                        cell.Style.Fill.BackgroundColor = XLColor.White;
                        cell.Style.Fill.PatternColor = XLColor.Black; // foreground pattern
                    }
                }
            }

            // 2) Conditional formats
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

        // ===================== EXCEL FILL: MATRIX =====================

        /// <summary>
        /// Заповнює лист матриці (Matrix) на основі шаблону.
        ///
        /// Критичний принцип: "НЕ ЛАМАТИ стилі" — тому очищаємо тільки Contents.
        ///
        /// Вхідні дані:
        /// - context.ScheduleMatrix: DataView -> Table -> Columns/Rows
        ///
        /// Очікувана поведінка:
        /// 1) B1 = ShopName
        /// 2) Row 1 (C..): заголовки працівників (по колонках DataTable, окрім службових)
        /// 3) Col B (rows 2..32): дати
        /// 4) Body (C..AA): значення з DataView або "-" як placeholder
        /// 5) Для "неіснуючих" днів (наприклад 30-31 у лютому) — записуємо " " (текст),
        ///    щоб Excel формули/CF не інтерпретували це як "0" або дату.
        /// </summary>
        private static void FillMatrixSheetFromTemplate(IXLWorksheet sheet, ScheduleExportContext context)
        {
            // Очищаємо ТІЛЬКИ значення (Contents), стиль залишається як у шаблоні.
            sheet.Range(MatrixClearRange).Clear(XLClearOptions.Contents);

            // B1 = ShopName
            sheet.Cell(M_ShopRow, M_ShopCol).Value = context.ShopName;

            // Якщо DataView/Table пусті — немає що заповнювати.
            var table = context.ScheduleMatrix?.Table;
            if (table is null) return;

            // Фільтруємо службові колонки, які не мають потрапляти в Excel.
            // ConflictColumnName / WeekendColumnName — з ScheduleMatrixConstants.
            var visibleCols = table.Columns.Cast<DataColumn>()
                .Where(c => c.ColumnName != ScheduleMatrixConstants.ConflictColumnName
                         && c.ColumnName != ScheduleMatrixConstants.WeekendColumnName)
                .ToList();

            // Визначаємо "day column": спеціальна колонка Day, або fallback на першу.
            var dayCol = visibleCols.FirstOrDefault(c => c.ColumnName == ScheduleMatrixConstants.DayColumnName)
                         ?? visibleCols.FirstOrDefault();

            // Все, що не dayCol — це "дані" (колонки працівників/слотів).
            var dataCols = visibleCols.Where(c => !ReferenceEquals(c, dayCol)).ToList();

            // Перевіряємо, чи помістяться всі колонки у шаблон (C..AA).
            var capacity = M_LastDataCol - M_FirstDataCol + 1;
            if (dataCols.Count > capacity)
                throw new InvalidOperationException($"Template supports max {capacity} employee columns (C..AA). Current: {dataCols.Count}.");

            // Пишемо заголовки працівників у row 1, починаючи з колонки C.
            // Caption використовується, якщо є (часто він більш "людяний" ніж ColumnName).
            for (int i = 0; i < dataCols.Count; i++)
            {
                var col = dataCols[i];
                sheet.Cell(M_HeaderRow, M_FirstDataCol + i).Value =
                    string.IsNullOrWhiteSpace(col.Caption) ? col.ColumnName : col.Caption;
            }

            // Колонки, які є в шаблоні, але не мають працівника — заповнюємо "0"
            // (це, ймовірно, потрібно для формул/умовного форматування в шаблоні).
            for (int c = M_FirstDataCol + dataCols.Count; c <= M_LastDataCol; c++)
                sheet.Cell(M_HeaderRow, c).Value = "0";

            // Скільки днів реально у місяці.
            var daysInMonth = DateTime.DaysInMonth(context.ScheduleYear, context.ScheduleMonth);

            // Скільки рядків можемо читати з DataView (але не більше 31).
            var maxRows = Math.Min(M_DayCount, context.ScheduleMatrix.Count);

            // Заповнюємо 31 рядок (2..32) незалежно від місяця, бо шаблон саме такий.
            for (int r = 0; r < M_DayCount; r++)
            {
                var excelRow = M_FirstDayRow + r;
                DataRowView? rowView = r < maxRows ? (DataRowView)context.ScheduleMatrix[r] : null;

                // === Заповнення дати у колонці B ===
                // Важливо: якщо день існує у місяці — кладемо DateTime, щоб Excel міг форматувати як дату.
                // Якщо день НЕ існує (наприклад 30-31 у лютому) — кладемо текст " ",
                // щоб Excel не застосував weekend CF/формули некоректно.
                var dateCell = sheet.Cell(excelRow, M_DateCol);

                if (r < daysInMonth)
                {
                    // Якщо в даних є день/дата — пробуємо відновити DateTime.
                    // Якщо нема — беремо просто календарну дату (year/month/(r+1)).
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

                // === Заповнення основних даних по працівниках ===
                for (int i = 0; i < dataCols.Count; i++)
                {
                    var col = dataCols[i];
                    var cell = sheet.Cell(excelRow, M_FirstDataCol + i);

                    // Якщо DataView не має цього рядка — ставимо "-" для "порожньої" клітинки.
                    if (rowView == null)
                    {
                        cell.Value = "-";
                        continue;
                    }

                    // Якщо значення null/empty — теж "-".
                    SetDashIfEmpty(cell, rowView[col.ColumnName]);
                }

                // Порожні "placeholder" колонки (до AA) -> "-"
                for (int c = M_FirstDataCol + dataCols.Count; c <= M_LastDataCol; c++)
                    sheet.Cell(excelRow, c).Value = "-";
            }

            // Настройки друку:
            // - повторювати заголовок (row 1) на кожній сторінці
            // - повторювати колонку днів (col B) зліва
            sheet.PageSetup.SetRowsToRepeatAtTop(M_HeaderRow, M_HeaderRow);
            sheet.PageSetup.SetColumnsToRepeatAtLeft(M_DateCol, M_DateCol);

            // Опційно: обмежити Print Area тільки матрицею.
            sheet.PageSetup.PrintAreas.Clear();
            sheet.PageSetup.PrintAreas.Add(MatrixClearRange); // B1:AA32
        }

        /// <summary>
        /// Записує "-" якщо значення null/DBNull/empty.
        /// Це стабілізує вигляд матриці і прибирає "порожні" клітинки.
        /// </summary>
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

        /// <summary>
        /// Пробує побудувати дату для рядка матриці.
        ///
        /// Підтримує формати:
        /// - raw вже DateTime
        /// - day-of-month як int або як string ("1", "02")
        /// - повний date string, який парситься під CurrentCulture
        ///
        /// Якщо нічого не вийшло — повертає null.
        /// </summary>
        private static DateTime? TryBuildDate(ScheduleExportContext context, DataColumn? dayCol, DataRowView rowView)
        {
            if (dayCol is null) return null;

            var raw = rowView[dayCol.ColumnName];
            if (raw is null || raw == DBNull.Value) return null;

            // Already typed DateTime
            if (raw is DateTime dt) return dt.Date;

            // Day-of-month as int
            if (raw is int d && d >= 1 && d <= 31)
                return SafeDate(context.ScheduleYear, context.ScheduleMonth, d);

            // Day-of-month as string
            var s = raw.ToString()?.Trim();
            if (int.TryParse(s, out var parsed) && parsed >= 1 && parsed <= 31)
                return SafeDate(context.ScheduleYear, context.ScheduleMonth, parsed);

            // Full date string parse (optional)
            if (!string.IsNullOrWhiteSpace(s) &&
                DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.None, out var parsedDt))
                return parsedDt.Date;

            return null;
        }

        /// <summary>
        /// Safe-конструктор DateTime: повертає null, якщо рік/місяць/день невалідні.
        /// </summary>
        private static DateTime? SafeDate(int year, int month, int day)
        {
            try { return new DateTime(year, month, day); }
            catch { return null; }
        }

        // ===================== EXCEL FILL: STATISTIC =====================

        /// <summary>
        /// Заповнює лист статистики (Statistic) з контексту schedule.
        ///
        /// Важливо:
        /// - Шаблон має вже готові стилі/merge/CF.
        /// - Ми просто вставляємо значення у правильні клітинки.
        ///
        /// Фішка:
        /// - Якщо SummaryRows більше ніж S_MaxSummaryRows (10),
        ///   то вставляємо додаткові рядки і копіюємо формат з останнього "шаблонного" рядка.
        /// </summary>
        private static void FillStatisticSheetFromTemplate(IXLWorksheet sheet, ScheduleExportContext context)
        {
            // === Header fields (B2..B11) ===
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

            // A12: список працівників у одному рядку (зручно для швидкого перегляду).
            sheet.Cell(S_EmployeesLineRow, 1).Value =
                $"Total employees ({context.TotalEmployees}): {context.TotalEmployeesListText}";

            // === Day headers (Row 14) ===
            // Кожен день = 3 колонки. Починаємо з E (5).
            // Наприклад:
            // Day1 header -> E14
            // Day2 header -> H14
            // Day3 header -> K14
            for (int i = 0; i < S_DaysCapacity; i++)
            {
                var col = S_FirstDayCol + i * 3;
                var text = i < context.SummaryDayHeaders.Count ? context.SummaryDayHeaders[i].Text : string.Empty;
                sheet.Cell(S_DayHeaderRow, col).Value = text;
            }

            // === Map Work/Free stats by employee name ===
            // Групуємо по Employee (case-insensitive) і беремо перший запис.
            // Це дозволяє швидко знайти WorkDays/FreeDays для кожного summaryRow.
            var wfByEmployee = (context.EmployeeWorkFreeStats ?? Array.Empty<ContainerScheduleProfileViewModel.EmployeeWorkFreeStatRow>())
                .Where(x => !string.IsNullOrWhiteSpace(x.Employee))
                .GroupBy(x => x.Employee!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            // === SUMMARY: auto-expand rows if needed ===
            // Якщо у нас більше працівників, ніж у шаблоні передбачено стилізованих рядків — додаємо рядки.
            var summaryNeeded = context.SummaryRows.Count;
            var summaryExtra = Math.Max(0, summaryNeeded - S_MaxSummaryRows);

            // Остання колонка "summary area":
            // S_FirstDayCol + (31-1)*3 + 2 (бо 3 колонки на день: +0 +1 +2)
            var summaryLastCol = S_FirstDayCol + (S_DaysCapacity - 1) * 3 + 2;

            if (summaryExtra > 0)
            {
                // insertAfter — останній стилізований рядок (25)
                var insertAfter = S_BodyFirstRow + S_MaxSummaryRows - 1;
                sheet.Row(insertAfter).InsertRowsBelow(summaryExtra);

                // Копіюємо форматування з останнього стилізованого рядка на нові рядки.
                var templateRange = sheet.Range(insertAfter, 1, insertAfter, summaryLastCol);
                var templateHeight = sheet.Row(insertAfter).Height;

                for (int k = 1; k <= summaryExtra; k++)
                {
                    templateRange.CopyTo(sheet.Range(insertAfter + k, 1, insertAfter + k, summaryLastCol));
                    sheet.Row(insertAfter + k).Height = templateHeight;
                }
            }

            // summaryTotalRows = максимум з (стилізованих рядків) та (реально потрібних).
            // Це дозволяє:
            // - якщо людей менше 10: почистити зайві рядки
            // - якщо людей більше 10: заповнити всі додані рядки
            var summaryTotalRows = Math.Max(S_MaxSummaryRows, summaryNeeded);

            for (int r = 0; r < summaryTotalRows; r++)
            {
                var row = S_BodyFirstRow + r;

                // Якщо рядок "зайвий" (працівників менше), чистимо Contents, залишаючи стиль.
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

                // Реальний рядок summary по працівнику.
                var summaryRow = context.SummaryRows[r];
                var employeeName = summaryRow.Employee ?? string.Empty;

                // A: Employee name
                sheet.Cell(row, S_EmployeeCol).Value = employeeName;

                // B/C: WorkDays/FreeDays (беремо з wfByEmployee)
                if (wfByEmployee.TryGetValue(employeeName, out var wf))
                {
                    sheet.Cell(row, S_WorkDaysCol).Value = wf.WorkDays.ToString(CultureInfo.InvariantCulture);
                    sheet.Cell(row, S_FreeDaysCol).Value = wf.FreeDays.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    // Якщо немає stats по працівнику — просто очищаємо (не ставимо 0, щоб не вводити в оману).
                    sheet.Cell(row, S_WorkDaysCol).Clear(XLClearOptions.Contents);
                    sheet.Cell(row, S_FreeDaysCol).Clear(XLClearOptions.Contents);
                }

                // D: Sum (наприклад "160h")
                sheet.Cell(row, S_SumCol).Value = summaryRow.Sum ?? string.Empty;

                // Денні клітинки (From/To/Hours). Якщо даних менше 31 — решта буде пусто.
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

            // Примітка: "нижню mini-таблицю" прибрано — коментар у коді означає,
            // що раніше, ймовірно, було ще одне місце заповнення, але зараз воно видалене.
        }

        /// <summary>
        /// Заповнює лист "Container" на основі ContainerTemplate.xlsx.
        ///
        /// Алгоритм:
        /// 1) Проходимо по всіх used cells і замінюємо плейсхолдери виду "{Name}", "{Total Hours}" тощо.
        /// 2) Знаходимо headerRow таблиці по працівниках (рядок, де в колонці 1 "Employee" і в колонці 4 "Hours Sum").
        /// 3) Знаходимо templateRow (перший рядок, де є "{Employee}") — з нього копіюється формат у додані рядки.
        /// 4) Визначаємо shop-колонки (послідовність колонок із "{Shop}" у headerRow).
        /// 5) Записуємо назви магазинів у заголовки.
        /// 6) Заповнюємо рядки працівників і години по магазинах (HoursByShop).
        /// 7) Накладаємо borders на всю таблицю.
        /// 8) Додатково стилізуємо рядок TOTAL (жирний + top border).
        /// </summary>
        private static void FillContainerTemplateSheetFromTemplate(IXLWorksheet sheet, ContainerExcelExportContext context)
        {
            // 1) Replace placeholders everywhere.
            // Map: ключ — шаблонний токен, значення — реальні дані.
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

            // Замінюємо токени у всіх used cells.
            // Оптимізація: пропускаємо рядки без '{' щоб не робити зайвих Replace.
            foreach (var cell in sheet.CellsUsed())
            {
                var s = cell.GetString();
                if (string.IsNullOrWhiteSpace(s) || !s.Contains('{')) continue;

                foreach (var kv in map)
                    if (s.Contains(kv.Key, StringComparison.Ordinal))
                        s = s.Replace(kv.Key, kv.Value);

                cell.Value = s;
            }

            // 2) Locate table header row: "Employee" + "Hours Sum"
            // Це "якір", щоб не залежати від конкретного номера рядка в шаблоні.
            var headerRow = sheet.RowsUsed()
                .Select(r => r.RowNumber())
                .FirstOrDefault(r =>
                    string.Equals(sheet.Cell(r, 1).GetString(), "Employee", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(sheet.Cell(r, 4).GetString(), "Hours Sum", StringComparison.OrdinalIgnoreCase));

            if (headerRow <= 0) return;

            // Data template row: перший рядок, де є "{Employee}".
            // Якщо не знайшли — беремо headerRow + 1 як fallback.
            var templateRow = sheet.RowsUsed()
                .Select(r => r.RowNumber())
                .FirstOrDefault(r => sheet.Cell(r, 1).GetString().Contains("{Employee}", StringComparison.OrdinalIgnoreCase));

            if (templateRow <= 0) templateRow = headerRow + 1;

            // 3) Find shop columns in header row (cells with "{Shop}")
            // Шукаємо першу колонку, де у headerRow є "{Shop}".
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

            // lastShopCol — кінець contiguous блоку "{Shop}" колонок.
            lastShopCol = firstShopCol;
            while (lastShopCol <= lastUsedCol &&
                   sheet.Cell(headerRow, lastShopCol).GetString().Contains("{Shop}", StringComparison.OrdinalIgnoreCase))
                lastShopCol++;
            lastShopCol--;

            var shopCapacity = lastShopCol - firstShopCol + 1;
            var shops = context.ShopHeaders?.ToList() ?? new List<ContainerProfileViewModel.ShopHeader>();

            // Якщо магазинів більше, ніж колонок {Shop} у шаблоні — це конфігураційна помилка.
            if (shops.Count > shopCapacity)
                throw new InvalidOperationException($"ContainerTemplate supports max {shopCapacity} shops. Add more {{Shop}} columns to the template.");

            // 4) Write shop headers
            // Записуємо назви магазинів у заголовки; якщо магазинів менше capacity — решта порожня.
            for (int i = 0; i < shopCapacity; i++)
            {
                sheet.Cell(headerRow, firstShopCol + i).Value = i < shops.Count ? shops[i].Name : string.Empty;
            }

            // 5) Fill employee rows
            var rows = context.EmployeeShopHoursRows?.ToList() ?? new List<ContainerProfileViewModel.EmployeeShopHoursRow>();

            // Гарантуємо, що рядок TOTAL (якщо існує) буде внизу таблиці.
            // Тут OrderBy повертає 0 для звичайних і 1 для TOTAL -> TOTAL опиниться останнім.
            if (rows.Count > 1)
            {
                rows = rows
                    .OrderBy(x => string.Equals(x.Employee, "TOTAL", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                    .ToList();
            }

            // Якщо рядків немає — очищаємо templateRow і виходимо.
            if (rows.Count == 0)
            {
                sheet.Range(templateRow, 1, templateRow, lastShopCol).Clear(XLClearOptions.Contents);
                return;
            }

            // Якщо рядків більше 1 — вставляємо (rows.Count - 1) рядків під templateRow
            // і копіюємо форматування з templateRow у всі нові рядки.
            if (rows.Count > 1)
            {
                sheet.Row(templateRow).InsertRowsBelow(rows.Count - 1);

                var templateRange = sheet.Range(templateRow, 1, templateRow, lastShopCol);
                for (int i = 1; i < rows.Count; i++)
                    templateRange.CopyTo(sheet.Range(templateRow + i, 1, templateRow + i, lastShopCol));
            }

            // Заповнюємо кожен рядок працівника.
            for (int i = 0; i < rows.Count; i++)
            {
                var r = templateRow + i;
                var item = rows[i];

                // A: Employee
                sheet.Cell(r, 1).Value = item.Employee ?? string.Empty;

                // B/C: WorkDays / FreeDays
                // Тут типи можуть бути int/string — залежить від ViewModel.
                sheet.Cell(r, 2).Value = item.WorkDays;
                sheet.Cell(r, 3).Value = item.FreeDays;

                // D: Hours Sum (fallback "0")
                sheet.Cell(r, 4).Value = item.HoursSum ?? "0";

                // Shops: години по кожному магазину.
                for (int s = 0; s < shopCapacity; s++)
                {
                    if (s >= shops.Count)
                    {
                        // Якщо магазинів менше ніж capacity, то залишаємо порожньо.
                        sheet.Cell(r, firstShopCol + s).Value = string.Empty;
                        continue;
                    }

                    var shopKey = shops[s].Key;

                    // HoursByShop: Dictionary<shopKey, string>
                    item.HoursByShop.TryGetValue(shopKey, out var val);

                    // Нормалізація: якщо пусто -> "0"
                    sheet.Cell(r, firstShopCol + s).Value = string.IsNullOrWhiteSpace(val) ? "0" : val;
                }
            }

            // 6) Apply "All Borders" for whole table (header + all data rows + shops columns)
            var lastDataRow = templateRow + rows.Count - 1;

            // endCol — остання "реальна" shop-колонка (не capacity, а фактично заповнена),
            // але з fallback мінімум 1, щоб не зламати Range.
            var endCol = firstShopCol + Math.Max(shops.Count, 1) - 1;
            var tableRange = sheet.Range(headerRow, 1, lastDataRow, endCol);

            // All borders: зовнішній + внутрішній.
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // ✅ Re-apply TOTAL styling AFTER borders
            // Важливо: якщо спочатку зробити border, а потім "TOTAL border top", воно буде видно.
            // Якщо навпаки — може перезатертись.
            var totalIndex = rows.FindIndex(x => string.Equals(x.Employee, "TOTAL", StringComparison.OrdinalIgnoreCase));
            if (totalIndex >= 0)
            {
                var totalRow = templateRow + totalIndex;

                // Жирний шрифт на всьому рядку TOTAL.
                sheet.Row(totalRow).Style.Font.Bold = true;

                // Top-border (Medium) по всій ширині таблиці, щоб відділити TOTAL від інших.
                tableRange.Worksheet.Range(totalRow, 1, totalRow, tableRange.LastColumn().ColumnNumber())
                    .Style.Border.TopBorder = XLBorderStyleValues.Medium; // можна змінити на Thin, якщо треба менш контрастно
            }
        }

        // ===================== SQL EXPORT =====================

        /// <summary>
        /// Будує SQL-скрипт для контейнера (і його schedules).
        ///
        /// Стратегія:
        /// - Пише BEGIN TRANSACTION; ... COMMIT;
        /// - Робить INSERT OR IGNORE для ідемпотентності (повторний запуск не створює дублі)
        /// - Збирає всі сутності зі всіх charts, дедуплікує їх по Id (shops, employees, availability*)
        /// - Сортує перед вставкою (OrderBy(Id)), щоб результат був стабільний/детермінований
        ///
        /// Під БД тут очевидно мається на увазі SQLite (бо INSERT OR IGNORE).
        /// </summary>
        private static string BuildContainerSqlScript(ContainerSqlExportContext context)
        {
            var sb = new StringBuilder(8192);
            sb.AppendLine("-- GF3 Container export");
            sb.AppendLine($"-- Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("BEGIN TRANSACTION;");
            sb.AppendLine();

            var container = context.Container;

            // container
            sb.AppendLine(SqlInsert("container",
                ("id", container.Id),
                ("name", container.Name),
                ("note", container.Note)));

            // Підготовка колекцій для дедуплікації по ID.
            var shops = new Dictionary<int, ShopModel>();
            var employees = new Dictionary<int, EmployeeModel>();
            var availabilityGroups = new Dictionary<int, AvailabilityGroupModel>();
            var availabilityMembers = new Dictionary<int, AvailabilityGroupMemberModel>();
            var availabilityDays = new Dictionary<int, AvailabilityGroupDayModel>();
            var schedules = new List<ScheduleModel>();
            var scheduleEmployees = new List<ScheduleEmployeeModel>();
            var slots = new List<ScheduleSlotModel>();
            var styles = new List<ScheduleCellStyleModel>();

            // Збираємо дані з усіх schedules ("charts").
            foreach (var chart in context.Charts)
            {
                var scheduleCtx = chart.Schedule;
                var schedule = scheduleCtx.Schedule;
                schedules.Add(schedule);

                // Shop
                if (schedule.Shop != null && !shops.ContainsKey(schedule.Shop.Id))
                    shops.Add(schedule.Shop.Id, schedule.Shop);

                // Employees — збираємо з schedule_employee + availability members.
                foreach (var employee in CollectEmployees(scheduleCtx))
                {
                    if (!employees.ContainsKey(employee.Id))
                        employees.Add(employee.Id, employee);
                }

                // Availability group (optional)
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

                // Додаємо schedule-specific таблиці як є (можуть повторюватися по різних schedules по Id? зазвичай ні).
                scheduleEmployees.AddRange(scheduleCtx.Employees ?? Array.Empty<ScheduleEmployeeModel>());
                slots.AddRange(scheduleCtx.Slots ?? Array.Empty<ScheduleSlotModel>());
                styles.AddRange(scheduleCtx.CellStyles ?? Array.Empty<ScheduleCellStyleModel>());
            }

            // Вставляємо shops, employees, availability* — відсортовано, щоб скрипт був детермінований.
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

            // schedules
            foreach (var schedule in schedules.OrderBy(s => s.Id))
                sb.AppendLine(SqlInsert("schedule", ("id", schedule.Id), ("container_id", schedule.ContainerId), ("shop_id", schedule.ShopId), ("name", schedule.Name), ("year", schedule.Year), ("month", schedule.Month), ("people_per_shift", schedule.PeoplePerShift), ("shift1_time", schedule.Shift1Time), ("shift2_time", schedule.Shift2Time), ("max_hours_per_emp_month", schedule.MaxHoursPerEmpMonth), ("max_consecutive_days", schedule.MaxConsecutiveDays), ("max_consecutive_full", schedule.MaxConsecutiveFull), ("max_full_per_month", schedule.MaxFullPerMonth), ("note", schedule.Note), ("availability_group_id", schedule.AvailabilityGroupId)));

            // schedule_employee
            foreach (var se in scheduleEmployees.OrderBy(e => e.Id))
                sb.AppendLine(SqlInsert("schedule_employee", ("id", se.Id), ("schedule_id", se.ScheduleId), ("employee_id", se.EmployeeId), ("min_hours_month", se.MinHoursMonth)));

            // schedule_slot
            foreach (var slot in slots.OrderBy(s => s.Id))
                sb.AppendLine(SqlInsert("schedule_slot", ("id", slot.Id), ("schedule_id", slot.ScheduleId), ("day_of_month", slot.DayOfMonth), ("slot_no", slot.SlotNo), ("employee_id", slot.EmployeeId), ("status", slot.Status.ToString()), ("from_time", slot.FromTime), ("to_time", slot.ToTime)));

            // schedule_cell_style
            foreach (var style in styles.OrderBy(s => s.Id))
                sb.AppendLine(SqlInsert("schedule_cell_style", ("id", style.Id), ("schedule_id", style.ScheduleId), ("day_of_month", style.DayOfMonth), ("employee_id", style.EmployeeId), ("background_color_argb", style.BackgroundColorArgb), ("text_color_argb", style.TextColorArgb)));

            sb.AppendLine();
            sb.AppendLine("COMMIT;");
            return sb.ToString();
        }

        /// <summary>
        /// Будує SQL-скрипт для ОДНОГО schedule.
        /// Порядок вставок:
        /// - container (якщо є)
        /// - shop (якщо є)
        /// - employee (зібрані)
        /// - availability_group* (якщо є)
        /// - schedule
        /// - schedule_employee
        /// - schedule_slot
        /// - schedule_cell_style
        ///
        /// Використовується INSERT OR IGNORE, тому скрипт ідемпотентний.
        /// </summary>
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

            // container (optional)
            if (container != null)
            {
                sb.AppendLine(SqlInsert("container",
                    ("id", container.Id),
                    ("name", container.Name),
                    ("note", container.Note)));
            }

            // shop (optional)
            if (shop != null)
            {
                sb.AppendLine(SqlInsert("shop",
                    ("id", shop.Id),
                    ("name", shop.Name),
                    ("address", shop.Address),
                    ("description", shop.Description)));
            }

            // employees
            foreach (var employee in employees.OrderBy(e => e.Id))
            {
                sb.AppendLine(SqlInsert("employee",
                    ("id", employee.Id),
                    ("first_name", employee.FirstName),
                    ("last_name", employee.LastName),
                    ("phone", employee.Phone),
                    ("email", employee.Email)));
            }

            // availability group (optional)
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

            // schedule
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

            // schedule_employee
            foreach (var scheduleEmployee in scheduleEmployees.OrderBy(e => e.Id))
            {
                sb.AppendLine(SqlInsert("schedule_employee",
                    ("id", scheduleEmployee.Id),
                    ("schedule_id", scheduleEmployee.ScheduleId),
                    ("employee_id", scheduleEmployee.EmployeeId),
                    ("min_hours_month", scheduleEmployee.MinHoursMonth)));
            }

            // schedule_slot
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

            // schedule_cell_style
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

        /// <summary>
        /// Збирає EmployeeModel зі ScheduleSqlExportContext.
        ///
        /// Джерела:
        /// - schedule_employee.Employee
        /// - availability_group_member.Employee (якщо availability є)
        ///
        /// Дедуплікація по Employee.Id.
        /// </summary>
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

        /// <summary>
        /// Будує SQL INSERT OR IGNORE.
        ///
        /// INSERT OR IGNORE:
        /// - Якщо запис з таким PK/UNIQUE уже існує — вставка ігнорується без помилки.
        /// Це дає "ідемпотентність" скрипта.
        ///
        /// values: params масив пар (Column, Value).
        /// </summary>
        private static string SqlInsert(string table, params (string Column, object? Value)[] values)
        {
            var columns = string.Join(", ", values.Select(v => v.Column));
            var vals = string.Join(", ", values.Select(v => ToSqlLiteral(v.Value)));

            return $"INSERT OR IGNORE INTO {table} ({columns}) VALUES ({vals});";
        }

        /// <summary>
        /// Перетворює .NET-значення у SQL literal (для вставки у текст скрипта).
        ///
        /// Правила:
        /// - null -> NULL
        /// - string -> '...' з екрануванням апострофів
        /// - bool -> 1/0 (SQLite-стиль)
        /// - DateTime -> 'yyyy-MM-dd HH:mm:ss'
        /// - Enum -> 'EnumValue'
        /// - IFormattable -> InvariantCulture (щоб не було коми/крапки залежно від локалі)
        ///
        /// IMPORTANT (практичний нюанс):
        /// Це "ручне" формування SQL. Для великих систем краще параметризовані запити,
        /// але тут мета — саме експорт скрипта у файл.
        /// </summary>
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

        /// <summary>
        /// Екранує апострофи для SQL string literal:
        /// ' -> '' (подвійний апостроф)
        /// </summary>
        private static string EscapeSqlString(string value) => value.Replace("'", "''");

        // ===================== EXISTING HELPERS =====================

        /// <summary>
        /// Safe helper для читання bool з DataRowView:
        /// - перевіряє існування таблиці/колонки
        /// - підтримує вже bool або рядок "true/false"
        /// - null/DBNull -> false
        ///
        /// У цьому файлі наразі НЕ використовується, але залишений як helper.
        /// </summary>
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