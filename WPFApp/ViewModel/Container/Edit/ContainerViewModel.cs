using BusinessLogicLayer.Generators;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WPFApp.Infrastructure;
using WPFApp.Service;
using WPFApp.ViewModel.Container.List;
using WPFApp.ViewModel.Container.Profile;
using WPFApp.ViewModel.Container.ScheduleEdit;
using WPFApp.ViewModel.Container.ScheduleProfile;

namespace WPFApp.ViewModel.Container.Edit
{
    /// <summary>
    /// ContainerViewModel — “головний координатор” модуля Container.
    ///
    /// Важливе про архітектуру:
    /// - Це partial-class: майже вся логіка винесена в окремі файли (Navigation, Lookups, Schedules, Preview, UI тощо).
    /// - Цей файл спеціально залишений дуже коротким і виконує 2 ролі:
    ///
    ///   1) Зберігає DI-залежності (сервіси), які потрібні різним partial-модулям.
    ///   2) Створює під-ViewModel-и (List/Edit/Profile/ScheduleEdit/ScheduleProfile)
    ///      і задає стартовий екран (CurrentSection).
    ///
    /// Таким чином:
    /// - “композиція” (що з чого складається екран) живе тут
    /// - “поведінка” (як воно працює) живе в інших partial-файлах
    /// </summary>
    public sealed partial class ContainerViewModel : ViewModelBase
    {
        // =========================================================
        // 1) СЕРВІСИ (DI)
        // =========================================================
        //
        // Це залежності, які передає DI-контейнер (або фабрика) при створенні ContainerViewModel.
        // Вони зберігаються в полях, щоб ними користувалися різні partial-файли:
        //
        // - _containerService: CRUD контейнерів
        // - _scheduleService: CRUD schedule + detail loading
        // - _availabilityGroupService: робота з групами доступності (load members/days)
        // - _shopService: довідник магазинів
        // - _employeeService: довідник працівників
        // - _generator: генерація слотів schedule
        // - _colorPickerService: діалог вибору кольору для стилів клітинок
        //
        // ВАЖЛИВО:
        // - ці поля тут "protected by design": вони private readonly, і їх використовують частини partial-класу.
        private readonly IContainerService _containerService;
        private readonly IScheduleService _scheduleService;
        private readonly IAvailabilityGroupService _availabilityGroupService;
        private readonly IShopService _shopService;
        private readonly IEmployeeService _employeeService;
        private readonly IScheduleGenerator _generator;
        private readonly IColorPickerService _colorPickerService;
        private readonly IScheduleExportService _scheduleExportService;
        private readonly ConcurrentDictionary<int, Lazy<Task<ScheduleModel?>>> _scheduleDetailsCache = new();

        // =========================================================
        // 2) ВНУТРІШНІ СТАНИ ДЛЯ ВСЬОГО МОДУЛЯ
        // =========================================================

        /// <summary>
        /// Прапорець “ініціалізація вже виконана”.
        /// Використовується в EnsureInitializedAsync (в іншому partial-файлі),
        /// щоб стартові дані (список контейнерів) не завантажувати повторно.
        /// </summary>
        private bool _initialized;

        /// <summary>
        /// Id контейнера, профіль якого зараз відкритий.
        /// Потрібно для навігації та коректного повернення у Profile після Save/Edit.
        /// (Логіка використання живе в Containers partial).
        /// </summary>
        private int? _openedProfileContainerId;

        /// <summary>
        /// Максимальна кількість відкритих schedule-табів в ScheduleEdit.
        /// Константа, бо це “правило UI”, а не дані.
        /// </summary>
        private const int MaxOpenedSchedules = 20;

        // =========================================================
        // 3) ПІД-ВЮ МОДЕЛИ (КОМПОЗИЦІЯ UI)
        // =========================================================
        //
        // Ці ViewModel-и живуть довго (поки живе ContainerViewModel)
        // і між ними відбувається перемикання через CurrentSection (Navigation partial).
        //
        // Вони приймають "this" (owner), щоб делегувати складні дії назад у ContainerViewModel.
        // Це стандартний патерн: child VM -> owner VM -> services.
        public ContainerListViewModel ListVm { get; }
        public ContainerEditViewModel EditVm { get; }
        public ContainerProfileViewModel ProfileVm { get; }
        public ContainerScheduleEditViewModel ScheduleEditVm { get; }
        public ContainerScheduleProfileViewModel ScheduleProfileVm { get; }

