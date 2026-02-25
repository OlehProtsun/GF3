using BusinessLogicLayer.Contracts.Shops;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WPFApp.MVVM.Commands;
using WPFApp.MVVM.Core;

namespace WPFApp.ViewModel.Shop
{
    /// <summary>
    /// ShopListViewModel — VM для списку магазинів.
    ///
    /// Відповідальність:
    /// - Items: ObservableCollection для DataGrid/ListView
    /// - SelectedItem: поточний вибір
    /// - SearchText: текст пошуку
    /// - Команди (Search/Add/Edit/Delete/OpenProfile), які делегуються owner’у
    ///
    /// Оптимізації:
    /// 1) SetItems(...) не робить Clear/Add якщо список фактично не змінився (UI не мерехтить).
    /// 2) Після оновлення списку відновлюємо SelectedItem по Id.
    /// 3) RaiseCanExecuteChanged для залежних команд робиться циклом (без дублювання).
    /// </summary>
    public sealed class ShopListViewModel : ViewModelBase
    {
        // Owner (ShopViewModel) виконує реальні дії (CRUD/навігацію).
        private readonly ShopViewModel _owner;

        /// <summary>
        /// Items — список магазинів для UI.
        /// </summary>
        public ObservableCollection<ShopDto> Items { get; } = new();

        private ShopDto? _selectedItem;

        /// <summary>
        /// SelectedItem — вибраний магазин.
        /// При зміні:
        /// - оновлюємо CanExecute для Edit/Delete/OpenProfile
        /// </summary>
        public ShopDto? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                    UpdateSelectionCommands();
            }
        }

        private string _searchText = string.Empty;

        /// <summary>
        /// SearchText — текст пошуку.
        /// </summary>
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

        // Масив команд, залежних від selection, щоб не дублювати RaiseCanExecuteChanged.
        private readonly AsyncRelayCommand[] _selectionDependentCommands;

        public ShopListViewModel(ShopViewModel owner)
        {
            // 1) Зберігаємо owner.
            _owner = owner;

            // 2) Команди, доступні завжди.
            SearchCommand = new AsyncRelayCommand(() => _owner.SearchAsync());
            AddNewCommand = new AsyncRelayCommand(() => _owner.StartAddAsync());

            // 3) Команди, що залежать від selection.
            EditCommand = new AsyncRelayCommand(() => _owner.EditSelectedAsync(), () => HasSelection);
            DeleteCommand = new AsyncRelayCommand(() => _owner.DeleteSelectedAsync(), () => HasSelection);
            OpenProfileCommand = new AsyncRelayCommand(() => _owner.OpenProfileAsync(), () => HasSelection);

            // 4) Для оновлення CanExecute циклом.
            _selectionDependentCommands = new[]
            {
                EditCommand,
                DeleteCommand,
                OpenProfileCommand
            };
        }

        private bool HasSelection => SelectedItem != null;

        /// <summary>
        /// SetItems — оновити Items.
        ///
        /// Оптимізація:
        /// - якщо список еквівалентний поточному (за ключовими полями) — виходимо
        /// - якщо оновлюємо — відновлюємо SelectedItem по Id
        /// </summary>
        public void SetItems(IEnumerable<ShopDto> shops)
        {
            // 1) Null-safe.
            shops ??= Array.Empty<ShopDto>();

            // 2) Запам’ятовуємо поточний selection.
            var selectedId = SelectedItem?.Id ?? 0;

            // 3) Матеріалізуємо в IList, щоб можна було порівнювати по індексу.
            if (shops is not IList<ShopDto> list)
                list = shops.ToList();

            // 4) Якщо “по суті” список не змінився — нічого не робимо.
            if (IsSameList(Items, list))
                return;

            // 5) Перезаливаємо Items.
            Items.Clear();
            foreach (var s in list)
                Items.Add(s);

            // 6) Відновлюємо selection, якщо було.
            if (selectedId > 0)
                SelectedItem = Items.FirstOrDefault(x => x.Id == selectedId);
        }

        private static bool IsSameList(IList<ShopDto> current, IList<ShopDto> next)
        {
            // 1) Якщо різна кількість — точно різні.
            if (current.Count != next.Count)
                return false;

            // 2) Порівняння по індексу (бо порядок у DataGrid важливий).
            for (int i = 0; i < next.Count; i++)
            {
                var a = current[i];
                var b = next[i];

                if (a is null || b is null)
                    return false;

                // 3) Id — основний ключ.
                if (a.Id != b.Id)
                    return false;

                // 4) Додатково порівнюємо ключові поля, що показуються/важливі для UI.
                if (!string.Equals(a.Name ?? string.Empty, b.Name ?? string.Empty, StringComparison.Ordinal))
                    return false;

                if (!string.Equals(a.Address ?? string.Empty, b.Address ?? string.Empty, StringComparison.Ordinal))
                    return false;

                if (!string.Equals(a.Description ?? string.Empty, b.Description ?? string.Empty, StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        private void UpdateSelectionCommands()
        {
            // 1) Оновлюємо CanExecute у всіх selection-dependent команд.
            for (int i = 0; i < _selectionDependentCommands.Length; i++)
                _selectionDependentCommands[i].RaiseCanExecuteChanged();
        }
    }
}
