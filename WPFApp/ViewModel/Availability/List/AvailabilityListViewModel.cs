using BusinessLogicLayer.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WPFApp.MVVM.Commands;
using WPFApp.MVVM.Core;
using WPFApp.ViewModel.Availability.Main;

namespace WPFApp.ViewModel.Availability.List
{
    /// <summary>
    /// AvailabilityListViewModel — ViewModel для екрану списку AvailabilityGroup.
    ///
    /// Основні задачі:
    /// 1) Показати Items (ObservableCollection) у DataGrid / ListView.
    /// 2) Тримати SelectedItem (вибір користувача).
    /// 3) Тримати SearchText (текст пошуку).
    /// 4) Дати команди: Search/Add/Edit/Delete/OpenProfile.
    ///
    /// Важливо:
    /// - Реальна логіка (пошук, CRUD, навігація) делегується owner’у (AvailabilityViewModel),
    ///   а цей VM — “тонкий” UI-шар.
    /// </summary>
    public sealed class AvailabilityListViewModel : ViewModelBase
    {
        // =========================================================
        // 1) Залежності
        // =========================================================

        /// <summary>
        /// Owner (AvailabilityViewModel) — координатор модуля:
        /// - виконує запити до сервісів
        /// - керує навігацією (List/Edit/Profile)
        /// - показує діалоги (Confirm/Error/Info)
        ///
        /// Тут ListVm лише делегує команди в owner.
        /// </summary>
        private readonly AvailabilityViewModel _owner;

        // =========================================================
        // 2) Дані для UI
        // =========================================================

        /// <summary>
        /// Items — список груп, який відображається в UI.
        /// ObservableCollection потрібен, щоб WPF автоматично реагував на:
        /// - Clear/Add
        /// - вставку/видалення
        /// </summary>
        public ObservableCollection<AvailabilityGroupModel> Items { get; } = new();

        /// <summary>
        /// SelectedItem — поточний вибір (1 елемент).
        /// Використовується командами Edit/Delete/OpenProfile.
        /// </summary>
        private AvailabilityGroupModel? _selectedItem;

        public AvailabilityGroupModel? SelectedItem
        {
            get => _selectedItem;
            set
            {
                // SetProperty:
                // - порівнює старе/нове значення
                // - якщо реально змінилось — присвоює
                // - піднімає PropertyChanged(nameof(SelectedItem))
                //
                // Якщо вибір змінився — треба оновити CanExecute для команд, які залежать від вибору.
                if (SetProperty(ref _selectedItem, value))
                    UpdateSelectionCommands();
            }
        }

        /// <summary>
        /// SearchText — текст пошуку, який вводить користувач.
        /// Пошук запускається через SearchCommand (кнопка/Enter).
        /// </summary>
        private string _searchText = string.Empty;

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        // =========================================================
        // 3) Команди UI
        // =========================================================

        /// <summary>
        /// SearchCommand — оновити список згідно SearchText.
        /// Делегуємо в _owner.SearchAsync().
        /// </summary>
        public AsyncRelayCommand SearchCommand { get; }

        /// <summary>
        /// AddNewCommand — перейти у форму створення нової групи.
        /// Делегуємо в _owner.StartAddAsync().
        /// </summary>
        public AsyncRelayCommand AddNewCommand { get; }

        /// <summary>
        /// EditCommand — редагувати вибрану групу.
        /// Активна тільки якщо є SelectedItem.
        /// Делегуємо в _owner.EditSelectedAsync().
        /// </summary>
        public AsyncRelayCommand EditCommand { get; }

        /// <summary>
        /// DeleteCommand — видалити вибрану групу.
        /// Активна тільки якщо є SelectedItem.
        /// Делегуємо в _owner.DeleteSelectedAsync().
        /// </summary>
        public AsyncRelayCommand DeleteCommand { get; }

