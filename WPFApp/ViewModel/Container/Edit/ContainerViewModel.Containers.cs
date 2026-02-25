using BusinessLogicLayer.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WPFApp.ViewModel.Container.Edit.Helpers;
using WPFApp.ViewModel.Shared;

namespace WPFApp.ViewModel.Container.Edit
{
    /// <summary>
    /// ContainerViewModel.Containers — частина (partial) ContainerViewModel, яка відповідає ТІЛЬКИ за:
    ///
    /// 1) Ініціалізацію екрану контейнерів (EnsureInitializedAsync)
    /// 2) CRUD контейнерів:
    ///    - SearchAsync
    ///    - StartAddAsync
    ///    - EditSelectedAsync
    ///    - SaveAsync
    ///    - DeleteSelectedAsync
    ///    - OpenProfileAsync
    /// 3) Підвантаження даних для списків:
    ///    - LoadContainersAsync
    ///    - LoadSchedulesAsync (бо профіль контейнера показує schedules)
    ///
    /// Навіщо винесено:
    /// - це окрема бізнес-ділянка (робота з ContainerModel)
    /// - не має змішуватися з schedule-генерацією, preview pipeline, lookup-фільтрами, навігацією
    /// - головний ContainerViewModel.cs стане “скелетом”, а логіка буде по модулях
    /// </summary>
    public sealed partial class ContainerViewModel
    {
        /// <summary>
        /// EnsureInitializedAsync — гарантує, що стартові дані (список контейнерів) завантажені один раз.
        /// Якщо метод викликали вдруге — просто виходимо.
        /// </summary>
        public async Task EnsureInitializedAsync(CancellationToken ct = default)
        {
            if (_initialized) return;

            _initialized = true;
            await LoadContainersAsync(ct);
        }

        /// <summary>
        /// SearchAsync — пошук контейнерів.
        ///
        /// Логіка:
        /// - якщо SearchText порожній => беремо всі контейнери
        /// - інакше => шукаємо по значенню (бекенд вирішує як саме)
        ///
        /// Результат кладемо в ListVm.Items через SetItems (ObservableCollection => UI оновиться).
        /// </summary>
        internal async Task SearchAsync(CancellationToken ct = default)
        {
            var term = ListVm.SearchText;

            var list = string.IsNullOrWhiteSpace(term)
                ? await _containerService.GetAllAsync(ct)
                : await _containerService.GetByValueAsync(term, ct);

            ListVm.SetItems(list);
        }

        /// <summary>
        /// StartAddAsync — перейти у форму створення нового контейнера.
        ///
        /// Потік:
        /// 1) ResetForNew — очистити EditVm
        /// 2) CancelTarget = List — якщо користувач натисне Cancel, повертаємось у список
        /// 3) SwitchToEditAsync — переключаємо UI секцію (Navigation partial)
        /// </summary>
        internal Task StartAddAsync(CancellationToken ct = default)
            => UiOperationRunner.RunNavStatusFlowAsync(
                ct,
                ResetNavUiCts,
                ShowNavWorkingAsync,
                WaitForUiIdleAsync,
                async _ =>
                {
                    await RunOnUiThreadAsync(() =>
                    {
                        EditVm.ResetForNew();
                        CancelTarget = ContainerSection.List;
                    });

                    await SwitchToEditAsync();
                },
                ShowNavSuccessThenAutoHideAsync,
                HideNavStatusAsync,
                ShowError,
                successDelayMs: 700);


        /// <summary>
        /// EditSelectedAsync — відкрити редагування поточного контейнера.
        ///
        /// Потік:
        /// 1) Визначаємо containerId через GetCurrentContainerId()
        /// 2) Тягнемо "latest" модель з сервісу (щоб мати актуальні дані)
        /// 3) Заповнюємо EditVm
        /// 4) Виставляємо CancelTarget:
        ///    - якщо відкрили edit з профілю => Cancel поверне у Profile
        ///    - інакше => Cancel поверне у List
        /// 5) Переходимо у Edit секцію
        /// </summary>
        internal Task EditSelectedAsync(CancellationToken ct = default)
        {
            var id = GetCurrentContainerId();
            if (id <= 0)
                return Task.CompletedTask;

            return UiOperationRunner.RunNavStatusFlowAsync(
                ct,
                ResetNavUiCts,
                ShowNavWorkingAsync,
                WaitForUiIdleAsync,
                async uiToken =>
                {
                    var latest = await _containerService.GetAsync(id, uiToken).ConfigureAwait(false);
                    if (latest is null)
                        return false;

                    await RunOnUiThreadAsync(() =>
                    {
                        EditVm.SetContainer(latest);

                        CancelTarget = Mode == ContainerSection.Profile
                            ? ContainerSection.Profile
                            : ContainerSection.List;
                    }).ConfigureAwait(false);

                    await SwitchToEditAsync().ConfigureAwait(false);
                    return true;
                },
                ShowNavSuccessThenAutoHideAsync,
                HideNavStatusAsync,
                ShowError,
                successDelayMs: 700);
        }


