using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WPFApp.ViewModel.Container.Edit.Helpers;

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
        {
            EditVm.ResetForNew();
            CancelTarget = ContainerSection.List;
            return SwitchToEditAsync();
        }

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
        internal async Task EditSelectedAsync(CancellationToken ct = default)
        {
            var id = GetCurrentContainerId();
            if (id <= 0) return;

            var latest = await _containerService.GetAsync(id, ct);
            if (latest is null) return;

            EditVm.SetContainer(latest);

            CancelTarget = Mode == ContainerSection.Profile
                ? ContainerSection.Profile
                : ContainerSection.List;

            await SwitchToEditAsync();
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
        internal async Task SaveAsync(CancellationToken ct = default)
        {
            EditVm.ClearValidationErrors();

            var model = EditVm.ToModel();
            var errors = ValidateContainer(model);

            if (errors.Count > 0)
            {
                EditVm.SetValidationErrors(errors);
                return;
            }

            try
            {
                if (EditVm.IsEdit)
                {
                    await _containerService.UpdateAsync(model, ct);
                }
                else
                {
                    var created = await _containerService.CreateAsync(model, ct);
                    EditVm.ContainerId = created.Id;
                    model = created;
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
                return;
            }

            ShowInfo(EditVm.IsEdit
                ? "Container updated successfully."
                : "Container added successfully.");

            _databaseChangeNotifier.NotifyDatabaseChanged("Container.Save");

            await LoadContainersAsync(ct, selectId: model.Id);

            // Якщо ми прийшли в Edit із профілю, то після Save треба повернутись назад у профіль
            if (CancelTarget == ContainerSection.Profile)
            {
                var profileId = _openedProfileContainerId ?? model.Id;

                if (profileId > 0)
                {
                    var latest = await _containerService.GetAsync(profileId, ct) ?? model;
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

            try
            {
                await _containerService.DeleteAsync(currentId, ct);
            }
            catch (Exception ex)
            {
                ShowError(ex);
                return;
            }

            ShowInfo("Container deleted successfully.");

            _databaseChangeNotifier.NotifyDatabaseChanged("Container.Delete");
            await LoadContainersAsync(ct, selectId: null);
            await SwitchToListAsync();
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
        internal async Task OpenProfileAsync(CancellationToken ct = default)
        {
            var selected = ListVm.SelectedItem;
            if (selected is null) return;

            var latest = await _containerService.GetAsync(selected.Id, ct) ?? selected;

            _openedProfileContainerId = latest.Id;

            ProfileVm.SetProfile(latest);
            ListVm.SelectedItem = latest;

            await LoadSchedulesAsync(latest.Id, search: null, ct);

            CancelTarget = ContainerSection.List;
            await SwitchToProfileAsync();
        }

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
            var schedules = await _scheduleService.GetByContainerAsync(containerId, search, ct);
            ProfileVm.ScheduleListVm.SetItems(schedules);
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
