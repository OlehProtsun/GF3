using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WPFApp.ViewModel.Availability.Main
{
    /// <summary>
    /// Initialization — стартове завантаження довідників/списків.
    /// </summary>
    public sealed partial class AvailabilityViewModel
    {
        // Прапорець “успішно ініціалізовано”.
        private bool _initialized;

        // Поточний init-task (щоб конкурентні виклики не запускали ініціалізацію вдруге).
        private Task? _initializeTask;

        // Лок для створення init-task.
        private readonly object _initLock = new();

        /// <summary>
        /// EnsureInitializedAsync — гарантує одноразове завантаження:
        /// - груп (List)
        /// - працівників (Edit lookup)
        /// - bind-ів (Edit binds)
        /// </summary>
        public Task EnsureInitializedAsync(CancellationToken ct = default)
        {
            // 1) Якщо вже успішно ініціалізовано — виходимо.
            if (_initialized)
                return Task.CompletedTask;

            // 2) Якщо init-task вже створено — просто повертаємо його (усі чекатимуть одне).
            if (_initializeTask != null)
                return _initializeTask;

            // 3) Створюємо init-task під lock, щоб не створити 2 task-и паралельно.
            lock (_initLock)
            {
                // 4) Могло змінитись, поки чекали lock.
                if (_initialized)
                    return Task.CompletedTask;

                // 5) Якщо task вже створено — повертаємо.
                if (_initializeTask != null)
                    return _initializeTask;

                // 6) Створюємо реальний task ініціалізації.
                _initializeTask = InitializeCoreAsync(ct);

                // 7) Повертаємо його.
                return _initializeTask;
            }
        }

        private async Task InitializeCoreAsync(CancellationToken ct)
        {
            try
            {
                // 1) Завантажуємо список груп (для List).
                await LoadAllGroupsAsync(ct);

                // 2) Завантажуємо працівників (для Edit lookup і фільтра).
                await LoadEmployeesAsync(ct);

                // 3) Завантажуємо bind-и (для Edit).
                await LoadBindsAsync(ct);

                // 4) Фіксуємо успішну ініціалізацію.
                _initialized = true;
            }
            catch
            {
                // 5) Якщо щось впало — дозволяємо повторний init:
                //    - скидаємо task
                //    - скидаємо прапорець
                lock (_initLock)
                {
                    _initializeTask = null;
                    _initialized = false;
                }

                // 6) Прокидуємо виняток вище (щоб UI/логіка могла показати помилку).
                throw;
            }
        }

        // =========================================================
        // Завантаження даних (списки)
        // =========================================================

        private async Task LoadAllGroupsAsync(CancellationToken ct = default)
        {
            // 1) Беремо всі групи з сервісу.
            var list = await _availabilityService.GetAllAsync(ct);

            // 2) Кладемо в ListVm (там вже логіка оновлення ObservableCollection).
            ListVm.SetItems(list);
        }

        private async Task LoadEmployeesAsync(CancellationToken ct = default)
        {
            // 1) Отримуємо працівників.
            var employees = await _employeeService.GetAllAsync(ct);

            // 2) Перекладаємо у локальні кеші.
            //    (Кеші оголошені в Employees partial.)
            _allEmployees.Clear();
            _employeeNames.Clear();

            // 3) Зберігаємо повний список.
            _allEmployees.AddRange(employees);

            // 4) Будуємо lookup employeeId -> "First Last".
            foreach (var e in employees)
                _employeeNames[e.Id] = $"{e.FirstName} {e.LastName}";

            // 5) Віддаємо в EditVm повний список для відображення.
            EditVm.SetEmployees(_allEmployees, _employeeNames);
        }

        private async Task LoadBindsAsync(CancellationToken ct = default)
        {
            // 1) Беремо bind-и.
            var binds = await _bindService.GetAllAsync(ct);

            // 2) Віддаємо їх в EditVm.
            EditVm.SetBinds(binds);
        }
    }
}