        /// <summary>
        /// SaveAsync — зберегти контейнер (create або update).
        ///
        /// Потік:
        /// 1) очистити помилки EditVm
        /// 2) зібрати модель з EditVm.ToModel()
        /// 3) validate (поки лишаємо просту перевірку Name)
        /// 4) create або update через сервіс
        /// 5) reload список контейнерів і виділити створений/оновлений
        /// 6) повернутись туди, звідки зайшли (CancelTarget)
        /// </summary>
        internal Task SaveAsync(CancellationToken ct = default)
        {
            EditVm.ClearValidationErrors();

            var model = EditVm.ToModel();
            var errors = ValidateContainer(model);

            if (errors.Count > 0)
            {
                EditVm.SetValidationErrors(errors);
                return Task.CompletedTask;
            }

            return UiOperationRunner.RunNavStatusFlowAsync(
                ct,
                ResetNavUiCts,
                ShowNavWorkingAsync,
                WaitForUiIdleAsync,
                async uiToken =>
                {
                    if (EditVm.IsEdit)
                    {
                        await _containerService.UpdateAsync(model, uiToken).ConfigureAwait(false);
                    }
                    else
                    {
                        var created = await _containerService.CreateAsync(model, uiToken).ConfigureAwait(false);
                        await RunOnUiThreadAsync(() => EditVm.ContainerId = created.Id).ConfigureAwait(false);
                        model = created;
                    }

                    _databaseChangeNotifier.NotifyDatabaseChanged("Container.Save");
                    await LoadContainersAsync(uiToken, selectId: model.Id).ConfigureAwait(false);

                    if (CancelTarget == ContainerSection.Profile)
                    {
                        var profileId = _openedProfileContainerId ?? model.Id;
                        if (profileId > 0)
                        {
                            var latest = await _containerService.GetAsync(profileId, uiToken).ConfigureAwait(false) ?? model;
                            await SetProfileAndSelectionAsync(latest).ConfigureAwait(false);
                        }

                        await SwitchToProfileAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        await SwitchToListAsync().ConfigureAwait(false);
                    }
                },
                ShowNavSuccessThenAutoHideAsync,
                HideNavStatusAsync,
                ShowError,
                successDelayMs: 900);
        }


        /// <summary>
        /// DeleteSelectedAsync — видалити поточний контейнер.
        ///
        /// Потік:
        /// 1) визначаємо id
        /// 2) confirm
        /// 3) delete
        /// 4) reload список контейнерів
        /// 5) повертаємось в List
        /// </summary>
        internal async Task DeleteSelectedAsync(CancellationToken ct = default)
        {
            var currentId = GetCurrentContainerId();
            if (currentId <= 0) return;

            var currentName = Mode == ContainerSection.Profile
                ? ProfileVm.Name
                : ListVm.SelectedItem?.Name ?? string.Empty;

            if (!Confirm(string.IsNullOrWhiteSpace(currentName)
                ? "Delete container?"
                : $"Delete {currentName}?"))
            {
                return;
            }

            await UiOperationRunner.RunNavStatusFlowAsync(
                ct,
                ResetNavUiCts,
                ShowNavWorkingAsync,
                WaitForUiIdleAsync,
                async uiToken =>
                {
                    await _containerService.DeleteAsync(currentId, uiToken);

                    _databaseChangeNotifier.NotifyDatabaseChanged("Container.Delete");
                    await LoadContainersAsync(uiToken, selectId: null);
                    await SwitchToListAsync();
                },
                ShowNavSuccessThenAutoHideAsync,
                HideNavStatusAsync,
                ShowError,
                successDelayMs: 900);
        }

