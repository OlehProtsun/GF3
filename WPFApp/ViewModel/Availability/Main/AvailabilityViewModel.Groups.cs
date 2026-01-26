using BusinessLogicLayer.Availability;
using DataAccessLayer.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WPFApp.ViewModel.Availability.Main
{
    /// <summary>
    /// Groups — CRUD і “бізнес-потоки” для AvailabilityGroup:
    /// - Search
    /// - StartAdd
    /// - EditSelected
    /// - Save
    /// - DeleteSelected
    /// - OpenProfile
    /// - Cancel
    /// </summary>
    public sealed partial class AvailabilityViewModel
    {
        internal async Task SearchAsync(CancellationToken ct = default)
        {
            // 1) Беремо search term з ListVm.
            var term = ListVm.SearchText;

            // 2) Якщо пусто — беремо все, інакше — пошук по значенню.
            var list = string.IsNullOrWhiteSpace(term)
                ? await _availabilityService.GetAllAsync(ct)
                : await _availabilityService.GetByValueAsync(term, ct);

            // 3) Кладемо результат у ListVm.
            ListVm.SetItems(list);
        }

        internal async Task StartAddAsync(CancellationToken ct = default)
        {
            // 1) Перед створенням — переконуємось, що employees актуальні.
            await LoadEmployeesAsync(ct);

            // 2) Скидаємо фільтр працівників (щоб користувач бачив повний список).
            ResetEmployeeSearch();

            // 3) Скидаємо EditVm у “новий запис”.
            EditVm.ResetForNew();

            // 4) Якщо користувач натисне Cancel — повертаємось у List.
            CancelTarget = AvailabilitySection.List;

            // 5) Переходимо в Edit.
            await SwitchToEditAsync();
        }

        internal async Task EditSelectedAsync(CancellationToken ct = default)
        {
            // 1) Беремо вибраний елемент зі списку.
            var selected = ListVm.SelectedItem;

            // 2) Якщо нічого не вибрано — виходимо.
            if (selected is null)
                return;

            // 3) Оновлюємо employees (щоб headers/captions були актуальні).
            await LoadEmployeesAsync(ct);

            // 4) Завантажуємо повний набір (group+members+days).
            var (group, members, days) = await _availabilityService.LoadFullAsync(selected.Id, ct);

            // 5) Заповнюємо EditVm.
            EditVm.LoadGroup(group, members, days, _employeeNames);

            // 6) Налаштовуємо CancelTarget:
            //    - якщо ми прийшли з Profile — Cancel має повернути в Profile
            //    - інакше — у List
            CancelTarget = Mode == AvailabilitySection.Profile
                ? AvailabilitySection.Profile
                : AvailabilitySection.List;

            // 7) Відкриваємо Edit.
            await SwitchToEditAsync();
        }

        internal async Task SaveAsync(CancellationToken ct = default)
        {
            // 1) Перед збереженням — очищаємо попередні помилки форми.
            EditVm.ClearValidationErrors();

            // 2) Зчитуємо назву, trim.
            var rawName = (EditVm.AvailabilityName ?? string.Empty).Trim();

            // 3) Визначаємо, чи це create.
            var isNew = EditVm.AvailabilityId == 0;

            // 4) Сuffix для нової групи (MM.YYYY).
            var suffix = $"{EditVm.AvailabilityMonth:D2}.{EditVm.AvailabilityYear}";

            // 5) Для “нових” — додаємо suffix в назву, щоб відрізняти місяці.
            var finalName = isNew
                ? $"{rawName} : {suffix}"
                : rawName;

            // 6) Формуємо модель групи.
            var group = new AvailabilityGroupModel
            {
                Id = EditVm.AvailabilityId,
                Name = finalName,
                Year = EditVm.AvailabilityYear,
                Month = EditVm.AvailabilityMonth
            };

            // 7) Валідатор домену (BLL): повертає словник помилок (property -> message).
            var errors = AvailabilityGroupValidator.Validate(group);

            // 8) Якщо помилки є — показуємо в EditVm (INotifyDataErrorInfo) і виходимо.
            if (errors.Count > 0)
            {
                EditVm.SetValidationErrors(errors);
                return;
            }

            // 9) Перевіряємо, що вибрано хоча б 1 працівника.
            var selectedEmployees = EditVm.GetSelectedEmployeeIds();
            if (selectedEmployees.Count == 0)
            {
                ShowError("Add at least 1 employee to the group.");
                return;
            }

            // 10) Для швидкого contains — робимо HashSet.
            var selectedSet = selectedEmployees.ToHashSet();

            // 11) Беремо коди з матриці і лишаємо тільки для вибраних employeeId.
            var raw = EditVm.ReadGroupCodes()
                .Where(x => selectedSet.Contains(x.employeeId));

            // 12) Будуємо payload для BLL (перетворення "+/-/interval" -> доменні структури).
            if (!AvailabilityPayloadBuilder.TryBuild(raw, out var payload, out var err))
            {
                ShowError(err ?? "Invalid availability codes.");
                return;
            }

            try
            {
                // 13) Виконуємо збереження.
                await _availabilityService.SaveGroupAsync(group, payload, ct);
            }
            catch (Exception ex)
            {
                // 14) На будь-якій помилці — показуємо exception.
                ShowError(ex);
                return;
            }

            // 15) Повідомляємо користувача.
            ShowInfo(isNew
                ? "Availability Group added successfully."
                : "Availability Group updated successfully.");

            // 16) Перезавантажуємо список.
            await LoadAllGroupsAsync(ct);

            // 17) Після Save повертаємось туди, звідки зайшли.
            if (CancelTarget == AvailabilitySection.Profile)
            {
                // 18) Якщо треба повернутись в Profile — перезавантажуємо його дані.
                var profileId = _openedProfileGroupId ?? group.Id;

                if (profileId > 0)
                {
                    var (g, members, days) = await _availabilityService.LoadFullAsync(profileId, ct);
                    ProfileVm.SetProfile(g, members, days);
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
            // 1) Беремо поточний вибір.
            var current = ListVm.SelectedItem;
            if (current is null)
                return;

            // 2) Confirm.
            if (!Confirm($"Delete '{current.Name}' ?"))
                return;

            try
            {
                // 3) Delete через сервіс.
                await _availabilityService.DeleteAsync(current.Id, ct);
            }
            catch (Exception ex)
            {
                // 4) Помилка — показуємо.
                ShowError(ex);
                return;
            }

            // 5) Info.
            ShowInfo("Availability Group deleted successfully.");

            // 6) Reload list.
            await LoadAllGroupsAsync(ct);

            // 7) Повертаємось у List.
            await SwitchToListAsync();
        }

        internal async Task OpenProfileAsync(CancellationToken ct = default)
        {
            // 1) Беремо вибір.
            var current = ListVm.SelectedItem;
            if (current is null)
                return;

            // 2) Запам’ятовуємо id відкритого profile.
            _openedProfileGroupId = current.Id;

            // 3) Завантажуємо повні дані.
            var (group, members, days) = await _availabilityService.LoadFullAsync(current.Id, ct);

            // 4) Заповнюємо ProfileVm.
            ProfileVm.SetProfile(group, members, days);

            // 5) Якщо з profile натиснуть Edit->Cancel — маємо вернутись в List.
            CancelTarget = AvailabilitySection.List;

            // 6) Переходимо в Profile.
            await SwitchToProfileAsync();
        }

        internal Task CancelAsync()
        {
            // 1) При Cancel з Edit — чистимо validation, щоб не “прилипало”.
            EditVm.ClearValidationErrors();

            // 2) Скидаємо пошук employees (щоб при наступному відкритті був повний список).
            ResetEmployeeSearch();

            // 3) Навігація залежить від Mode і CancelTarget.
            return Mode switch
            {
                AvailabilitySection.Edit => CancelTarget == AvailabilitySection.Profile
                    ? SwitchToProfileAsync()
                    : SwitchToListAsync(),

                _ => SwitchToListAsync()
            };
        }
    }
}
