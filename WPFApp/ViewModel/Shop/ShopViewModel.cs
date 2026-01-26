using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using WPFApp.Infrastructure;
using WPFApp.Infrastructure.Validation;
using WPFApp.Service;
using WPFApp.ViewModel.Dialogs;
using WPFApp.ViewModel.Shop.Helpers;

namespace WPFApp.ViewModel.Shop
{
    public enum ShopSection
    {
        List,
        Edit,
        Profile
    }

    /// <summary>
    /// ShopViewModel — owner/coordinator модуля Shop.
    ///
    /// Відповідальність:
    /// - тримати 3 під-VM: List/Edit/Profile
    /// - керувати навігацією: CurrentSection + Mode + CancelTarget
    /// - виконувати CRUD через IShopService
    /// - показувати повідомлення користувачу
    ///
    /// Оптимізації:
    /// 1) EnsureInitializedAsync:
    ///    - конкурентні виклики чекають один init-task
    ///    - _initialized стає true лише після успішного завершення
    /// 2) Валідація перенесена в ShopValidationRules
    /// 3) Після Save:
    ///    - робимо reload списку
    ///    - відновлюємо selection
    ///    - якщо повертаємось у Profile — refresh profile
    /// </summary>
    public sealed class ShopViewModel : ViewModelBase
    {
        private readonly IShopService _shopService;

        // ----------------------------
        // Initialization (safe, без гонок)
        // ----------------------------

        private bool _initialized;
        private Task? _initializeTask;
        private readonly object _initLock = new();

        // Id профілю, який зараз відкрито (щоб після Save можна було refresh).
        private int? _openedProfileShopId;

        // ----------------------------
        // Navigation state
        // ----------------------------

        private object _currentSection = null!;
        public object CurrentSection
        {
            get => _currentSection;
            private set => SetProperty(ref _currentSection, value);
        }

        private ShopSection _mode = ShopSection.List;
        public ShopSection Mode
        {
            get => _mode;
            private set => SetProperty(ref _mode, value);
        }

        /// <summary>
        /// CancelTarget — куди повертаємось з Edit при Cancel (List або Profile).
        /// </summary>
        public ShopSection CancelTarget { get; private set; } = ShopSection.List;

        // ----------------------------
        // Child VMs
        // ----------------------------

        public ShopListViewModel ListVm { get; }
        public ShopEditViewModel EditVm { get; }
        public ShopProfileViewModel ProfileVm { get; }

        public ShopViewModel(IShopService shopService)
        {
            _shopService = shopService;

            ListVm = new ShopListViewModel(this);
            EditVm = new ShopEditViewModel(this);
            ProfileVm = new ShopProfileViewModel(this);

            CurrentSection = ListVm;
        }

        // =========================================================
        // Initialization
        // =========================================================

        public Task EnsureInitializedAsync(CancellationToken ct = default)
        {
            // 1) Якщо вже ініціалізовано — ок.
            if (_initialized)
                return Task.CompletedTask;

            // 2) Якщо task вже існує — повертаємо його.
            if (_initializeTask != null)
                return _initializeTask;

            // 3) Створюємо init-task під lock, щоб не запускати двічі.
            lock (_initLock)
            {
                // 4) Перевірка повторно, бо поки чекали lock стан міг змінитись.
                if (_initialized)
                    return Task.CompletedTask;

                if (_initializeTask != null)
                    return _initializeTask;

                // 5) Створюємо реальний init-task.
                _initializeTask = InitializeCoreAsync(ct);

                return _initializeTask;
            }
        }

        private async Task InitializeCoreAsync(CancellationToken ct)
        {
            try
            {
                // 1) Завантажуємо початковий список.
                await LoadShopsAsync(ct, selectId: null);

                // 2) Тільки тут ставимо _initialized=true (після успіху).
                _initialized = true;
            }
            catch
            {
                // 3) Якщо впали — дозволяємо повторити init.
                lock (_initLock)
                {
                    _initializeTask = null;
                    _initialized = false;
                }

                throw;
            }
        }