        /// <summary>
        /// OpenProfileAsync — відкрити профіль контейнера (шапка + schedule list).
        ///
        /// Потік:
        /// 1) беремо SelectedItem зі списку
        /// 2) тягнемо latest з сервісу
        /// 3) заповнюємо ProfileVm
        /// 4) вантажимо schedules для цього контейнера
        /// 5) ставимо CancelTarget=List і переходимо в Profile
        /// </summary>
        internal Task OpenProfileAsync(CancellationToken ct = default)
        {
            var selected = ListVm.SelectedItem;
            if (selected is null)
                return Task.CompletedTask;

            return UiOperationRunner.RunNavStatusFlowAsync(
                ct,
                ResetNavUiCts,
                ShowNavWorkingAsync,
                WaitForUiIdleAsync,
                async uiToken =>
                {
                    var latest = await _containerService.GetAsync(selected.Id, uiToken).ConfigureAwait(false) ?? selected;

                    _openedProfileContainerId = latest.Id;
                    await SetProfileAndSelectionAsync(latest).ConfigureAwait(false);
                    await LoadSchedulesAsync(latest.Id, search: null, uiToken).ConfigureAwait(false);

                    CancelTarget = ContainerSection.List;
                    await SwitchToProfileAsync().ConfigureAwait(false);
                },
                ShowNavSuccessThenAutoHideAsync,
                HideNavStatusAsync,
                ShowError,
                successDelayMs: 900);
        }



        private Task SetProfileAndSelectionAsync(ContainerModel model)
            => RunOnUiThreadAsync(() =>
            {
                ProfileVm.SetProfile(model);
                ListVm.SelectedItem = ListVm.Items.FirstOrDefault(x => x.Id == model.Id) ?? model;
            });

        // =========================================================
        // Завантаження даних (списки)
        // =========================================================

        /// <summary>
        /// LoadContainersAsync — завантажити всі контейнери в ListVm.
        /// Опційно: selectId — виділити контейнер в списку.
        /// </summary>
        private async Task LoadContainersAsync(CancellationToken ct, int? selectId = null)
        {
            var list = await _containerService.GetAllAsync(ct).ConfigureAwait(false);

            var disp = System.Windows.Application.Current?.Dispatcher;
            if (disp != null && !disp.CheckAccess())
            {
                await disp.InvokeAsync(() =>
                {
                    ListVm.SetItems(list);
                    if (selectId.HasValue)
                        ListVm.SelectedItem = ListVm.Items.FirstOrDefault(x => x.Id == selectId.Value);
                });
            }
            else
            {
                ListVm.SetItems(list);
                if (selectId.HasValue)
                    ListVm.SelectedItem = ListVm.Items.FirstOrDefault(x => x.Id == selectId.Value);
            }
        }

        /// <summary>
        /// LoadSchedulesAsync — завантажити schedules конкретного контейнера у ProfileVm.ScheduleListVm.
        /// Це потрібно при відкритті профілю контейнера і після операцій зі schedule.
        /// </summary>
        private async Task LoadSchedulesAsync(int containerId, string? search, CancellationToken ct)
        {
            ClearScheduleDetailsCache();

            var schedules = await _scheduleService
                .GetByContainerAsync(containerId, search, ct)
                .ConfigureAwait(false);

            var disp = Application.Current?.Dispatcher;
            if (disp != null && !disp.CheckAccess())
            {
                await disp.InvokeAsync(() => ProfileVm.ScheduleListVm.SetItems(schedules));
            }
            else
            {
                ProfileVm.ScheduleListVm.SetItems(schedules);
            }
        }


        // =========================================================
        // Валідація контейнера (поки проста)
        // =========================================================

        /// <summary>
        /// ValidateContainer — мінімальна перевірка ContainerModel перед Save.
        /// Ключі словника — імена властивостей у ContainerEditViewModel.
        ///
        /// Пізніше це можна замінити на ContainerValidationRules (як у ScheduleValidationRules),
        /// але навіть у поточному вигляді метод коректний.
        /// </summary>
        private static Dictionary<string, string> ValidateContainer(ContainerModel model)
        {
            var errors = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(model.Name))
                errors[nameof(ContainerEditViewModel.Name)] = "Name is required.";

            return errors;
        }
    }
}