        /// <summary>
        /// OpenProfileCommand — відкрити профіль вибраної групи.
        /// Активна тільки якщо є SelectedItem.
        /// Делегуємо в _owner.OpenProfileAsync().
        /// </summary>
        public AsyncRelayCommand OpenProfileCommand { get; }

        /// <summary>
        /// Масив команд, які залежать від SelectedItem.
        /// Використовуємо, щоб не дублювати RaiseCanExecuteChanged 3 рази.
        /// </summary>
        private readonly AsyncRelayCommand[] _selectionDependentCommands;

        /// <summary>
        /// Конструктор:
        /// - зберігає owner
        /// - ініціалізує команди
        /// - готує список selection-dependent команд
        /// </summary>
        public AvailabilityListViewModel(AvailabilityViewModel owner)
        {
            // 1) Зберігаємо owner.
            _owner = owner;

            // 2) Створюємо команду пошуку.
            //    Тут немає canExecute — пошук можна робити завжди.
            SearchCommand = new AsyncRelayCommand(() => _owner.SearchAsync());

            // 3) Створюємо команду “додати нову”.
            AddNewCommand = new AsyncRelayCommand(() => _owner.StartAddAsync());

            // 4) Команди, які залежать від вибору.
            //    canExecute = () => HasSelection (тобто SelectedItem != null)
            EditCommand = new AsyncRelayCommand(
                execute: () => _owner.EditSelectedAsync(),
                canExecute: () => HasSelection);

            DeleteCommand = new AsyncRelayCommand(
                execute: () => _owner.DeleteSelectedAsync(),
                canExecute: () => HasSelection);

            OpenProfileCommand = new AsyncRelayCommand(
                execute: () => _owner.OpenProfileAsync(),
                canExecute: () => HasSelection);

            // 5) Формуємо масив команд, щоб RaiseCanExecuteChanged викликати циклом.
            _selectionDependentCommands = new[]
            {
                EditCommand,
                DeleteCommand,
                OpenProfileCommand
            };
        }

        /// <summary>
        /// HasSelection — компактна “єдиний центр правди” для canExecute.
        /// Якщо в майбутньому з’являться додаткові умови (наприклад роль/права),
        /// їх буде легше додати тут, не шукаючи 3 лямбди по файлу.
        /// </summary>
        private bool HasSelection => SelectedItem != null;

        // =========================================================
        // 4) Оновлення Items
        // =========================================================

        /// <summary>
        /// SetItems — встановити новий список груп у Items.
        ///
        /// Оптимізації порівняно з простим Clear+Add:
        /// 1) Якщо список фактично не змінився (за ключовими полями) — нічого не робимо:
        ///    - менше UI-миготіння
        ///    - менше зайвих подій/перемальовок
        ///    - selection менше “стрибає”
        ///
        /// 2) Якщо список змінився і ми перезалили Items — намагаємось відновити SelectedItem по Id:
        ///    - корисно, коли бекенд повертає НОВІ інстанси моделей при кожному Search/GetAll
        /// </summary>
        public void SetItems(IEnumerable<AvailabilityGroupModel> items)
        {
            // 1) Захист від null: трактуємо як “порожній список”.
            items ??= Array.Empty<AvailabilityGroupModel>();

            // 2) Зберігаємо поточний selectedId, щоб після оновлення (за потреби) відновити вибір.
            //    Якщо SelectedItem == null, selectedId буде 0.
            var selectedId = SelectedItem?.Id ?? 0;

            // 3) Матеріалізуємо items у IList, щоб:
            //    - мати доступ по індексу
            //    - не ітерувати IEnumerable кілька разів
            if (items is not IList<AvailabilityGroupModel> list)
                list = items.ToList();

            // 4) Якщо Items і новий list “однакові” — нічого не робимо.
            //    Це ключова оптимізація проти зайвих Clear/Add.
            if (IsSameList(Items, list))
            {
                // Навіть якщо список той самий, selection може бути валідним — нічого не чіпаємо.
                return;
            }

            // 5) Якщо список відрізняється — перезаливаємо Items.
            Items.Clear();

            // 6) Додаємо всі елементи по одному (ObservableCollection сам дасть UI нотифікації).
            foreach (var it in list)
                Items.Add(it);

            // 7) Після перезаливки WPF може скинути SelectedItem (бо старий reference вже не в Items).
            //    Тому пробуємо відновити вибір по Id, якщо Id був.
            if (selectedId > 0)
            {
                // 8) Знаходимо новий елемент з тим самим Id.
                var restored = Items.FirstOrDefault(x => x.Id == selectedId);

                // 9) Якщо знайшли — присвоюємо.
                //    SetProperty всередині setter’а сам вирішить, чи треба нотифікації/UpdateSelectionCommands.
                SelectedItem = restored;
            }
            else
            {
                // 10) Якщо selection не було — нічого не робимо.
                //     (можна було б явно SelectedItem = null, але це зайво).
            }
        }

