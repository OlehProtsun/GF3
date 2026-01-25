using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WPFApp.ViewModel.Container.Edit
{
    /// <summary>
    /// ContainerViewModel.Lookups — частина (partial) ContainerViewModel, яка відповідає ТІЛЬКИ за:
    ///
    /// 1) Завантаження довідників (lookup data):
    ///    - Shops
    ///    - AvailabilityGroups
    ///    - Employees
    ///
    /// 2) Кешування цих довідників у списках _allShops/_allAvailabilityGroups/_allEmployees,
    ///    щоб не ходити в БД/сервіс при кожному пошуку у текстовому полі.
    ///
    /// 3) Фільтрацію довідників за текстом (search text) без повторних звернень до сервісів:
    ///    - ApplyShopFilter
    ///    - ApplyAvailabilityFilter
    ///    - ApplyEmployeeFilter
    ///
    /// 4) Пам’ять останніх фільтрів (_lastShopFilter/...), щоб НЕ перераховувати списки,
    ///    якщо користувач ввів той самий текст ще раз.
    ///
    /// Чому це винесено:
    /// - це окрема тема (lookup cache + filtering),
    /// - вона не має змішуватися з CRUD контейнерів, schedule-flow, preview pipeline тощо.
    /// </summary>
    public sealed partial class ContainerViewModel
    {
        // =========================================================
        // 1) ЛОКАЛЬНИЙ КЕШ ДОВІДНИКІВ
        // =========================================================

        /// <summary>
        /// Повний список магазинів, який ми один раз завантажили з _shopService.
        /// Далі під час “пошуку по тексту” ми фільтруємо цей список локально.
        /// </summary>
        private readonly List<ShopModel> _allShops = new();

        /// <summary>
        /// Повний список availability groups, завантажений з _availabilityGroupService.
        /// </summary>
        private readonly List<AvailabilityGroupModel> _allAvailabilityGroups = new();

        /// <summary>
        /// Повний список працівників, завантажений з _employeeService.
        /// </summary>
        private readonly List<EmployeeModel> _allEmployees = new();

        // =========================================================
        // 2) ОСТАННІ ЗАСТОСОВАНІ ФІЛЬТРИ (ЩОБ НЕ РАХУВАТИ ПОВТОРНО)
        // =========================================================

        /// <summary>
        /// Останній текст фільтру для Shops.
        /// Якщо користувач вводить те саме значення повторно — ми нічого не робимо.
        /// </summary>
        private string? _lastShopFilter;

        /// <summary>
        /// Останній текст фільтру для AvailabilityGroups.
        /// </summary>
        private string? _lastAvailabilityFilter;

        /// <summary>
        /// Останній текст фільтру для Employees.
        /// </summary>
        private string? _lastEmployeeFilter;

        // =========================================================
        // 3) ЗАВАНТАЖЕННЯ LOOKUPS З СЕРВІСІВ
        // =========================================================

        /// <summary>
        /// LoadLookupsAsync — один раз (або перед відкриттям schedule editor)
        /// завантажує довідники з бекенду:
        /// - групи доступності
        /// - магазини
        /// - працівники
        ///
        /// Потім:
        /// 1) кладемо їх в _all... списки (кеш),
        /// 2) скидаємо last filters,
        /// 3) передаємо дані у ScheduleEditVm.SetLookups(...)
        ///
        /// Важливо:
        /// - ScheduleEditVm.SetLookups оновлює ObservableCollection, яка прив’язана до UI.
        /// - Тому викликаємо її в UI thread через RunOnUiThreadAsync.
        /// </summary>
        private async Task LoadLookupsAsync(CancellationToken ct)
        {
            // 1) Тягнемо дані з сервісів
            // ToList() для groups: щоб не тягнути enumerable повторно.
            var groups = (await _availabilityGroupService.GetAllAsync(ct)).ToList();
            var shops = await _shopService.GetAllAsync(ct);
            var employees = await _employeeService.GetAllAsync(ct);

            // 2) Оновлюємо кеші
            _allAvailabilityGroups.Clear();
            _allAvailabilityGroups.AddRange(groups);

            _allShops.Clear();
            _allShops.AddRange(shops);

            _allEmployees.Clear();
            _allEmployees.AddRange(employees);

            // 3) Скидаємо попередні фільтри — бо дані могли змінитися
            _lastShopFilter = null;
            _lastAvailabilityFilter = null;
            _lastEmployeeFilter = null;

            // 4) Прокидуємо в ScheduleEditVm (UI колекції)
            await RunOnUiThreadAsync(() =>
            {
                ScheduleEditVm.SetLookups(shops, groups, employees);
            }).ConfigureAwait(false);
        }

        // =========================================================
        // 4) RESET ФІЛЬТРІВ (повернути “повні списки” в ScheduleEditVm)
        // =========================================================

        /// <summary>
        /// ResetScheduleFilters — повністю очищає search text'и в ScheduleEditVm
        /// і повертає повні _all... списки назад в lookup колекції.
        ///
        /// Коли використовується:
        /// - при відкритті schedule editor (StartScheduleAdd/Edit/MultiOpen)
        /// - коли треба “почати з чистого листа” у пошуку
        /// </summary>
        private void ResetScheduleFilters()
        {
            // 1) Очищаємо текстові поля пошуку (UI)
            ScheduleEditVm.ShopSearchText = string.Empty;
            ScheduleEditVm.AvailabilitySearchText = string.Empty;
            ScheduleEditVm.EmployeeSearchText = string.Empty;

            // 2) Скидаємо кеш останніх фільтрів
            _lastShopFilter = null;
            _lastAvailabilityFilter = null;
            _lastEmployeeFilter = null;

            // 3) Віддаємо повні списки в ScheduleEditVm
            ScheduleEditVm.SetLookups(_allShops, _allAvailabilityGroups, _allEmployees);
        }

        // =========================================================
        // 5) ПУБЛІЧНІ “ПОШУКОВІ” МЕТОДИ, ЯКІ ВИКЛИКАЮТЬСЯ З ScheduleEditVm КОМАНД
        // =========================================================

        /// <summary>
        /// Викликається, коли користувач натиснув Enter/кнопку пошуку для Shops у ScheduleEdit.
        /// Ми НЕ ходимо в сервіс: просто фільтруємо _allShops локально.
        /// </summary>
        internal Task SearchScheduleShopsAsync(CancellationToken ct = default)
        {
            ApplyShopFilter(ScheduleEditVm.ShopSearchText);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Пошук по AvailabilityGroups у ScheduleEdit.
        /// </summary>
        internal Task SearchScheduleAvailabilityAsync(CancellationToken ct = default)
        {
            ApplyAvailabilityFilter(ScheduleEditVm.AvailabilitySearchText);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Пошук по Employees у ScheduleEdit.
        /// </summary>
        internal Task SearchScheduleEmployeesAsync(CancellationToken ct = default)
        {
            ApplyEmployeeFilter(ScheduleEditVm.EmployeeSearchText);
            return Task.CompletedTask;
        }

        // =========================================================
        // 6) ФІЛЬТРИ ДОВІДНИКІВ
        // =========================================================

        private void ApplyShopFilter(string? raw)
        {
            ApplyFilter(
                raw,
                ref _lastShopFilter,
                _allShops,
                s => ContainsIgnoreCase(s.Name, _lastShopFilter ?? string.Empty),
                ScheduleEditVm.SetShops);
        }

        private void ApplyAvailabilityFilter(string? raw)
        {
            ApplyFilter(
                raw,
                ref _lastAvailabilityFilter,
                _allAvailabilityGroups,
                g => ContainsIgnoreCase(g.Name, _lastAvailabilityFilter ?? string.Empty),
                ScheduleEditVm.SetAvailabilityGroups);
        }

        private void ApplyEmployeeFilter(string? raw)
        {
            ApplyFilter(
                raw,
                ref _lastEmployeeFilter,
                _allEmployees,
                e => ContainsIgnoreCase(e.FirstName, _lastEmployeeFilter ?? string.Empty)
                  || ContainsIgnoreCase(e.LastName, _lastEmployeeFilter ?? string.Empty),
                ScheduleEditVm.SetEmployees);
        }

        /// <summary>
        /// Перевірка “містить рядок без врахування регістру”.
        /// Якщо source null — вважаємо що це "".
        /// </summary>
        private static bool ContainsIgnoreCase(string? source, string value)
            => (source ?? string.Empty).Contains(value, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Універсальний механізм фільтрації:
        ///
        /// raw      — те, що ввів користувач
        /// last     — що було застосовано попередній раз (кеш)
        /// all      — повний список довідника
        /// predicate— умова фільтрації
        /// apply    — як покласти результат у ScheduleEditVm (SetShops/SetEmployees/...)
        ///
        /// Алгоритм:
        /// 1) Нормалізуємо term (trim)
        /// 2) Якщо term не змінився (порівняння OrdinalIgnoreCase) — виходимо
        /// 3) Якщо term порожній — застосовуємо full list
        /// 4) Інакше — робимо ручний for-loop (швидше і без зайвих алокацій LINQ)
        /// </summary>
        private void ApplyFilter<T>(
            string? raw,
            ref string? last,
            IReadOnlyList<T> all,
            Func<T, bool> predicate,
            Action<IEnumerable<T>> apply)
        {
            var term = raw?.Trim() ?? string.Empty;

            // Якщо користувач ввів те саме — нічого не робимо
            if (string.Equals(last, term, StringComparison.OrdinalIgnoreCase))
                return;

            last = term;

            // Якщо term порожній — показуємо весь список
            if (string.IsNullOrWhiteSpace(term))
            {
                apply(all);
                return;
            }

            // Фільтруємо вручну, щоб не створювати багато проміжних enumerable
            var filtered = new List<T>(capacity: Math.Min(all.Count, 256));
            for (int i = 0; i < all.Count; i++)
            {
                var item = all[i];
                if (predicate(item))
                    filtered.Add(item);
            }

            apply(filtered);
        }
    }
}