        // =========================================================
        // List flows
        // =========================================================

        internal async Task SearchAsync(CancellationToken ct = default)
        {
            // 1) Беремо search term.
            var term = ListVm.SearchText;

            // 2) Якщо пусто — беремо все, інакше — фільтр.
            var list = string.IsNullOrWhiteSpace(term)
                ? await _shopService.GetAllAsync(ct)
                : await _shopService.GetByValueAsync(term, ct);

            // 3) Віддаємо в ListVm.
            ListVm.SetItems(list);
        }

        internal Task StartAddAsync(CancellationToken ct = default)
        {
            // 1) Скидаємо форму.
            EditVm.ResetForNew();

            // 2) Cancel з Edit поверне у List.
            CancelTarget = ShopSection.List;

            // 3) Переходимо в Edit.
            return SwitchToEditAsync();
        }

        internal async Task EditSelectedAsync(CancellationToken ct = default)
        {
            // 1) Беремо selection зі списку.
            var selected = ListVm.SelectedItem;
            if (selected is null)
                return;

            // 2) Беремо “latest” з сервісу (щоб редагувати актуальні дані).
            var latest = await _shopService.GetAsync(selected.Id, ct) ?? selected;

            // 3) Заповнюємо форму.
            EditVm.SetShop(latest);

            // 4) Якщо прийшли з Profile — Cancel має повернути в Profile.
            CancelTarget = Mode == ShopSection.Profile
                ? ShopSection.Profile
                : ShopSection.List;

            // 5) В Edit.
            await SwitchToEditAsync();
        }

        // =========================================================
        // Save / Delete / Profile flows
        // =========================================================

        internal async Task SaveAsync(CancellationToken ct = default)
        {
            // 1) При новому Save чистимо попередні помилки.
            EditVm.ClearValidationErrors();

            // 2) Збираємо модель.
            var model = EditVm.ToModel();

            // 3) Валідація через rules.
            var errors = ShopValidationRules.ValidateAll(model);

            // 4) Якщо є помилки — показуємо у формі і виходимо.
            if (errors.Count > 0)
            {
                EditVm.SetValidationErrors(errors);
                return;
            }

            try
            {
                // 5) Update або Create.
                if (EditVm.IsEdit)
                {
                    await _shopService.UpdateAsync(model, ct);
                }
                else
                {
                    var created = await _shopService.CreateAsync(model, ct);
                    EditVm.ShopId = created.Id;
                    model = created;
                }
            }
            catch (Exception ex)
            {
                // 6) Помилка — показуємо.
                ShowError(ex);
                return;
            }

            // 7) Повідомлення.
            ShowInfo(EditVm.IsEdit
                ? "Shop updated successfully."
                : "Shop added successfully.");

            // 8) Reload list + selection.
            await LoadShopsAsync(ct, selectId: model.Id);

            // 9) Повернення в Profile або List залежно від CancelTarget.
            if (CancelTarget == ShopSection.Profile)
            {
                // 9.1) Визначаємо id профілю для refresh.
                var profileId = _openedProfileShopId ?? model.Id;

                // 9.2) Якщо id валідний — refresh профілю.
                if (profileId > 0)
                {
                    var latest = await _shopService.GetAsync(profileId, ct) ?? model;

                    ProfileVm.SetProfile(latest);
                    ListVm.SelectedItem = latest;
                }

                await SwitchToProfileAsync();
            }
            else
            {
                await SwitchToListAsync();
            }
        }

        internal async Task DeleteSelectedAsync(CancellationToken ct = default)
        {
            // 1) Визначаємо currentId (із профілю або зі списку).
            var currentId = GetCurrentShopId();
            if (currentId <= 0)
                return;

            // 2) Для confirm беремо ім’я:
            //    - якщо у Profile — там вже є Name
            //    - якщо у List — беремо зі SelectedItem
            var currentName = Mode == ShopSection.Profile
                ? ProfileVm.Name
                : ShopDisplayHelper.NameOrEmpty(ListVm.SelectedItem);

            // 3) Confirm.
            if (!Confirm(string.IsNullOrWhiteSpace(currentName)
                    ? "Delete shop?"
                    : $"Delete {currentName}?"))
            {
                return;
            }

            try
            {
                // 4) Delete.
                await _shopService.DeleteAsync(currentId, ct);
            }
            catch (Exception ex)
            {
                // 5) Error.
                ShowError(ex);
                return;
            }

            // 6) Info.
            ShowInfo("Shop deleted successfully.");

            // 7) Reload + return to list.
            await LoadShopsAsync(ct, selectId: null);
            await SwitchToListAsync();
        }

