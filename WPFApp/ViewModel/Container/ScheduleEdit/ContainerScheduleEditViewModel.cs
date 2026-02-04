// --------------------------
// ПІДКЛЮЧЕННЯ ПРОСТОРІВ ІМЕН
// --------------------------
// Тут підключаються всі типи/моделі, які використовує ViewModel:
// - DataAccessLayer.Models: моделі з БД/сервісу (ScheduleModel, ShopModel, EmployeeModel, SlotModel тощо)
// - System.*: базові колекції/дані/тексти
// - WPFApp.Infrastructure.*: твої утиліти (MatrixEngine, ValidationErrors, Debouncer тощо)
// - WPFApp.Service: команди/RelayCommand, AsyncRelayCommand (ймовірно звідси)
using BusinessLogicLayer.Availability;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using WPFApp.Infrastructure;
using WPFApp.Infrastructure.AvailabilityPreview;
using WPFApp.Infrastructure.ScheduleMatrix;
using WPFApp.Infrastructure.Threading;
using WPFApp.Infrastructure.Validation;
using WPFApp.Service;
using WPFApp.ViewModel.Container.Edit;
using WPFApp.ViewModel.Container.ScheduleEdit.Helpers;

namespace WPFApp.ViewModel.Container.ScheduleEdit
{