        // =========================================================
        // 4) КОНСТРУКТОР (ЗБІРКА ВСЬОГО МОДУЛЯ)
        // =========================================================

        /// <summary>
        /// Конструктор ContainerViewModel.
        ///
        /// Що робить:
        /// 1) приймає всі DI-залежності (сервіси) і зберігає їх у полях
        /// 2) створює під-ViewModel-и (List/Edit/Profile/ScheduleEdit/ScheduleProfile)
        /// 3) встановлює стартовий екран: CurrentSection = ListVm
        ///
        /// Чому створення child VM робиться тут:
        /// - це “композиція” (хто з ким пов’язаний)
        /// - так легше підтримувати: один погляд на цей файл дає розуміння структури модуля
        /// </summary>
        public ContainerViewModel(
            IContainerService containerService,
            IScheduleService scheduleService,
            IAvailabilityGroupService availabilityGroupService,
            IShopService shopService,
            IEmployeeService employeeService,
            IScheduleGenerator generator,
            IColorPickerService colorPickerService,
            IScheduleExportService scheduleExportService)
        {
            // Null-guards:
            // Якщо DI-контейнер налаштований неправильно, ми хочемо отримати помилку одразу тут,
            // а не десь глибоко в логіці при першому зверненні.
            ArgumentNullException.ThrowIfNull(containerService);
            ArgumentNullException.ThrowIfNull(scheduleService);
            ArgumentNullException.ThrowIfNull(availabilityGroupService);
            ArgumentNullException.ThrowIfNull(shopService);
            ArgumentNullException.ThrowIfNull(employeeService);
            ArgumentNullException.ThrowIfNull(generator);
            ArgumentNullException.ThrowIfNull(colorPickerService);
            ArgumentNullException.ThrowIfNull(scheduleExportService);

            _containerService = containerService;
            _scheduleService = scheduleService;
            _availabilityGroupService = availabilityGroupService;
            _shopService = shopService;
            _employeeService = employeeService;
            _generator = generator;
            _colorPickerService = colorPickerService;
            _scheduleExportService = scheduleExportService;

            // Створюємо “дочірні” VM-и.
            // Вони отримують owner (this), щоб викликати методи типу:
            // - SearchAsync / SaveAsync / OpenProfileAsync
            // - StartScheduleAddAsync / GenerateScheduleAsync / SaveScheduleAsync
            // - TryPickScheduleCellColor / RunOnUiThreadAsync / Confirm / ShowError ...
            ListVm = new ContainerListViewModel(this);
            EditVm = new ContainerEditViewModel(this);
            ProfileVm = new ContainerProfileViewModel(this);
            ScheduleEditVm = new ContainerScheduleEditViewModel(
                this,
                _availabilityGroupService,
                _employeeService);
            ScheduleProfileVm = new ContainerScheduleProfileViewModel(this);

            ProfileVm.EmployeesLoader = LoadScheduleEmployeesAsync;
            ProfileVm.SlotsLoader = LoadScheduleSlotsAsync;

            // Стартова секція — список контейнерів.
            // CurrentSection визначений у Navigation partial-файлі.
            CurrentSection = ListVm;
        }

        private async Task<IReadOnlyList<ScheduleEmployeeModel>> LoadScheduleEmployeesAsync(int scheduleId, CancellationToken ct)
        {
            var detailed = await GetScheduleDetailsCachedAsync(scheduleId, ct).ConfigureAwait(false);
            return detailed?.Employees?.ToList() ?? new List<ScheduleEmployeeModel>();
        }

        private async Task<IReadOnlyList<ScheduleSlotModel>> LoadScheduleSlotsAsync(int scheduleId, CancellationToken ct)
        {
            var detailed = await GetScheduleDetailsCachedAsync(scheduleId, ct).ConfigureAwait(false);
            return detailed?.Slots?.ToList() ?? new List<ScheduleSlotModel>();
        }

        private async Task<ScheduleModel?> GetScheduleDetailsCachedAsync(int scheduleId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (scheduleId <= 0)
                return null;

            var lazyTask = _scheduleDetailsCache.GetOrAdd(
                scheduleId,
                id => new Lazy<Task<ScheduleModel?>>(() => _scheduleService.GetDetailedAsync(id, CancellationToken.None)));

            var detailed = await lazyTask.Value.ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            return detailed;
        }

        private void ClearScheduleDetailsCache()
            => _scheduleDetailsCache.Clear();
    }
}