        /// <summary>
        /// Перевіряє, чи старий список і новий список “однакові” для UI.
        ///
        /// Тут ми НЕ використовуємо reference equality моделей як єдину умову,
        /// бо бекенд часто повертає нові інстанси навіть для тих самих записів.
        ///
        /// Тому порівнюємо по “ключових” полях.
        /// Мінімально достатньо: Id.
        /// Але щоб не пропустити зміни назви/дати — додаємо Name/Month/Year, які типово є в AvailabilityGroupModel.
        ///
        /// Якщо у твоїй моделі поля називаються інакше — скажеш, я піджену.
        /// </summary>
        private static bool IsSameList(IList<AvailabilityGroupModel> current, IList<AvailabilityGroupModel> next)
        {
            // 1) Якщо кількість відрізняється — списки різні.
            if (current.Count != next.Count)
                return false;

            // 2) Порівнюємо елементи по індексах (важливий порядок у DataGrid).
            for (int i = 0; i < next.Count; i++)
            {
                // 3) Беремо поточний елемент і новий.
                var a = current[i];
                var b = next[i];

                // 4) Якщо один з них null (малоймовірно для моделей) — вважаємо різним.
                if (a is null || b is null)
                    return false;

                // 5) Порівнюємо ключові поля.
                //    Id — обов’язково.
                if (a.Id != b.Id)
                    return false;

                // 6) Порівнюємо Name (щоб не пропускати перейменування).
                //    Якщо Name може бути null — нормалізуємо до "".
                if (!string.Equals(a.Name ?? string.Empty, b.Name ?? string.Empty, StringComparison.Ordinal))
                    return false;

                // 7) Порівнюємо Month/Year (щоб не пропускати зміни місяця/року).
                //    Якщо у твоїй моделі ці поля відсутні або інші — можна прибрати/адаптувати.
                if (a.Month != b.Month)
                    return false;

                if (a.Year != b.Year)
                    return false;
            }

            // 8) Якщо всі перевірки пройдені — списки еквівалентні для UI.
            return true;
        }

        // =========================================================
        // 5) Оновлення CanExecute команд
        // =========================================================

        /// <summary>
        /// UpdateSelectionCommands — оновити CanExecute для команд,
        /// які залежать від SelectedItem.
        ///
        /// Викликається у setter SelectedItem, тому:
        /// - коли SelectedItem став null → Edit/Delete/Profile повинні вимкнутись
        /// - коли SelectedItem став не null → увімкнутись
        /// </summary>
        private void UpdateSelectionCommands()
        {
            // 1) Пробігаємо по всіх selection-dependent командах.
            for (int i = 0; i < _selectionDependentCommands.Length; i++)
            {
                // 2) Просимо команду перевизначити CanExecute.
                _selectionDependentCommands[i].RaiseCanExecuteChanged();
            }
        }
    }
}
