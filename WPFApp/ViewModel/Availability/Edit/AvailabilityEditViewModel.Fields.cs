using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using WPFApp.Applications.Matrix.Availability;
using WPFApp.MVVM.Core;
using WPFApp.MVVM.Validation;
using WPFApp.ViewModel.Availability.Helpers;
using WPFApp.ViewModel.Availability.Main;

namespace WPFApp.ViewModel.Availability.Edit
{
    /// <summary>
    /// Поля і властивості (стан VM):
    /// - власник (owner)
    /// - таблиця матриці і view
    /// - selections
    /// - form fields
    /// - колекції для UI (Employees/Binds)
    /// - кеши/мапи
    /// - прапорці батчингу/рекурсії
    /// </summary>
    public sealed partial class AvailabilityEditViewModel : ViewModelBase
    {
        // Day column name — єдиний центр правди (через engine).
        private const string DayColumnName = AvailabilityMatrixEngine.DayColumnName;

        // Owner — координатор екрану (Save/Cancel/UI messages/bind operations).
        private readonly AvailabilityViewModel _owner;

        // Контейнер помилок для INotifyDataErrorInfo (реалізація — у Validation partial).
        private readonly ValidationErrors _validation = new();

        // Основна таблиця матриці.
        private readonly DataTable _groupTable = new();

        // Те, що байндиться в DataGrid: DataView поверх DataTable.
        public DataView AvailabilityDays => _groupTable.DefaultView;

        // Map employeeId -> columnName ("emp_12") для зв’язку UI (Id) з DataTable (назва колонки).
        private readonly Dictionary<int, string> _employeeIdToColumn = new();

        // Lookup employeeId -> displayName (щоб caption колонки був зрозумілий користувачу).
        private readonly Dictionary<int, string> _employeeNames = new();

        // Кеш активних bind-ів: normalizedKey -> bindValue.
        private readonly Dictionary<string, string> _activeBindCache = new(StringComparer.OrdinalIgnoreCase);

        // Прапорець “кеш треба перебудувати”.
        private bool _bindCacheDirty = true;

        // Підписки на PropertyChanged BindRow, щоб будь-які зміни BindRow “робили кеш dirty”.
        private readonly Dictionary<BindRow, PropertyChangedEventHandler> _bindRowHandlers = new();

        // Батчинг нотифікацій матриці (MatrixChanged + PropertyChanged(nameof(AvailabilityDays))).
        private int _matrixUpdateDepth;
        private bool _pendingMatrixChanged;

        // Захист від рекурсії у ColumnChanged (коли ми самі переприсвоюємо нормалізоване значення).
        private bool _suppressColumnChangedHandler;

        // Батчинг зміни дати (Year+Month разом) — щоб не регенерувати рядки двічі.
        private int _dateSyncDepth;

        // Колекції для UI.
        public ObservableCollection<EmployeeListItem> Employees { get; } = new();
        public ObservableCollection<BindRow> Binds { get; } = new();

        // ----------------------------
        // Selections (UI state)
        // ----------------------------

        private EmployeeListItem? _selectedEmployee;

        public EmployeeListItem? SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                // 1) Пишемо в backing field + викликаємо PropertyChanged.
                // 2) Якщо значення реально змінилось — оновлюємо залежну властивість.
                if (SetProperty(ref _selectedEmployee, value))
                {
                    // SelectedEmployeeId залежить від SelectedEmployee, тому теж оновлюємо.
                    OnPropertyChanged(nameof(SelectedEmployeeId));
                }
            }
        }

        public string SelectedEmployeeId =>
            // Якщо SelectedEmployee є і Id > 0 — повертаємо string(Id)
            SelectedEmployee?.Id > 0
                ? SelectedEmployee.Id.ToString()
                // Інакше — пусто (щоб UI не показував “0”).
                : string.Empty;

        private BindRow? _selectedBind;

        public BindRow? SelectedBind
        {
            get => _selectedBind;
            set => SetProperty(ref _selectedBind, value);
        }

        private object? _selectedAvailabilityDay;

        public object? SelectedAvailabilityDay
        {
            get => _selectedAvailabilityDay;
            set => SetProperty(ref _selectedAvailabilityDay, value);
        }

        // ----------------------------
        // Form fields (Information tab)
        // ----------------------------

        private int _availabilityId;

        public int AvailabilityId
        {
            get => _availabilityId;
            set => SetProperty(ref _availabilityId, value);
        }

        private int _availabilityMonth;

        public int AvailabilityMonth
        {
            get => _availabilityMonth;
            set
            {
                // 1) Якщо значення не змінилось — нічого не робимо.
                if (!SetProperty(ref _availabilityMonth, value))
                    return;

                // 2) При зміні — чистимо попередні помилки саме цього поля.
                ClearValidationErrors(nameof(AvailabilityMonth));

                // 3) Далі — перевіряємо поле за rules (деталі — у Validation partial).
                ValidateProperty(nameof(AvailabilityMonth));

                // 4) Якщо зараз “пакетно” виставляємо Year+Month — не регенеруємо рядки тут.
                if (_dateSyncDepth > 0)
                    return;

                // 5) Інакше синхронізуємо кількість day rows з новим month/year.
                RegenerateGroupDays();
            }
        }

        private int _availabilityYear;

        public int AvailabilityYear
        {
            get => _availabilityYear;
            set
            {
                // 1) Якщо значення не змінилось — виходимо.
                if (!SetProperty(ref _availabilityYear, value))
                    return;

                // 2) Чистимо помилки поля.
                ClearValidationErrors(nameof(AvailabilityYear));

                // 3) Валідуємо.
                ValidateProperty(nameof(AvailabilityYear));

                // 4) Якщо в date-sync scope — не робимо regen зараз.
                if (_dateSyncDepth > 0)
                    return;

                // 5) Інакше — регенерація рядків.
                RegenerateGroupDays();
            }
        }

        private string _availabilityName = string.Empty;

        public string AvailabilityName
        {
            get => _availabilityName;
            set
            {
                // 1) Оновлюємо backing field.
                if (!SetProperty(ref _availabilityName, value))
                    return;

                // 2) Чистимо помилки поля.
                ClearValidationErrors(nameof(AvailabilityName));

                // 3) Валідуємо.
                ValidateProperty(nameof(AvailabilityName));
            }
        }

        private string _employeeSearchText = string.Empty;

        public string EmployeeSearchText
        {
            get => _employeeSearchText;
            set => SetProperty(ref _employeeSearchText, value);
        }
    }
}
