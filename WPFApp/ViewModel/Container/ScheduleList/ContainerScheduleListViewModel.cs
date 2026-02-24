using DataAccessLayer.Models;                 // ScheduleModel, ShopModel (дані з DAL)
using System;                                 // Action, EventHandler (для подій)
using System.Collections.ObjectModel;         // ObservableCollection для WPF binding
using System.Linq;                            // LINQ (Where/Select/Any/ToList)
using System.Windows.Input;                   // ICommand
using WPFApp.MVVM.Commands;
using WPFApp.MVVM.Core;
using WPFApp.ViewModel.Container.Edit;
using WPFApp.ViewModel.Dialogs;
using RelayCommand = WPFApp.MVVM.Commands.RelayCommand;               // (ймовірно Confirm/Dialogs через owner)

namespace WPFApp.ViewModel.Container.ScheduleList
{
    /// <summary>
    /// ScheduleRowVm — ViewModel одного рядка в таблиці (DataGrid) зі списком розкладів.
    ///
    /// Навіщо окремий Row VM:
    /// - ScheduleModel — це “дані” (DAL), він не зобов’язаний мати UI-стан.
    /// - Для UI нам потрібен додатковий стан, якого в моделі немає:
    ///   наприклад IsChecked (галочка для мультивідкриття).
    /// - Також зручно “прокинути” поля моделі у властивості (Name/Year/Month/Shop),
    ///   щоб XAML був простим і стабільним.
    /// </summary>
    public sealed class ScheduleRowVm : ViewModelBase
    {
        /// <summary>
        /// Оригінальна модель розкладу, яку представляє цей рядок.
        /// Це “джерело правди” для Name/Year/Month/Shop.
        /// </summary>
        public ScheduleModel Model { get; }

        public ScheduleRowVm(ScheduleModel model)
        {
            Model = model;
        }

        // Поля для відображення у DataGrid. Це просто проксі до Model.
        public string Name => Model.Name;
        public int Year => Model.Year;
        public int Month => Model.Month;
        public ShopModel? Shop => Model.Shop;

        /// <summary>
        /// Подія “галочка змінилася”.
        ///
        /// Чому event, а не Action-властивість:
        /// - event — стандартний .NET-підхід для “сповіщення про зміну”
        /// - неможливо випадково перезаписати попередній обробник (як у set Action)
        /// - легше керувати підпискою/відпискою, менший ризик витоків/дублів
        ///
        /// Хто підписується:
        /// - ContainerScheduleListViewModel, щоб оновити CanExecute для MultiOpenCommand.
        /// </summary>
        public event Action? CheckedChanged;

