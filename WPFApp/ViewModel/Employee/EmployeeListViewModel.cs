using BusinessLogicLayer.Contracts.Employees;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WPFApp.MVVM.Commands;
using WPFApp.MVVM.Core;

namespace WPFApp.ViewModel.Employee
{
    /// <summary>
    /// EmployeeListViewModel — VM для списку працівників.
    ///
    /// Відповідальність:
    /// - тримати Items (ObservableCollection) для DataGrid/ListView
    /// - тримати SelectedItem
    /// - тримати SearchText
    /// - надавати команди (Search/Add/Edit/Delete/Profile), які делегуються owner’у
    ///
    /// Оптимізації:
    /// 1) SetItems(...) не робить “зайвий Clear/Add”, якщо список фактично не змінився.
    /// 2) Після оновлення списку відновлюємо selection по Id.
    /// 3) RaiseCanExecuteChanged для selection-dependent команд викликається циклом (без дублювання).
    /// </summary>
    public sealed class EmployeeListViewModel : ViewModelBase
    {
        // Owner (EmployeeViewModel) виконує реальні дії (CRUD, навігацію).
        private readonly EmployeeViewModel _owner;

        // Колекція для UI.
        public ObservableCollection<EmployeeDto> Items { get; } = new();

        // Поточний вибраний елемент.
        private EmployeeDto? _selectedItem;

        public EmployeeDto? SelectedItem
        {
            get => _selectedItem;
            set
            {
                // Якщо selection реально змінився — оновлюємо CanExecute для залежних команд.
                if (SetProperty(ref _selectedItem, value))
                    UpdateSelectionCommands();
            }
        }

        // Текст пошуку.
        private string _searchText = string.Empty;

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        // Команди.
        public AsyncRelayCommand SearchCommand { get; }
        public AsyncRelayCommand AddNewCommand { get; }
        public AsyncRelayCommand EditCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }
        public AsyncRelayCommand OpenProfileCommand { get; }

        // Масив команд, що залежать від SelectedItem (щоб не дублювати RaiseCanExecuteChanged).
        private readonly AsyncRelayCommand[] _selectionDependentCommands;

        public EmployeeListViewModel(EmployeeViewModel owner)
        {
            // 1) Зберігаємо owner.
            _owner = owner;

            // 2) Команди, які доступні завжди.
            SearchCommand = new AsyncRelayCommand(() => _owner.SearchAsync());
            AddNewCommand = new AsyncRelayCommand(() => _owner.StartAddAsync());

            // 3) Команди, які залежать від selection.
            EditCommand = new AsyncRelayCommand(
                execute: () => _owner.EditSelectedAsync(),
                canExecute: () => HasSelection);

            DeleteCommand = new AsyncRelayCommand(
                execute: () => _owner.DeleteSelectedAsync(),
                canExecute: () => HasSelection);

            OpenProfileCommand = new AsyncRelayCommand(
                execute: () => _owner.OpenProfileAsync(),
                canExecute: () => HasSelection);

            // 4) Готуємо масив для оновлення CanExecute.
            _selectionDependentCommands = new[]
            {
                EditCommand,
                DeleteCommand,
                OpenProfileCommand
            };
        }

        // Єдине місце істини для “чи є вибір”.
        private bool HasSelection => SelectedItem != null;

        /// <summary>
        /// SetItems — встановити новий список працівників у Items.
        ///
        /// Оптимізація:
        /// - якщо список еквівалентний поточному (за ключовими полями) — нічого не робимо
        /// - якщо оновлюємо — відновлюємо selection по Id
        /// </summary>
        public void SetItems(IEnumerable<EmployeeDto> employees)
        {
            // 1) Null-safe.
            employees ??= Array.Empty<EmployeeDto>();

            // 2) Запам’ятовуємо поточний selectedId.
            var selectedId = SelectedItem?.Id ?? 0;

            // 3) Матеріалізуємо в IList, щоб не ітерувати кілька разів.
            if (employees is not IList<EmployeeDto> list)
                list = employees.ToList();

            // 4) Якщо список “той самий” — виходимо.
            if (IsSameList(Items, list))
                return;

            // 5) Перезаливаємо Items.
            Items.Clear();
            foreach (var e in list)
                Items.Add(e);

            // 6) Відновлюємо selection по Id.
            if (selectedId > 0)
                SelectedItem = Items.FirstOrDefault(x => x.Id == selectedId);
        }

        /// <summary>
        /// Порівняння “чи еквівалентний” список для UI.
        /// Порівнюємо по порядку (бо DataGrid показує порядок).
        /// </summary>
        private static bool IsSameList(IList<EmployeeDto> current, IList<EmployeeDto> next)
        {
            // 1) Різна кількість — різні.
            if (current.Count != next.Count)
                return false;

            // 2) Порівнюємо по індексах.
            for (int i = 0; i < next.Count; i++)
            {
                var a = current[i];
                var b = next[i];

                if (a is null || b is null)
                    return false;

                // Мінімальний ключ — Id.
                if (a.Id != b.Id)
                    return false;

                // Додатково — ключові поля, які відображаються/важливі для UI.
                if (!string.Equals(a.FirstName ?? string.Empty, b.FirstName ?? string.Empty, StringComparison.Ordinal))
                    return false;

                if (!string.Equals(a.LastName ?? string.Empty, b.LastName ?? string.Empty, StringComparison.Ordinal))
                    return false;

                if (!string.Equals(a.Email ?? string.Empty, b.Email ?? string.Empty, StringComparison.Ordinal))
                    return false;

                if (!string.Equals(a.Phone ?? string.Empty, b.Phone ?? string.Empty, StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        private void UpdateSelectionCommands()
        {
            // 1) Оновлюємо CanExecute у всіх selection-dependent командах.
            for (int i = 0; i < _selectionDependentCommands.Length; i++)
                _selectionDependentCommands[i].RaiseCanExecuteChanged();
        }
    }
}