    /// <summary>
    /// Головний ViewModel для додавання/редагування Schedule (розкладу).
    ///
    /// Важливо:
    /// - Це partial-клас: логіка рознесена по кількох .cs файлах для “чистоти”.
    /// - Цей файл містить переважно:
    ///   1) основні властивості (ScheduleName/Month/Year…)
    ///   2) командні об'єкти (Save/Generate/…)
    ///   3) базовий стан та зв'язки (SelectedBlock, SelectedShop, SelectedAvailabilityGroup)
    ///   4) базову ініціалізацію (constructor + ResetForNew)
    ///
    /// Інші “великі” логіки винесені:
    /// - Matrix refresh/edit -> Matrix.cs / ScheduleMatrixEngine
    /// - Validation storage -> ValidationErrors + Validation.cs
    /// - Selection debounce logic -> Selection.cs + UiDebouncedAction
    /// - Cell styling (background/foreground) -> CellStyling.cs + ScheduleCellStyleStore
    /// - Lookups set/sync -> Lookups.cs
    /// - Generic model setter -> ModelBinding.cs
    /// </summary>
    public sealed partial class ContainerScheduleEditViewModel
        : ViewModelBase, INotifyDataErrorInfo, IScheduleMatrixStyleProvider
    {
        private readonly IAvailabilityGroupService _availabilityGroupService;
        private readonly IEmployeeService _employeeService;
        // --------------------------
        // 1) РЕЖИМ "ФАРБУВАННЯ" КЛІТИНОК
        // --------------------------
        //
        // PaintMode використовується для UX:
        // - None: користувач нічого не “малює”
        // - Background: застосовуємо останній вибраний колір фону до клітинок
        // - Foreground: застосовуємо останній вибраний колір тексту
        //
        // Цей enum потрібен, щоб UI розумів який інструмент активний.
        public enum SchedulePaintMode
        {
            None,
            Background,
            Foreground
        }

        // -------------------------------------------------------
        // 2) КОНСТАНТИ МАТРИЦІ (АЛІАСИ НА SHARED CONSTANTS)
        // -------------------------------------------------------
        //
        // Це назви колонок у DataTable/DataView, які будує ScheduleMatrixEngine.
        // Вони винесені в ScheduleMatrixConstants, а тут продубльовані як const-аліаси,
        // щоб в решті VM не змінювати сотні посилань.
        public const string DayColumnName = ScheduleMatrixConstants.DayColumnName;
        public const string ConflictColumnName = ScheduleMatrixConstants.ConflictColumnName;
        public const string WeekendColumnName = ScheduleMatrixConstants.WeekendColumnName;
        public const string EmptyMark = ScheduleMatrixConstants.EmptyMark;

        // --------------------------
        // 3) ПРИВАТНІ ПОЛЯ СТАНУ VM
        // --------------------------

        // Owner (ContainerViewModel) — “керівник” цього VM:
        // - запускає пошуки, збереження, генерацію
        // - виконує UI-thread виклики
        // - відкриває color picker і т.д.
        private readonly ContainerViewModel _owner;

        // _validation — єдиний контейнер помилок (INotifyDataErrorInfo).
        // Він зберігає помилки по propertyName і піднімає ErrorsChanged.
        private readonly WPFApp.Infrastructure.Validation.ValidationErrors _validation = new();

        // _colNameToEmpId — відповідність “ім’я колонки матриці” -> EmployeeId.
        // Заповнюється коли матриця будується (BuildScheduleTable повертає map).
        // Потрібно для:
        // - редагування клітинки (знати, який employeeId відповідає колонці)
        // - підказок/tooltip (наприклад total hours по працівнику)
        private readonly Dictionary<string, int> _colNameToEmpId = new();

        // Store стилів клітинок: швидкий індекс для (day, employee) -> стиль.
        // Під капотом він синхронізується з SelectedBlock.CellStyles.
        private readonly ScheduleCellStyleStore _cellStyleStore = new();

        // Прапорець, який приглушує реакції на зміну SelectedAvailabilityGroup.
        // Використовується, коли ми програмно виставляємо group з SelectedBlock,
        // щоб НЕ інвалідити генерацію і не запускати зайві реакції.
        private bool _suppressAvailabilityGroupUpdate;

        // Поточний активний режим фарбування клітинок.
        private SchedulePaintMode _activePaintMode = SchedulePaintMode.None;

        // Порожні колекції, щоб не повертати null у ScheduleEmployees/ScheduleSlots.
        // Це спрощує binding у WPF (не треба перевіряти null).
        private static readonly ObservableCollection<ScheduleEmployeeModel> EmptyScheduleEmployees = new();
        private static readonly ObservableCollection<ScheduleSlotModel> EmptyScheduleSlots = new();

        // Останній вибраний колір фону (ARGB як int), який користувач застосовує на клітинки.
        private int? _lastFillColorArgb;

        // Останній вибраний колір тексту (ARGB як int).
        private int? _lastTextColorArgb;

        // Cached brush для останнього fill color (щоб UI міг показати “квадратик кольору”).
        private Brush? _lastFillBrush;

        // Cached brush для останнього text color.
        private Brush? _lastTextBrush;

        // Одна конкретна “активна” клітинка (клік), якщо тобі треба мати 1 вибрану.
        private ScheduleMatrixCellRef? _selectedCellRef;

        // Totals: кількість працівників у блоці (показ в UI).
        private int _totalEmployees;

        // Totals: загальні години (показ в UI).
        private string _totalHoursText = "0h 0m";

        // Поточний відкритий/вибраний ScheduleBlock у формі.
        // Це головна “точка істини”: її Model/Employees/Slots і є редаговані дані.
        private ScheduleBlockViewModel? _selectedBlock;

        // Режим форми: редагуємо існуючий розклад (true) чи створюємо новий (false).
        private bool _isEdit;

        // Вибраний shop у UI. Важливо: це окремо від ScheduleShopId в моделі.
        // При зміні SelectedShop запускається debounce -> ScheduleShopId = ...
        private ShopModel? _selectedShop;

        // “Pending” вибір shop (як проміжний стан для confirm).
        private ShopModel? _pendingSelectedShop;

        // Вибрана availability group у UI.
        private AvailabilityGroupModel? _selectedAvailabilityGroup;

        // “Pending” вибір availability group.
        private AvailabilityGroupModel? _pendingSelectedAvailabilityGroup;

        // Вибраний schedule-employee (зв’язка працівника з schedule), наприклад для remove.
        private ScheduleEmployeeModel? _selectedScheduleEmployee;

        // Тексти пошуку (з UI).
        private string _shopSearchText = string.Empty;
        private string _availabilitySearchText = string.Empty;
        private string _employeeSearchText = string.Empty;

        // Матриця розкладу (DataView для WPF DataGrid).
        private DataView _scheduleMatrix = new DataView();

        // Preview-матриця доступності (окремий DataView).
        private DataView _availabilityPreviewMatrix = new DataView();

        // Ревізія стилів клітинок: інкремент — сигнал UI, що стилі змінилися.
        private int _cellStyleRevision;

        // Кеш кистей по ARGB, щоб не створювати SolidColorBrush 1000 разів.
        private readonly Dictionary<int, Brush> _brushCache = new();

        // -------------------------------------------------------
        // 4) INotifyDataErrorInfo: ПРОКСІ НА _validation
        // -------------------------------------------------------

        // Подія: WPF підписується, щоб реагувати на зміну помилок для властивості.
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        private void Validation_ErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
        {
            // Перепіднімаємо подію від імені VM (sender = this)
            ErrorsChanged?.Invoke(this, e);
        }

        // Чи є хоч одна помилка у формі.
        public bool HasErrors => _validation.HasErrors;

        // Повернути помилки:
        // - якщо propertyName null => всі помилки
        // - якщо propertyName заданий => помилки тільки для цього поля
        public System.Collections.IEnumerable GetErrors(string? propertyName)
            => _validation.GetErrors(propertyName);

        // --------------------------
        // 5) UI-ПОХІДНІ ВЛАСТИВОСТІ
        // --------------------------

        // Можна додавати блок тільки якщо не редагуємо (Add mode).
        public bool CanAddBlock => !IsEdit;

        // Заголовок форми в UI залежить від режиму.
        public string FormTitle => IsEdit ? "Edit Schedule" : "Add Schedule";

        // Підзаголовок форми залежить від режиму.
        public string FormSubtitle => IsEdit
            ? "Update schedule details and press Save."
            : "Fill the form and press Save.";

        // --------------------------
        // 6) КОЛЕКЦІЇ ДЛЯ UI
        // --------------------------

        // Виділені клітинки у матриці (multi-select).
        public ObservableCollection<ScheduleMatrixCellRef> SelectedCellRefs { get; } = new();

        // Всі відкриті блоки (розклади) в цій формі.
        public ObservableCollection<ScheduleBlockViewModel> Blocks { get; } = new();

        // Синонім (якщо UI/логіка очікує OpenedSchedules).
        public ObservableCollection<ScheduleBlockViewModel> OpenedSchedules => Blocks;

        // --------------------------
        // 7) PAINT MODE + ОСТАННІ КОЛЬОРИ
        // --------------------------

        // Поточний режим фарбування (None/Background/Foreground).
        public SchedulePaintMode ActivePaintMode
        {
            get => _activePaintMode;
            private set => SetProperty(ref _activePaintMode, value);
        }

        // Останній колір фону (ARGB).
        // Коли він змінюється — ми одразу перераховуємо Brush для UI.
        public int? LastFillColorArgb
        {
            get => _lastFillColorArgb;
            private set
            {
                // SetProperty робить:
                // - порівняння
                // - присвоєння
                // - OnPropertyChanged
                if (SetProperty(ref _lastFillColorArgb, value))
                    LastFillBrush = value.HasValue ? ToBrushCached(value.Value) : null; // ToBrushCached у CellStyling partial
            }
        }

        // Останній колір тексту (ARGB) + синхронізація Brush.
        public int? LastTextColorArgb
        {
            get => _lastTextColorArgb;
            private set
            {
                if (SetProperty(ref _lastTextColorArgb, value))
                    LastTextBrush = value.HasValue ? ToBrushCached(value.Value) : null;
            }
        }

        // Brush-и для UI (кнопки “останній колір” тощо).
        public Brush? LastFillBrush
        {
            get => _lastFillBrush;
            private set => SetProperty(ref _lastFillBrush, value);
        }

        public Brush? LastTextBrush
        {
            get => _lastTextBrush;
            private set => SetProperty(ref _lastTextBrush, value);
        }

        // Одна активна клітинка (якщо треба знати “поточну”).
        public ScheduleMatrixCellRef? SelectedCellRef
        {
            get => _selectedCellRef;
            set => SetProperty(ref _selectedCellRef, value);
        }

        // Totals (для UI).
        public int TotalEmployees
        {
            get => _totalEmployees;
            private set => SetProperty(ref _totalEmployees, value);
        }

        public string TotalHoursText
        {
            get => _totalHoursText;
            private set => SetProperty(ref _totalHoursText, value);
        }

        // --------------------------
        // 8) SELECTED BLOCK (ОСНОВНИЙ КОНТЕКСТ РЕДАГУВАННЯ)
        // --------------------------

        public ScheduleBlockViewModel? SelectedBlock
        {
            get => _selectedBlock;
            set
            {
                // SetProperty оновлює поле і піднімає PropertyChanged (для SelectedBlock).
                if (SetProperty(ref _selectedBlock, value))
                {
                    // Коли блок змінюється, ми хочемо оновити всі залежні властивості,
                    // які фактично читають SelectedBlock.Model.* або SelectedBlock.Employees/Slots.
                    OnPropertiesChanged(
                        nameof(ScheduleId),
                        nameof(ScheduleContainerId),
                        nameof(ScheduleShopId),
                        nameof(ScheduleName),
                        nameof(ScheduleYear),
                        nameof(ScheduleMonth),
                        nameof(SchedulePeoplePerShift),
                        nameof(ScheduleShift1),
                        nameof(ScheduleShift2),
                        nameof(ScheduleMaxHoursPerEmp),
                        nameof(ScheduleMaxConsecutiveDays),
                        nameof(ScheduleMaxConsecutiveFull),
                        nameof(ScheduleMaxFullPerMonth),
                        nameof(ScheduleNote),
                        nameof(ScheduleEmployees),
                        nameof(SelectedAvailabilityGroup));

                    // ActiveSchedule — alias на SelectedBlock, теж треба оновити.
                    OnPropertyChanged(nameof(ActiveSchedule));

                    // 1) Синхронізуємо SelectedShop/SelectedAvailabilityGroup з даними блоку.
                    //    Цей метод винесений в Lookups/Selection partial.
                    SyncSelectionFromBlock();

                    // 2) Скидаємо матриці/preview/тотали для нового блоку.
                    //    Це метод з Matrix partial.
                    RestoreMatricesForSelection();

                    // 3) Завантажуємо/індексуємо стилі клітинок для нового блоку.
                    //    (CellStyling partial)
                    RefreshCellStyleMap();

                    // 4) Очищаємо виділення клітинок
                    SelectedCellRefs.Clear();
                    SelectedCellRef = null;
                }
            }
        }

        // Alias на SelectedBlock (інколи UI прив’язаний до ActiveSchedule).
        public ScheduleBlockViewModel? ActiveSchedule
        {
            get => SelectedBlock;
            set => SelectedBlock = value;
        }

        // --------------------------
        // 9) РЕЖИМ ФОРМИ: ADD / EDIT
        // --------------------------

        public bool IsEdit
        {
            get => _isEdit;
            set
            {
                if (SetProperty(ref _isEdit, value))
                    OnPropertiesChanged(nameof(FormTitle), nameof(FormSubtitle), nameof(CanAddBlock));
            }
        }

        // --------------------------
        // 10) ПРОКСІ-ВЛАСТИВОСТІ НА ScheduleModel (SelectedBlock.Model.*)
        // --------------------------
        //
        // Ці властивості — те, з чим працює UI (TextBox/ComboBox).
        // Вони не зберігають значення в полях VM, а напряму читають/пишуть в SelectedBlock.Model.
        //
        // Перевага:
        // - джерело правди одне: ScheduleModel.
        // - UI завжди показує актуальні дані обраного блоку.
        //
        // Недолік:
        // - треба акуратно обробляти SelectedBlock == null (тому скрізь ?? 0/"")

        public int ScheduleId
        {
            get => SelectedBlock?.Model.Id ?? 0;
            set
            {
                if (SelectedBlock is null) return;
                if (SelectedBlock.Model.Id == value) return;
                SelectedBlock.Model.Id = value;
                OnPropertyChanged();
            }
        }

        public int ScheduleContainerId
        {
            get => SelectedBlock?.Model.ContainerId ?? 0;
            set
            {
                if (SelectedBlock is null) return;
                if (SelectedBlock.Model.ContainerId == value) return;
                SelectedBlock.Model.ContainerId = value;
                OnPropertyChanged();
            }
        }

        // Далі більшість властивостей встановлюються через SetScheduleValue(...).
        // SetScheduleValue — універсальний метод (винесений у ModelBinding partial),
        // який робить:
        // - перевірку змін
        // - запис у модель
        // - OnPropertyChanged
        // - очистку помилок + inline-валидацію
        // - за потреби InvalidateGeneratedSchedule (наприклад коли змінився Month/Year)

        public int ScheduleShopId
        {
            get => SelectedBlock?.Model.ShopId ?? 0;
            set => SetScheduleValue(
                value,
                m => m.ShopId,
                (m, v) => m.ShopId = v);
        }

        public string ScheduleName
        {
            get => SelectedBlock?.Model.Name ?? string.Empty;
            set => SetScheduleValue(
                value,
                m => m.Name ?? string.Empty,
                (m, v) => m.Name = v);
        }

        public int ScheduleYear
        {
            get => SelectedBlock?.Model.Year ?? 0;
            set
            {
                if (SetScheduleValue(
                        value,
                        m => m.Year,
                        (m, v) => m.Year = v,
                        invalidateGenerated: true))
                {
                    OnSchedulePeriodChanged();
                }
            }
        }

        public int ScheduleMonth
        {
            get => SelectedBlock?.Model.Month ?? 0;
            set
            {
                if (SetScheduleValue(
                        value,
                        m => m.Month,
                        (m, v) => m.Month = v,
                        invalidateGenerated: true))
                {
                    OnSchedulePeriodChanged();
                }
            }
        }

        public int SchedulePeoplePerShift
        {
            get => SelectedBlock?.Model.PeoplePerShift ?? 0;
            set => SetScheduleValue(
                value,
                m => m.PeoplePerShift,
                (m, v) => m.PeoplePerShift = v);
        }

        public string ScheduleShift1
        {
            get => SelectedBlock?.Model.Shift1Time ?? string.Empty;
            set => SetScheduleValue(
                value,
                m => m.Shift1Time ?? string.Empty,
                (m, v) => m.Shift1Time = v);
        }

        public string ScheduleShift2
        {
            get => SelectedBlock?.Model.Shift2Time ?? string.Empty;
            set => SetScheduleValue(
                value,
                m => m.Shift2Time ?? string.Empty,
                (m, v) => m.Shift2Time = v);
        }

        public int ScheduleMaxHoursPerEmp
        {
            get => SelectedBlock?.Model.MaxHoursPerEmpMonth ?? 0;
            set => SetScheduleValue(
                value,
                m => m.MaxHoursPerEmpMonth,
                (m, v) => m.MaxHoursPerEmpMonth = v);
        }

        public int ScheduleMaxConsecutiveDays
        {
            get => SelectedBlock?.Model.MaxConsecutiveDays ?? 0;
            set => SetScheduleValue(
                value,
                m => m.MaxConsecutiveDays,
                (m, v) => m.MaxConsecutiveDays = v);
        }

        public int ScheduleMaxConsecutiveFull
        {
            get => SelectedBlock?.Model.MaxConsecutiveFull ?? 0;
            set => SetScheduleValue(
                value,
                m => m.MaxConsecutiveFull,
                (m, v) => m.MaxConsecutiveFull = v);
        }

        public int ScheduleMaxFullPerMonth
        {
            get => SelectedBlock?.Model.MaxFullPerMonth ?? 0;
            set => SetScheduleValue(
                value,
                m => m.MaxFullPerMonth,
                (m, v) => m.MaxFullPerMonth = v);
        }

        public string ScheduleNote
        {
            get => SelectedBlock?.Model.Note ?? string.Empty;
            set => SetScheduleValue(
                value,
                m => m.Note ?? string.Empty,
                (m, v) => m.Note = v);
        }

        // Колекції слотов/працівників поточного блоку.
        public ObservableCollection<ScheduleEmployeeModel> ScheduleEmployees
            => SelectedBlock?.Employees ?? EmptyScheduleEmployees;

        public ObservableCollection<ScheduleSlotModel> ScheduleSlots
            => SelectedBlock?.Slots ?? EmptyScheduleSlots;

        // --------------------------
        // 11) LOOKUPS (ДОВІДНИКИ) ДЛЯ UI
        // --------------------------
        //
        // Ці колекції наповнює owner (пошук) і VM через SetLookups/SetShops/SetEmployees.
        public ObservableCollection<ShopModel> Shops { get; } = new();
        public ObservableCollection<AvailabilityGroupModel> AvailabilityGroups { get; } = new();
        public ObservableCollection<EmployeeModel> Employees { get; } = new();

        // --------------------------
        // 12) SELECTED SHOP / AVAILABILITY GROUP (UI selection)
        // --------------------------

        public ShopModel? SelectedShop
        {
            get => _selectedShop;
            set
            {
                // Порівнюємо id до і після, щоб не тригерити реакцію без зміни суті.
                var oldId = _selectedShop?.Id ?? 0;
                var newId = value?.Id ?? 0;

                // Якщо selected shop не змінився (reference/Equals) — виходимо.
                if (!SetProperty(ref _selectedShop, value))
                    return;

                // PendingSelectedShop тримає останній вибір (навіть якщо він ще не “закомічений”).
                PendingSelectedShop = value;

                // _selectionSyncDepth > 0 означає: зараз ми програмно синхронізуємо selection,
                // і НЕ хочемо запускати debounce/реакції. (EnterSelectionSync в Lookups partial)
                if (_selectionSyncDepth > 0)
                    return;

                // Якщо id не змінився — нічого не робимо.
                if (oldId == newId)
                    return;

                // Реакція на зміну selection винесена в Selection.cs:
                // там буде debounce і потім ScheduleShopId = newId.
                ScheduleShopSelectionChange(newId);
            }
        }

        public ShopModel? PendingSelectedShop
        {
            get => _pendingSelectedShop;
            set
            {
                SetProperty(ref _pendingSelectedShop, value);

                ClearValidationErrors(nameof(PendingSelectedShop));
                ClearValidationErrors(nameof(ScheduleShopId));
                ClearValidationErrors(nameof(SelectedShop)); // ← додати
            }
        }


        public AvailabilityGroupModel? SelectedAvailabilityGroup
        {
            get => _selectedAvailabilityGroup;
            set
            {
                var oldId = _selectedAvailabilityGroup?.Id ?? 0;
                var newId = value?.Id ?? 0;

                if (!SetProperty(ref _selectedAvailabilityGroup, value))
                    return;

                PendingSelectedAvailabilityGroup = value;

                // Якщо ми програмно виставляємо групу і не хочемо реакцій — виходимо.
                if (_suppressAvailabilityGroupUpdate)
                    return;

                if (oldId == newId)
                    return;

                // Без SelectedBlock немає куди записати SelectedAvailabilityGroupId.
                if (SelectedBlock == null)
                    return;

                // Якщо йде службова синхронізація selection — не тригеримо реакції.
                if (_selectionSyncDepth > 0)
                    return;

                // Реакція (debounce + інвалідація) винесена в Selection.cs.
                ScheduleAvailabilitySelectionChange(newId);
            }
        }

        public AvailabilityGroupModel? PendingSelectedAvailabilityGroup
        {
            get => _pendingSelectedAvailabilityGroup;
            set
            {
                SetProperty(ref _pendingSelectedAvailabilityGroup, value);
                ClearValidationErrors(nameof(PendingSelectedAvailabilityGroup));
            }
        }

        // --------------------------
        // 13) ВИБРАНИЙ EMPLOYEE (ДОВІДНИК) І ВИБРАНИЙ SCHEDULE-EMPLOYEE (В БЛОЦІ)
        // --------------------------

        private EmployeeModel? _selectedEmployee;
        public EmployeeModel? SelectedEmployee
        {
            get => _selectedEmployee;
            set => SetProperty(ref _selectedEmployee, value);
        }

        public ScheduleEmployeeModel? SelectedScheduleEmployee
        {
            get => _selectedScheduleEmployee;
            set => SetProperty(ref _selectedScheduleEmployee, value);
        }

        // --------------------------
        // 14) ПОШУКОВІ РЯДКИ
        // --------------------------

        public string ShopSearchText
        {
            get => _shopSearchText;
            set => SetProperty(ref _shopSearchText, value);
        }

        public string AvailabilitySearchText
        {
            get => _availabilitySearchText;
            set => SetProperty(ref _availabilitySearchText, value);
        }

        public string EmployeeSearchText
        {
            get => _employeeSearchText;
            set => SetProperty(ref _employeeSearchText, value);
        }

        // --------------------------
        // 15) МАТРИЦІ (DataView) ДЛЯ WPF DataGrid
        // --------------------------

        public DataView ScheduleMatrix
        {
            get => _scheduleMatrix;
            private set => SetProperty(ref _scheduleMatrix, value);
        }

        public DataView AvailabilityPreviewMatrix
        {
            get => _availabilityPreviewMatrix;
            private set => SetProperty(ref _availabilityPreviewMatrix, value);
        }

        // --------------------------
        // 16) РЕВІЗІЯ СТИЛІВ
        // --------------------------

        public int CellStyleRevision
        {
            get => _cellStyleRevision;
            private set => SetProperty(ref _cellStyleRevision, value);
        }

        // --------------------------
        // 17) КОМАНДИ (BUTTON COMMANDS)
        // --------------------------
        //
        // AsyncRelayCommand — асинхронні операції (Save/Generate/Search/…)
        // RelayCommand — синхронні операції (фарбування/очистка стилів)
        public AsyncRelayCommand SaveCommand { get; }
        public AsyncRelayCommand CancelCommand { get; }
        public AsyncRelayCommand GenerateCommand { get; }
        public AsyncRelayCommand AddBlockCommand { get; }
        public AsyncRelayCommand SearchShopCommand { get; }
        public AsyncRelayCommand SearchAvailabilityCommand { get; }
        public AsyncRelayCommand SearchEmployeeCommand { get; }
        public AsyncRelayCommand AddEmployeeCommand { get; }
        public AsyncRelayCommand RemoveEmployeeCommand { get; }
        public RelayCommand<ScheduleMatrixCellRef?> SetCellBackgroundColorCommand { get; }
        public RelayCommand<ScheduleMatrixCellRef?> SetCellTextColorCommand { get; }
        public RelayCommand<ScheduleMatrixCellRef?> ClearCellFormattingCommand { get; }
        public RelayCommand<ScheduleMatrixCellRef?> ClearSelectedCellStyleCommand { get; }
        public RelayCommand ClearAllScheduleStylesCommand { get; }
        public RelayCommand<ScheduleMatrixCellRef?> ApplyLastFillColorCommand { get; }
        public RelayCommand<ScheduleMatrixCellRef?> ApplyLastTextColorCommand { get; }
        public RelayCommand PickFillColorCommand { get; }
        public RelayCommand PickTextColorCommand { get; }

        // Подія: сигнал для UI/слухачів, що матриця змінилась (refresh/edit/invalidate).
        public event EventHandler? MatrixChanged;

        // --------------------------
        // 18) КОНСТРУКТОР
        // --------------------------
        public ContainerScheduleEditViewModel(ContainerViewModel owner, IAvailabilityGroupService availabilityGroupService,
            IEmployeeService employeeService)
        {
            _availabilityGroupService = availabilityGroupService;
            _employeeService = employeeService;
            _owner = owner;
            _validation.ErrorsChanged += Validation_ErrorsChanged;

            // Дебаунсери визначені в Selection.cs (partial),
            // але ініціалізуються тут у конструкторі.
            // SelectionDebounceDelay теж визначена там.
            _shopSelectionDebounce = new UiDebouncedAction(_owner.RunOnUiThreadAsync, SelectionDebounceDelay);
            _availabilitySelectionDebounce = new UiDebouncedAction(_owner.RunOnUiThreadAsync, SelectionDebounceDelay);

            // Save/Generate обгорнуті у валідацію (SaveWithValidationAsync/GenerateWithValidationAsync у Validation partial).
            SaveCommand = new AsyncRelayCommand(SaveWithValidationAsync);

            // Cancel — делегуємо owner, бо саме owner управляє “закриттям/відміною”.
            CancelCommand = new AsyncRelayCommand(() => _owner.CancelScheduleAsync());

            GenerateCommand = new AsyncRelayCommand(GenerateWithValidationAsync);

            // Add block — створює новий ScheduleBlock (логіка в owner).
            AddBlockCommand = new AsyncRelayCommand(() => _owner.AddScheduleBlockAsync());

            // Пошуки — теж через owner (бо він знає як і звідки підвантажувати дані).
            SearchShopCommand = new AsyncRelayCommand(() => _owner.SearchScheduleShopsAsync());
            SearchAvailabilityCommand = new AsyncRelayCommand(() => _owner.SearchScheduleAvailabilityAsync());
            SearchEmployeeCommand = new AsyncRelayCommand(() => _owner.SearchScheduleEmployeesAsync());

            // Додавання/видалення працівника зі schedule — знову ж таки делегуємо owner.
            AddEmployeeCommand = new AsyncRelayCommand(() => _owner.AddScheduleEmployeeAsync());
            RemoveEmployeeCommand = new AsyncRelayCommand(() => _owner.RemoveScheduleEmployeeAsync());

            // Команди форматування клітинок:
            // Реальна логіка SetCellBackgroundColor/Clear... знаходиться в CellStyling.cs partial.
            SetCellBackgroundColorCommand = new RelayCommand<ScheduleMatrixCellRef?>(SetCellBackgroundColor);
            SetCellTextColorCommand = new RelayCommand<ScheduleMatrixCellRef?>(SetCellTextColor);
            ClearCellFormattingCommand = new RelayCommand<ScheduleMatrixCellRef?>(ClearCellFormatting);
            ClearSelectedCellStyleCommand = new RelayCommand<ScheduleMatrixCellRef?>(ClearSelectedCellStyles);
            ClearAllScheduleStylesCommand = new RelayCommand(ClearAllScheduleStyles);
            ApplyLastFillColorCommand = new RelayCommand<ScheduleMatrixCellRef?>(ApplyLastFillColor);
            ApplyLastTextColorCommand = new RelayCommand<ScheduleMatrixCellRef?>(ApplyLastTextColor);
            PickFillColorCommand = new RelayCommand(PickFillColor);
            PickTextColorCommand = new RelayCommand(PickTextColor);
        }

        // --------------------------
        // 19) НАВІГАЦІЯ ПО БЛОКАХ (OWNER DELEGATION)
        // --------------------------

        // Обрати блок (відкрити для редагування) — делегуємо owner.
        public Task SelectBlockAsync(ScheduleBlockViewModel block)
            => _owner.SelectScheduleBlockAsync(block);

        // Закрити блок (прибрати вкладку/картку) — делегуємо owner.
        public Task CloseBlockAsync(ScheduleBlockViewModel block)
            => _owner.CloseScheduleBlockAsync(block);

        // --------------------------
        // 20) RESET STATE (ПОВНИЙ СКИДАННЯ ФОРМИ)
        // --------------------------
        public void ResetForNew()
        {
            // 1) Очищаємо відкриті блоки і вибір
            Blocks.Clear();
            SelectedBlock = null;

            // 2) Ставимо форму в режим “Add”
            IsEdit = false;

            // 3) Очищаємо всі помилки валідації (через ValidationErrors)
            ClearValidationErrors();

            // 4) Очищаємо текст пошуку
            ShopSearchText = string.Empty;
            AvailabilitySearchText = string.Empty;
            EmployeeSearchText = string.Empty;

            // 5) Скидаємо selection'и в UI
            SelectedShop = null;
            SelectedAvailabilityGroup = null;
            SelectedEmployee = null;
            SelectedScheduleEmployee = null;

            // 6) Скидаємо матриці (щоб UI не показував старі дані)
            ScheduleMatrix = new DataView();
            AvailabilityPreviewMatrix = new DataView();

            // _availabilityPreviewKey — поле з Matrix partial; тут ми його обнуляємо,
            // щоб preview не вважався “актуальним”.
            _availabilityPreviewKey = null;

            // 7) Скидаємо стилі клітинок (порожній store)
            _cellStyleStore.Load(Array.Empty<ScheduleCellStyleModel>());
            CellStyleRevision++; // сигнал для UI: стилі змінились

            // 8) Скидаємо виділення клітинок і paint mode
            SelectedCellRef = null;
            SelectedCellRefs.Clear();
            ActivePaintMode = SchedulePaintMode.None;

            // 9) Чистимо кеш кистей (ARGB->Brush), щоб не тягнути “старе” між сесіями
            _brushCache.Clear();

            // 10) Скасовуємо відкладені debounce-дії (Selection.cs)
            CancelSelectionDebounce();
        }


        public async Task<(IReadOnlyList<EmployeeModel> employees,
                   IReadOnlyList<ScheduleSlotModel> availabilitySlots,
                   bool periodMatched)>
    GetAvailabilityPreviewAsync(
        int availabilityGroupId,
        int scheduleYear,
        int scheduleMonth,
        string? shift1Text,
        string? shift2Text,
        CancellationToken ct = default)
        {
            var (group, members, days) =
                await _availabilityGroupService.LoadFullAsync(availabilityGroupId, ct)
                                               .ConfigureAwait(false);

            // 1) Employees завжди (навіть якщо місяць/рік не збігаються)
            var empIds = members.Select(m => m.EmployeeId).Distinct().ToHashSet();

            // Якщо members вже include-ять Employee — беремо звідти, інакше добираємо через EmployeeService
            var empById = new Dictionary<int, EmployeeModel>();

            foreach (var m in members)
            {
                if (m.Employee != null)
                    empById[m.EmployeeId] = m.Employee;
            }

            if (empById.Count != empIds.Count)
            {
                var all = await _employeeService.GetAllAsync(ct).ConfigureAwait(false);
                foreach (var e in all)
                {
                    if (empIds.Contains(e.Id))
                        empById[e.Id] = e;
                }
            }

            // проставляємо member.Employee (щоб AvailabilityPreviewBuilder міг правильно зібрати employees/slots)
            foreach (var m in members)
            {
                if (m.Employee == null && empById.TryGetValue(m.EmployeeId, out var e))
                    m.Employee = e;
            }

            var employees = empById.Values
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToList();

            // 2) Перевірка періоду (це твоя бізнес-вимога)
            var periodMatched = (group.Year == scheduleYear && group.Month == scheduleMonth);
            if (!periodMatched)
                return (employees, Array.Empty<ScheduleSlotModel>(), false);

            // 3) Будуємо availability slots для preview (ANY -> shift1/shift2)
            var shift1 = ParseShift(shift1Text);
            var shift2 = ParseShift(shift2Text);

            var (_, slots) = AvailabilityPreviewBuilder.Build(
                members,
                days,
                shift1,
                shift2,
                ct);

            return (employees, slots, true);
        }

        private static (string from, string to)? ParseShift(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            if (!AvailabilityCodeParser.TryNormalizeInterval(text, out var normalized))
                return null;

            var parts = normalized.Split('-', 2,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return parts.Length == 2 ? (parts[0], parts[1]) : null;
        }


    }
}
