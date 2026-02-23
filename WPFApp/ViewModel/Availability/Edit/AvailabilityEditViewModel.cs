using System;
using System.Collections.Specialized;
using System.Data;
using System.Reflection;
using WPFApp.Infrastructure;
using WPFApp.Infrastructure.AvailabilityMatrix;
using WPFApp.ViewModel.Availability.Main;

namespace WPFApp.ViewModel.Availability.Edit
{
    /// <summary>
    /// AvailabilityEditViewModel — головний “каркас” ViewModel.
    /// 
    /// Цей файл містить:
    /// - оголошення класу (partial)
    /// - event MatrixChanged
    /// - constructor і підписки на події
    /// 
    /// Інша логіка рознесена по partial-файлах:
    /// - Fields/Properties
    /// - Commands
    /// - Matrix (робота з даними матриці)
    /// - Validation (INotifyDataErrorInfo + валідація клітинок)
    /// - Binds (кеш, прив’язки, hotkeys)
    /// - Batching (батчинг матриці/дати)
    /// </summary>
    public sealed partial class AvailabilityEditViewModel
    {
        /// <summary>
        /// MatrixChanged — сигнал для UI/контролів:
        /// “структура або дані матриці змінилися”.
        /// </summary>
        public event EventHandler? MatrixChanged;

        /// <summary>
        /// Constructor: ініціалізує команди, матрицю і підписки.
        /// </summary>
        /// <param name="owner">Owner VM (вищий координатор екрану).</param>
        public AvailabilityEditViewModel(AvailabilityViewModel owner)
        {
            // 1) Зберігаємо owner — через нього делегуємо Save/Cancel/Binds/Filter/UI messages.
            _owner = owner;

            // 2) Створюємо команди (оголошення Command properties — у Commands partial).
            //    Save завжди через повну валідацію.
            SaveCommand = new AsyncRelayCommand(SaveWithValidationAsync);

            // 3) Cancel — делегуємо на owner (реальна навігація/закриття).
            CancelCommand = new AsyncRelayCommand(() => _owner.CancelAsync());

            // 4) В UI можуть бути декілька “Cancel” кнопок для різних секцій — усі ведуть на один CancelCommand.
            CancelInformationCommand = CancelCommand;
            CancelEmployeeCommand = CancelCommand;
            CancelBindCommand = CancelCommand;

            // 5) Команди дій з employees.
            AddEmployeeCommand = new AsyncRelayCommand(AddEmployeeAsync);
            RemoveEmployeeCommand = new AsyncRelayCommand(RemoveEmployeeAsync);
            SearchEmployeeCommand = new AsyncRelayCommand(SearchEmployeeAsync);

            // 6) Команди дій з binds — делегуємо на owner (owner знає як їх зберігати).
            AddBindCommand = new AsyncRelayCommand(() => _owner.AddBindAsync());
            DeleteBindCommand = new AsyncRelayCommand(() => _owner.DeleteBindAsync());

            // 7) Забезпечуємо наявність Day column (DayOfMonth) у DataTable матриці.
            //    Вся структура таблиці — через engine, щоб VM не дублював табличну логіку.
            AvailabilityMatrixEngine.EnsureDayColumn(_groupTable);

            // 8) Підписка на ColumnChanged:
            //    - коли користувач редагує клітинку у DataGrid,
            //      це виливається у зміну DataTable,
            //      і ми можемо нормалізувати/валідувати значення клітинки.
            _groupTable.ColumnChanged += GroupTable_ColumnChanged;

            // 9) Підписка на зміну колекції bind-ів:
            //    - щоб тримати кеш актуальним
            //    - і коректно підписуватися/відписуватися на PropertyChanged елементів
            Binds.CollectionChanged += Binds_CollectionChanged;
        }
    }
}