        internal async Task OpenProfileAsync(CancellationToken ct = default)
        {
            // 1) Беремо selection.
            var selected = ListVm.SelectedItem;
            if (selected is null)
                return;

            // 2) Latest.
            var latest = await _shopService.GetAsync(selected.Id, ct) ?? selected;

            // 3) Запам’ятовуємо відкритий profileId.
            _openedProfileShopId = latest.Id;

            // 4) Заповнюємо ProfileVm (він також синхронізує ListVm.SelectedItem).
            ProfileVm.SetProfile(latest);

            // 5) На всяк випадок синхронізуємо selection у списку.
            ListVm.SelectedItem = latest;

            // 6) Cancel з профілю веде в List.
            CancelTarget = ShopSection.List;

            // 7) Переходимо в Profile.
            await SwitchToProfileAsync();
        }

        internal Task CancelAsync()
        {
            // 1) При Cancel чистимо помилки Edit.
            EditVm.ClearValidationErrors();

            // 2) Навігація залежить від Mode та CancelTarget.
            return Mode switch
            {
                ShopSection.Edit => CancelTarget == ShopSection.Profile
                    ? SwitchToProfileAsync()
                    : SwitchToListAsync(),

                _ => SwitchToListAsync()
            };
        }

        // =========================================================
        // Load + navigation helpers
        // =========================================================

        private async Task LoadShopsAsync(CancellationToken ct, int? selectId)
        {
            // 1) Беремо список.
            var list = (await _shopService.GetAllAsync(ct)).ToList();

            // 2) Оновлюємо ListVm.
            ListVm.SetItems(list);

            // 3) Якщо треба — виставляємо selection по Id.
            if (selectId.HasValue)
                ListVm.SelectedItem = list.FirstOrDefault(s => s.Id == selectId.Value);
        }

        private Task SwitchToListAsync()
        {
            CurrentSection = ListVm;
            Mode = ShopSection.List;
            return Task.CompletedTask;
        }

        private Task SwitchToEditAsync()
        {
            CurrentSection = EditVm;
            Mode = ShopSection.Edit;
            return Task.CompletedTask;
        }

        private Task SwitchToProfileAsync()
        {
            CurrentSection = ProfileVm;
            Mode = ShopSection.Profile;
            return Task.CompletedTask;
        }

        private int GetCurrentShopId()
        {
            // Якщо ми в профілі — беремо ShopId з ProfileVm.
            if (Mode == ShopSection.Profile)
                return ProfileVm.ShopId;

            // Інакше — зі списку.
            return ListVm.SelectedItem?.Id ?? 0;
        }

        // =========================================================
        // UI messaging (як у твоєму проєкті)
        // =========================================================

        internal void ShowInfo(string text)
            => CustomMessageBox.Show("Info", text, CustomMessageBoxIcon.Info, okText: "OK");

        internal void ShowError(string text)
            => CustomMessageBox.Show("Error", text, CustomMessageBoxIcon.Error, okText: "OK");

        internal void ShowError(Exception ex)
        {
            var (summary, details) = ExceptionMessageBuilder.Build(ex);
            CustomMessageBox.Show("Error", summary, CustomMessageBoxIcon.Error, okText: "OK", details: details);
        }

        private bool Confirm(string text, string? caption = null)
            => CustomMessageBox.Show(
                caption ?? "Confirm",
                text,
                CustomMessageBoxIcon.Warning,
                okText: "Yes",
                cancelText: "No");
    }
}