        /// <summary>
        /// IsChecked — прапорець “цей розклад обраний для мультивідкриття”.
        ///
        /// Це НЕ те саме, що SelectedItem (одиночний вибір рядка у DataGrid).
        /// - SelectedItem використовується для Edit/Delete/Profile (один розклад).
        /// - IsChecked використовується для MultiOpen (кілька розкладів).
        ///
        /// Коли IsChecked змінюється — ми піднімаємо CheckedChanged,
        /// щоб контейнерний VM міг одразу оновити доступність MultiOpenCommand.
        /// </summary>
        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                // SetProperty:
                // 1) порівнює old/new
                // 2) якщо змінилось — присвоює
                // 3) піднімає PropertyChanged(nameof(IsChecked))
                if (SetProperty(ref _isChecked, value))
                {
                    // Сповіщаємо “зовнішній” VM, що сталася зміна галочки.
                    // UI сам оновить чекбокс через PropertyChanged,
                    // а ми оновимо тільки доступність команди MultiOpen.
                    CheckedChanged?.Invoke();
                }
            }
        }
    }


    /// <summary>
    /// ContainerScheduleListViewModel — VM екрану “список розкладів”.
    ///
    /// Основні задачі:
    /// 1) Показати список розкладів (Items)
    /// 2) Дати змогу:
    ///    - шукати (SearchCommand)
    ///    - додати новий (AddNewCommand)
    ///    - редагувати вибраний (EditCommand)
    ///    - видалити вибраний (DeleteCommand)
    ///    - відкрити профіль (OpenProfileCommand)
    /// 3) Мультивідкриття:
    ///    - увімкнути режим (IsMultiOpenEnabled)
    ///    - поставити “галочки” (ScheduleRowVm.IsChecked)
    ///    - виконати MultiOpenCommand
    ///
    /// Принцип архітектури:
    /// - ListViewModel не ходить у БД/діалоги напряму.
    /// - Він делегує “важкі дії” в _owner (ContainerViewModel),
    ///   який знає як зберігати/шукати/відкривати.
    /// </summary>
    public sealed class ContainerScheduleListViewModel : ViewModelBase
    {
        /// <summary>
        /// Owner — керує реальними операціями (запити/навігація/діалоги).
        /// Тут ми лише викликаємо його методи.
        /// </summary>
        private readonly ContainerViewModel _owner;

        /// <summary>
        /// Items — рядки DataGrid.
        /// ObservableCollection потрібен, щоб UI автоматично реагував на зміни списку.
        /// </summary>
        public ObservableCollection<ScheduleRowVm> Items { get; } = new();

        /// <summary>
        /// SelectedItem — одиночний вибір у DataGrid.
        /// Потрібен для команд, які працюють з 1 розкладом (Edit/Delete/Profile).
        /// </summary>
        private ScheduleRowVm? _selectedItem;
        public ScheduleRowVm? SelectedItem
        {
            get => _selectedItem;
            set
            {
                // Коли вибір змінюється — оновлюємо доступність Edit/Delete/Profile команд.
                if (SetProperty(ref _selectedItem, value))
                    UpdateSelectionCommands();
            }
        }

        /// <summary>
        /// SearchText — текст пошуку.
        /// Важливо: у твоєму сценарії пошук запускається тільки по кнопці/Enter,
        /// тому тут немає debounce і немає автоматичного пошуку в setter.
        /// </summary>
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        /// <summary>
        /// Режим “мультивідкриття”.
        ///
        /// Коли false:
        /// - ми скидаємо всі галочки IsChecked = false (щоб не лишались “приховані вибори”)
        /// - MultiOpenCommand стає недоступною
        ///
        /// Коли true:
        /// - користувач може виставляти IsChecked
        /// - MultiOpenCommand стає доступною, якщо є хоча б одна галочка
        /// </summary>
        private bool _isMultiOpenEnabled;
        public bool IsMultiOpenEnabled
        {
            get => _isMultiOpenEnabled;
            set
            {
                if (_isMultiOpenEnabled == value)
                    return;

                _isMultiOpenEnabled = value;
                OnPropertyChanged(nameof(IsMultiOpenEnabled));

                // Якщо режим вимкнули — прибираємо всі галочки.
                // Це важливо, щоб:
                // - користувач не “наклікав” галочки
                // - вимкнув режим
                // - а потім через 5 хвилин увімкнув і не розумів, чому MultiOpen активна.
                if (!_isMultiOpenEnabled)
                {
                    foreach (var it in Items)
                        it.IsChecked = false;
                }

                // MultiOpenCommand залежить від:
                // - IsMultiOpenEnabled
                // - Items.Any(x => x.IsChecked)
                MultiOpenCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Команда для швидкого перемикання IsMultiOpenEnabled (кнопка/тогл в UI).
        /// </summary>
        public ICommand ToggleMultiOpenCommand { get; }

        /// <summary>
        /// Команда “відкрити кілька розкладів”.
        /// Вона активна лише коли:
        /// - IsMultiOpenEnabled == true
        /// - є хоч одна галочка IsChecked == true
        /// </summary>
        public AsyncRelayCommand MultiOpenCommand { get; }

        // Основні команди екрану списку:
        public AsyncRelayCommand SearchCommand { get; }
        public AsyncRelayCommand AddNewCommand { get; }
        public AsyncRelayCommand EditCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }
        public AsyncRelayCommand OpenProfileCommand { get; }

        public ContainerScheduleListViewModel(ContainerViewModel owner)
        {
            _owner = owner;

            // Команди делегують фактичну роботу owner’у:
            SearchCommand = new AsyncRelayCommand(() => _owner.SearchScheduleAsync());
            AddNewCommand = new AsyncRelayCommand(() => _owner.StartScheduleAddAsync());

            // Edit/Delete/OpenProfile дозволені тільки якщо SelectedItem != null
            EditCommand = new AsyncRelayCommand(() => _owner.EditSelectedScheduleAsync(), () => SelectedItem != null);
            DeleteCommand = new AsyncRelayCommand(() => _owner.DeleteSelectedScheduleAsync(), () => SelectedItem != null);
            OpenProfileCommand = new AsyncRelayCommand(() => _owner.OpenScheduleProfileAsync(), () => SelectedItem != null);

            // ToggleMultiOpenCommand просто перемикає режим
            ToggleMultiOpenCommand = new RelayCommand(() =>
            {
                IsMultiOpenEnabled = !IsMultiOpenEnabled;
            });

            // MultiOpenCommand:
            // 1) збирає всі Models, де рядок має IsChecked == true
            // 2) формує текст confirm-повідомлення з назвами
            // 3) питає підтвердження у користувача (_owner.Confirm)
            // 4) якщо підтверджено — викликає _owner.MultiOpenSchedulesAsync(...)
            //
            // canExecute:
            // - режим увімкнений
            // - є хоча б одна галочка
            MultiOpenCommand = new AsyncRelayCommand(
                async () =>
                {
                    // 1) Витягуємо вибрані моделі
                    var selectedModels = Items
                        .Where(x => x.IsChecked)
                        .Select(x => x.Model)
                        .ToList();

                    // 2) Якщо нічого не вибрано — нічого не робимо
                    if (selectedModels.Count == 0)
                        return;

                    // 3) Формуємо список назв для confirm-повідомлення
                    //    (включаючи fallback "(Unnamed schedule #n)")
                    var names = selectedModels
                        .Select((model, index) => FormatScheduleName(model, index + 1))
                        .Select(name => $"'{name}'")
                        .ToList();

                    // 4) Граматика: schedule / schedules
                    var noun = selectedModels.Count == 1 ? "schedule" : "schedules";
                    var message = $"Open {selectedModels.Count} {noun}: {string.Join(", ", names)}";

                    // 5) Confirm діалог (через owner)
                    if (!_owner.Confirm(message))
                        return;

                    // 6) Реальне відкриття (owner вирішує як саме відкривати кілька)
                    await _owner.MultiOpenSchedulesAsync(selectedModels);
                },
                canExecute: () =>
                    IsMultiOpenEnabled && Items.Any(x => x.IsChecked)
            );
        }

        /// <summary>
        /// Заповнити Items новим списком розкладів.
        ///
        /// Що важливо:
        /// - Ми відписуємось від подій CheckedChanged попередніх рядків,
        ///   щоб не тримати “висячі” посилання (пам’ять/подвійні виклики).
        /// - Потім чистимо Items і додаємо нові рядки.
        /// - Для кожного нового рядка підписуємось на CheckedChanged,
        ///   щоб MultiOpenCommand міг оновлювати CanExecute.
        /// </summary>
        public void SetItems(IEnumerable<ScheduleModel> schedules)
        {
            // 1) Відписуємось від старих рядків (безпека від витоків і дублю)
            foreach (var oldItem in Items)
                oldItem.CheckedChanged -= OnRowCheckedChanged;

            // 2) Очищаємо список
            Items.Clear();

            // 3) Створюємо нові рядки і підписуємось на їх подію
            foreach (var schedule in schedules)
            {
                var row = new ScheduleRowVm(schedule);
                row.CheckedChanged += OnRowCheckedChanged;
                Items.Add(row);
            }

            // 4) Оновлюємо доступність MultiOpenCommand після оновлення списку
            MultiOpenCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Обробник події CheckedChanged від будь-якого ScheduleRowVm.
        ///
        /// Єдина потрібна дія:
        /// - оновити доступність MultiOpenCommand
        ///   (бо canExecute залежить від Items.Any(x => x.IsChecked)).
        /// </summary>
        private void OnRowCheckedChanged()
        {
            MultiOpenCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Оновити доступність команд, які залежать від SelectedItem.
        /// Викликається в setter SelectedItem.
        /// </summary>
        private void UpdateSelectionCommands()
        {
            EditCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
            OpenProfileCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Перемкнути галочку конкретного рядка (якщо режим мультивідкриття увімкнений).
        ///
        /// Навіщо:
        /// - інколи UI роблять так, що клік по рядку не тільки виділяє,
        ///   а й ставить/знімає галочку (коли режим активний).
        ///
        /// Захист:
        /// - якщо IsMultiOpenEnabled == false, ми не дозволяємо міняти галочку,
        ///   щоб випадкові кліки не накопичували “прихований” вибір.
        /// </summary>
        public void ToggleRowSelection(ScheduleRowVm row)
        {
            if (!IsMultiOpenEnabled)
                return;

            row.IsChecked = !row.IsChecked;
        }

        /// <summary>
        /// Форматувати назву розкладу для confirm/списків:
        /// - якщо Name заданий — використовуємо його
        /// - якщо Name пустий/пробіли — показуємо fallback "(Unnamed schedule #n)"
        ///
        /// index — щоб fallback виглядав “людяно” у списку (1..N).
        /// </summary>
        private static string FormatScheduleName(ScheduleModel model, int index)
        {
            if (!string.IsNullOrWhiteSpace(model.Name))
                return model.Name;

            return $"(Unnamed schedule #{index})";
        }
    }
}
