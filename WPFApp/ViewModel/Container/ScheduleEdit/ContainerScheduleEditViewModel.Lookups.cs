using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace WPFApp.ViewModel.Container.ScheduleEdit
{
    /// <summary>
    /// Частина (partial) того самого ViewModel, яка відповідає за:
    ///
    /// 1) Завантаження “довідників” (lookup’ів): Shops, AvailabilityGroups, Employees
    /// 2) Правильне оновлення ObservableCollection без зайвого “миготіння” UI
    /// 3) Синхронізацію вибраних значень (SelectedShop / SelectedAvailabilityGroup)
    ///    з даними обраного блоку (SelectedBlock)
    ///
    /// Чому це винесено в окремий файл:
    /// - в основному VM дуже багато логіки (матриця, стилі, команди, валідація)
    /// - “довідники + selection sync” — окрема тема, їй зручно жити тут
    /// </summary>
    public sealed partial class ContainerScheduleEditViewModel
    {
        // =========================
        // 1) Захист від “зациклення” при синхронізації selection
        // =========================

        /// <summary>
        /// Лічильник “глибини” синхронізації selection.
        ///
        /// Навіщо:
        /// - Коли ми програмно змінюємо SelectedShop/SelectedAvailabilityGroup,
        ///   їх setter’и можуть запускати логіку (debounce + schedule change).
        /// - Але коли ми робимо це “службово” (під час SetShops/SyncSelectionFromBlock),
        ///   нам НЕ треба запускати цю логіку, інакше буде зайва робота/зациклення.
        ///
        /// Тому в таких місцях ми заходимо в EnterSelectionSync():
        /// - збільшуємо depth
        /// - робимо потрібні присвоєння
        /// - на виході depth зменшується
        ///
        /// А в setter’ах SelectedShop/SelectedAvailabilityGroup є перевірка:
        ///   if (_selectionSyncDepth > 0) return;
        /// </summary>
        private int _selectionSyncDepth;

        /// <summary>
        /// Маленький scope-об’єкт для pattern “using var _ = EnterSelectionSync();”.
        ///
        /// Плюс цього підходу:
        /// - навіть якщо всередині кинули exception або зробили return,
        ///   Dispose() все одно спрацює, і _selectionSyncDepth не “зависне”.
        /// </summary>
        private readonly struct SelectionSyncScope : IDisposable
        {
            private readonly ContainerScheduleEditViewModel _vm;

            public SelectionSyncScope(ContainerScheduleEditViewModel vm)
            {
                _vm = vm;
                _vm._selectionSyncDepth++;
            }

            public void Dispose()
            {
                // гарантуємо, що не піде у мінус
                _vm._selectionSyncDepth = Math.Max(0, _vm._selectionSyncDepth - 1);
            }
        }

        /// <summary>
        /// Вхід у “режим синхронізації selection”.
        /// Використання:
        ///     using var _ = EnterSelectionSync();
        ///     ... присвоєння SelectedShop / SelectedAvailabilityGroup ...
        /// </summary>
        private SelectionSyncScope EnterSelectionSync() => new(this);


        // =========================
        // 2) Публічні методи для оновлення lookup’ів
        // =========================

        /// <summary>
        /// Оновити всі 3 довідники одним викликом.
        ///
        /// Коли викликається:
        /// - після завантаження даних з БД/API
        /// - після Search... команд, коли owner приніс новий список
        ///
        /// Чому тут просто виклик 3 методів:
        /// - кожен список має свою логіку “після оновлення”
        ///   (наприклад після SetShops потрібно виставити SelectedShop, якщо є SelectedBlock)
        /// </summary>
        public void SetLookups(IEnumerable<ShopModel> shops,
                               IEnumerable<AvailabilityGroupModel> groups,
                               IEnumerable<EmployeeModel> employees)
        {
            SetShops(shops);
            SetAvailabilityGroups(groups);
            SetEmployees(employees);
        }

        /// <summary>
        /// Оновлює список Shops (довідник магазинів) і синхронізує SelectedShop з SelectedBlock.
        ///
        /// Важливі моменти:
        /// 1) В EnterSelectionSync() ми робимо програмні присвоєння,
        ///    і не хочемо, щоб setter SelectedShop запустив debounce-логіку.
        ///
        /// 2) SetOptions(...) оновлює ObservableCollection максимально акуратно:
        ///    - якщо елементи ті ж самі, в тому ж порядку — нічого не робить
        ///    - інакше чистить і додає заново
        /// </summary>
        public void SetShops(IEnumerable<ShopModel> shops)
        {
            using var _ = EnterSelectionSync();

            SetOptions(Shops, shops);

            // Якщо є вибраний блок — підбираємо SelectedShop за ID з моделі
            if (SelectedBlock != null)
                SelectedShop = Shops.FirstOrDefault(s => s.Id == SelectedBlock.Model.ShopId);
        }

        /// <summary>
        /// Оновлює список AvailabilityGroups і синхронізує SelectedAvailabilityGroup з SelectedBlock.
        ///
        /// Тут додатковий нюанс:
        /// - в setter SelectedAvailabilityGroup у тебе є логіка, яка при зміні групи
        ///   може інваліднути (InvalidateGeneratedSchedule).
        /// - коли ми синхронізуємо selection “службово” (після отримання довідника),
        ///   нам НЕ треба інвалідити згенерований розклад.
        ///
        /// Тому ти використовуєш прапорець _suppressAvailabilityGroupUpdate:
        /// - під час службового присвоєння ставимо true
        /// - після присвоєння повертаємо false
        /// </summary>
        public void SetAvailabilityGroups(IEnumerable<AvailabilityGroupModel> groups)
        {
            using var _ = EnterSelectionSync();

            SetOptions(AvailabilityGroups, groups);

            if (SelectedBlock != null)
            {
                _suppressAvailabilityGroupUpdate = true;
                try
                {
                    SelectedAvailabilityGroup = AvailabilityGroups
                        .FirstOrDefault(g => g.Id == SelectedBlock.SelectedAvailabilityGroupId);
                }
                finally
                {
                    _suppressAvailabilityGroupUpdate = false;
                }
            }
        }

        /// <summary>
        /// Оновлює довідник Employees (список працівників).
        ///
        /// Тут синхронізувати SelectedEmployee/SelectedScheduleEmployee можна окремо,
        /// якщо потрібно. Зараз ти просто оновлюєш довідник.
        /// </summary>
        public void SetEmployees(IEnumerable<EmployeeModel> employees)
        {
            using var _ = EnterSelectionSync();
            SetOptions(Employees, employees);
        }


        // =========================
        // 3) Commit “pending” selection (коли є проміжний вибір)
        // =========================

        /// <summary>
        /// У тебе є PendingSelectedShop — це “тимчасово обраний” магазин,
        /// який ще НЕ застосований як SelectedShop.
        ///
        /// Такий патерн корисний, якщо:
        /// - ти хочеш показувати вибір в UI, але застосовувати його тільки після confirm
        /// - або при великих запитах, щоб не тригерити логіку при кожному кліку
        /// </summary>
        public void CommitPendingShopSelection()
        {
            if (PendingSelectedShop == SelectedShop)
                return;

            SelectedShop = PendingSelectedShop;
            if (SelectedShop?.Id > 0)
                ClearShopSelectionErrors();
        }

        /// <summary>
        /// Аналогічно для AvailabilityGroup.
        /// </summary>
        public void CommitPendingAvailabilitySelection()
        {
            if (PendingSelectedAvailabilityGroup == SelectedAvailabilityGroup)
                return;

            SelectedAvailabilityGroup = PendingSelectedAvailabilityGroup;
            if (SelectedAvailabilityGroup?.Id > 0)
                ClearAvailabilitySelectionErrors();
        }


        // =========================
        // 4) Синхронізація selection з SelectedBlock
        // =========================

        /// <summary>
        /// Синхронізує SelectedShop і SelectedAvailabilityGroup з поточним SelectedBlock.
        ///
        /// Коли викликається:
        /// - при зміні SelectedBlock (у setter SelectedBlock)
        ///
        /// Для SelectedBlock == null:
        /// - треба очистити selection, щоб UI не показував дані старого блоку
        /// </summary>
        public void SyncSelectionFromBlock()
        {
            using var _ = EnterSelectionSync();

            if (SelectedBlock == null)
            {
                SelectedShop = null;

                _suppressAvailabilityGroupUpdate = true;
                try
                {
                    SelectedAvailabilityGroup = null;
                }
                finally
                {
                    _suppressAvailabilityGroupUpdate = false;
                }

                return;
            }

            // Shop
            SelectedShop = Shops.FirstOrDefault(s => s.Id == SelectedBlock.Model.ShopId);

            // Availability group
            _suppressAvailabilityGroupUpdate = true;
            try
            {
                SelectedAvailabilityGroup = AvailabilityGroups
                    .FirstOrDefault(g => g.Id == SelectedBlock.SelectedAvailabilityGroupId);
            }
            finally
            {
                _suppressAvailabilityGroupUpdate = false;
            }
        }


        // =========================
        // 5) Загальний helper: “аккуратно оновити ObservableCollection”
        // =========================

        /// <summary>
        /// SetOptions — універсальний helper для оновлення ObservableCollection.
        ///
        /// Чому не просто target.Clear() + Add():
        /// - якщо дані не змінилися, зайве очищення:
        ///   - викликає багато UI-оновлень
        ///   - збиває selection у списках
        ///   - може створити “миготіння”
        ///
        /// Алгоритм:
        /// 1) Перетворюємо items у IList (щоб можна було звертатись по індексу).
        /// 2) Якщо кількість елементів однакова — перевіряємо “чи всі рівні в тому ж порядку”.
        ///    Якщо так — нічого не робимо.
        /// 3) Якщо різні — очищаємо і додаємо заново.
        /// </summary>
        private void SetOptions<T>(ObservableCollection<T> target, IEnumerable<T> items)
        {
            // 1) Намагаємось не робити зайвих алокацій.
            // Якщо items уже IList<T> — просто використовуємо.
            if (items is not IList<T> list)
                list = items.ToList();

            // 2) Якщо count однаковий — є шанс, що список взагалі не змінився.
            if (target.Count == list.Count)
            {
                var same = true;

                for (var i = 0; i < list.Count; i++)
                {
                    // EqualityComparer<T>.Default — стандартне порівняння для типу:
                    // - для класів це ReferenceEquals або override Equals
                    // - для struct це value equality
                    if (!EqualityComparer<T>.Default.Equals(target[i], list[i]))
                    {
                        same = false;
                        break;
                    }
                }

                // 3) Якщо все однакове — нічого не робимо.
                if (same)
                    return;
            }

            // 4) Якщо різне — оновлюємо повністю.
            target.Clear();
            foreach (var item in list)
                target.Add(item);
        }
    }
}
