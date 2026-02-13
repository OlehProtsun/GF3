using BusinessLogicLayer.Availability;
using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFApp.Infrastructure.ScheduleMatrix;
using WPFApp.ViewModel.Container.Edit.Helpers;
using WPFApp.ViewModel.Container.ScheduleEdit;
using WPFApp.ViewModel.Container.ScheduleEdit.Helpers;
using System.Globalization;

// Явно фіксуємо, ЯКИЙ саме ScheduleBlockViewModel ми використовуємо в цьому файлі,
// щоб не було “підміни” типу через інші using.
using ScheduleBlockVm = WPFApp.ViewModel.Container.ScheduleEdit.Helpers.ScheduleBlockViewModel;


namespace WPFApp.ViewModel.Container.Edit
{
    /// <summary>
    /// ContainerViewModel.Schedules.SaveGenerate — частина (partial) ContainerViewModel,
    /// яка відповідає ТІЛЬКИ за:
    ///
    /// 1) SaveScheduleAsync:
    ///    - валідація ВСІХ відкритих блоків (табів)
    ///    - підтвердження користувача
    ///    - збереження schedule + employees + slots + cellStyles
    ///    - перехід назад у Profile або ScheduleProfile
    ///
    /// 2) GenerateScheduleAsync:
    ///    - валідація активного блока
    ///    - синхронізація працівників з availability group
    ///    - виклик генератора
    ///    - оновлення слотів + refresh матриці
    ///
    /// 3) SyncEmployeesFromAvailabilityGroupAsync:
    ///    - приводить block.Employees у відповідність до обраної availability group
    ///    - не губить MinHoursMonth (важливо)
    ///
    /// 4) Допоміжні методи валідації/нормалізації:
    ///    - ValidateAndNormalizeSchedule (без ScheduleTimeParsing, тільки через TryNormalizeShiftRange)
    ///    - TryNormalizeShiftRange (парсер часу через ScheduleMatrixEngine.TryParseTime)
    ///    - BuildScheduleValidationSummary + GetScheduleFieldLabel
    ///
    /// Чому це винесено:
    /// - це найбільший і найскладніший блок ContainerViewModel
    /// - він не повинен змішуватись з навігацією/lookup/CRUD контейнерів
    /// - у головному файлі лишається “скелет”, а тут — “робота з schedule”
    /// </summary>
    public sealed partial class ContainerViewModel
    {
        // =========================================================
        // 1) SAVE SCHEDULES (усі відкриті блоки)
        // =========================================================

        /// <summary>
        /// SaveScheduleAsync — зберігає ВСІ відкриті schedule-блоки (таби) у ScheduleEditVm.
        ///
        /// Важливий принцип:
        /// - Ми не зберігаємо помилки “всередині блока” (бо block.ValidationErrors прибрано),
        ///   а тримаємо errors локально під час валідації.
        ///
        /// Потік:
        /// 1) якщо блоків немає -> помилка
        /// 2) для кожного блока:
        ///    - ValidateAndNormalizeSchedule(model, out normalizedShift1/2)
        ///    - якщо є помилки -> накопичуємо (block, errors)
        ///    - якщо нема -> записуємо нормалізовані shift1/shift2 назад у model
        /// 3) якщо є invalidBlocks:
        ///    - активуємо перший невалідний блок
        ///    - показуємо помилки в UI (SetValidationErrors)
        ///    - показуємо summary (ShowError)
        ///    - виходимо
        /// 4) перевіряємо що для кожного блока щось згенеровано (slots)
        /// 5) confirm список назв
        /// 6) SaveWithDetailsAsync для кожного блока
        /// 7) reload schedules + повідомлення
        /// 8) навігація назад
        /// </summary>
        internal async Task SaveScheduleAsync(CancellationToken ct = default)
        {
            ScheduleEditVm.ClearValidationErrors();

            if (ScheduleEditVm.Blocks.Count == 0)
            {
                ShowError("Add at least one schedule first.");
                return;
            }

            // invalidBlocks: список блоків з помилками (помилки зберігаємо окремо, не в block)
            var invalidBlocks = new List<(ScheduleBlockVm Block, Dictionary<string, string> Errors)>();

            foreach (ScheduleBlockVm block in ScheduleEditVm.Blocks)
            {
                var errors = ValidateAndNormalizeSchedule(block.Model, out var normalizedShift1, out var normalizedShift2);

                if (errors.Count > 0)
                {
                    // Явно іменуємо елементи кортежу — додатковий захист від інференсу
                    invalidBlocks.Add((Block: block, Errors: errors));
                    continue;
                }

                block.Model.Shift1Time = normalizedShift1!;
                block.Model.Shift2Time = normalizedShift2!;
            }


            // 2) Якщо є помилки — показуємо перший проблемний блок + summary
            if (invalidBlocks.Count > 0)
            {
                var first = invalidBlocks[0];

                ScheduleEditVm.SelectedBlock = first.Block;
                ScheduleEditVm.SetValidationErrors(first.Errors);

                ShowError(BuildScheduleValidationSummary(invalidBlocks));
                return;
            }

            // 3) Не дозволяємо Save, якщо в якомусь блоці нічого не згенеровано
            var missingGenerated = ScheduleEditVm.Blocks.Where(block => !HasGeneratedContent(block)).ToList();
            if (missingGenerated.Count > 0)
            {
                var first = missingGenerated.First();
                ScheduleEditVm.SelectedBlock = first;
                ShowError("You can’t save a schedule until something has been generated. Please run generation first.");
                return;
            }

            // 4) Confirm: збираємо список назв schedule-ів
            var names = ScheduleEditVm.Blocks
                .Select((block, index) =>
                {
                    var name = string.IsNullOrWhiteSpace(block.Model.Name)
                        ? $"Schedule {index + 1}"
                        : block.Model.Name;
                    return $"- {name}";
                })
                .ToList();

            var confirmMessage =
                $"Do you want to save these schedules?{Environment.NewLine}{string.Join(Environment.NewLine, names)}";

            if (!Confirm(confirmMessage))
                return;

            // 5) Збереження кожного блока
            foreach (var block in ScheduleEditVm.Blocks)
            {
                // employees: унікальні по EmployeeId
                var employees = block.Employees
                    .GroupBy(e => e.EmployeeId)
                    .Select(g => new ScheduleEmployeeModel
                    {
                        EmployeeId = g.Key,
                        MinHoursMonth = g.First().MinHoursMonth
                    })
                    .ToList();

                var slots = block.Slots.ToList();

                foreach (var s in slots)
                {
                    // 1) Якщо десь просочується 0 замість null — SQLite вважає це NOT NULL
                    if (s.EmployeeId is int id && id == 0)
                        s.EmployeeId = null;

                    // 2) Приводимо status до того, що дозволяє CHECK:
                    // UNFURNISHED => employee_id NULL
                    // ASSIGNED    => employee_id NOT NULL
                    s.Status = s.EmployeeId == null ? SlotStatus.UNFURNISHED : SlotStatus.ASSIGNED;

                    // 3) (опційно, але часто потрібно після ручного вводу) нормалізуємо "9:00" => "09:00"
                    s.FromTime = NormalizeHHmm(s.FromTime);
                    s.ToTime = NormalizeHHmm(s.ToTime);

                    // 4) (опційно) швидка перевірка порядку часу, бо є CHECK (from_time < to_time)
                    if (string.CompareOrdinal(s.FromTime, s.ToTime) >= 0)
                        throw new InvalidOperationException(
                            $"Invalid time range: day={s.DayOfMonth}, slot={s.SlotNo}, {s.FromTime}-{s.ToTime}");
                }

                static string NormalizeHHmm(string value)
                {
                    if (TimeSpan.TryParse(value, out var ts))
                        return ts.ToString(@"hh\:mm");
                    return value; // якщо не парситься — хай впаде на валідації/БД, або зроби ShowError
                }


                // cellStyles зберігаємо тільки якщо реально є колір (фон або текст)
                var cellStyles = block.CellStyles
                    .Where(cs => cs.BackgroundColorArgb.HasValue || cs.TextColorArgb.HasValue)
                    .ToList();

                // AvailabilityGroupId: зберігаємо вибрану групу, якщо є
                block.Model.AvailabilityGroupId = block.SelectedAvailabilityGroupId > 0
                    ? block.SelectedAvailabilityGroupId
                    : null;

                try
                {
                    await _scheduleService.SaveWithDetailsAsync(block.Model, employees, slots, cellStyles, ct);
                }
                catch (Exception ex)
                {
                    ShowError(ex);
                    return;
                }
            }

            // 6) Reload schedule list в профілі контейнера
            var containerId = ScheduleEditVm.Blocks.FirstOrDefault()?.Model.ContainerId ?? GetCurrentContainerId();
            if (containerId > 0)
                await LoadSchedulesAsync(containerId, search: null, ct);

            _databaseChangeNotifier.NotifyDatabaseChanged("Container.ScheduleSave");
            ShowInfo("Schedules saved successfully.");

            // 7) Якщо редагували (IsEdit) — зазвичай хочемо перейти у ScheduleProfile
            if (ScheduleEditVm.IsEdit)
            {
                var savedBlock = ScheduleEditVm.Blocks.FirstOrDefault();
                if (savedBlock != null)
                {
                    // Перетягуємо актуальну detailed-модель і показуємо у ScheduleProfile
                    var detailed = await _scheduleService.GetDetailedAsync(savedBlock.Model.Id, ct);
                    if (detailed != null)
                    {
                        var employees = detailed.Employees?.ToList() ?? new List<ScheduleEmployeeModel>();
                        var slots = detailed.Slots?.ToList() ?? new List<ScheduleSlotModel>();
                        var cellStyles = detailed.CellStyles?.ToList() ?? new List<ScheduleCellStyleModel>();

                        await ScheduleProfileVm.SetProfileAsync(detailed, employees, slots, cellStyles, ct);

                        // синхронізуємо selection у списку schedule
                        ProfileVm.ScheduleListVm.SelectedItem =
                            ProfileVm.ScheduleListVm.Items.FirstOrDefault(x => x.Model.Id == detailed.Id);
                    }
                }

                ScheduleCancelTarget = ContainerSection.Profile;
                await SwitchToScheduleProfileAsync();
                return;
            }

            // Якщо це “Add mode” — повертаємось у Profile контейнера
            await SwitchToProfileAsync();
        }

        // =========================================================
        // 2) GENERATE SCHEDULE (один активний блок)
        // =========================================================

        /// <summary>
        /// GenerateScheduleAsync — генерує слоти для поточного SelectedBlock.
        ///
        /// Потік:
        /// 1) прибираємо фокус (Keyboard.ClearFocus), щоб UI зберіг введені значення
        /// 2) беремо SelectedBlock + model
        /// 3) валідимо/нормалізуємо schedule
        /// 4) перевіряємо SelectedAvailabilityGroupId
        /// 5) model.AvailabilityGroupId = groupId (важливо для Save/Edit)
        /// 6) SyncEmployeesFromAvailabilityGroupAsync(groupId)
        /// 7) LoadFullAsync(group) і перевірка month/year
        /// 8) формуємо список employees для генератора (з MinHoursMonth)
        /// 9) викликаємо _generator.GenerateAsync(...)
        /// 10) оновлюємо block.Slots + refresh матриці
        /// </summary>
        internal async Task GenerateScheduleAsync(CancellationToken ct = default)
        {
            await RunOnUiThreadAsync(() => Keyboard.ClearFocus());

            if (ScheduleEditVm.SelectedBlock is null)
                return;

            var block = ScheduleEditVm.SelectedBlock;
            var model = block.Model;

            var errors = ValidateAndNormalizeSchedule(model, out var normalizedShift1, out var normalizedShift2);
            if (errors.Count > 0)
            {
                ScheduleEditVm.SetValidationErrors(errors);
                ShowError("Please fix the highlighted fields.");
                return;
            }

            model.Shift1Time = normalizedShift1!;
            model.Shift2Time = normalizedShift2!;

            var selectedGroupId = block.SelectedAvailabilityGroupId;
            if (selectedGroupId <= 0)
            {
                ShowError("Select an availability group.");
                return;
            }

            // ВАЖЛИВО: зберігаємо групу в моделі (потрібно для Save / Edit)
            model.AvailabilityGroupId = selectedGroupId;

            // 1) Синхронізуємо працівників у блоці з вибраною групою
            await SyncEmployeesFromAvailabilityGroupAsync(selectedGroupId, ct);

            // 2) Витягуємо повні дані групи (members + days)
            // Використовуємо Item1/Item2/Item3, щоб уникати проблем деconstruction.
            var loaded = await _availabilityGroupService.LoadFullAsync(selectedGroupId, ct).ConfigureAwait(false);
            var group = loaded.Item1;
            var members = loaded.Item2 ?? new List<AvailabilityGroupMemberModel>();
            var days = loaded.Item3 ?? new List<AvailabilityGroupDayModel>();

            if (group.Year != model.Year || group.Month != model.Month)
            {
                ShowError("Selected availability group is for a different month/year.");
                return;
            }

            // 3) Прив'язуємо days до members
            var daysByMember = days
                .GroupBy(d => d.AvailabilityGroupMemberId)
                .ToDictionary(g => g.Key, g => (ICollection<AvailabilityGroupDayModel>)g.ToList());

            foreach (var m in members)
            {
                m.Days = daysByMember.TryGetValue(m.Id, out var list)
                    ? list
                    : new List<AvailabilityGroupDayModel>();
            }

            group.Members = members;

            // 4) Формуємо employees ДЛЯ генератора з block.Employees (там вже введені MinHoursMonth)
            // На всяк випадок фільтруємо по членству в групі.
            var memberEmpIds = members.Select(m => m.EmployeeId).Distinct().ToHashSet();

            var employees = block.Employees
                .Where(e => memberEmpIds.Contains(e.EmployeeId))
                .GroupBy(e => e.EmployeeId)
                .Select(g =>
                {
                    var first = g.First();
                    return new ScheduleEmployeeModel
                    {
                        EmployeeId = first.EmployeeId,
                        Employee = first.Employee,          // підтягнутий у SyncEmployeesFromAvailabilityGroupAsync
                        MinHoursMonth = first.MinHoursMonth // НЕ губимо
                    };
                })
                .ToList();

            if (employees.Count == 0)
            {
                ShowError("No employees found for selected availability group.");
                return;
            }

            var fullGroups = new List<AvailabilityGroupModel>(capacity: 1) { group };

            // 5) Генерація
            var slots = await _generator.GenerateAsync(model, fullGroups, employees, progress: null, ct: ct)
                       ?? new List<ScheduleSlotModel>();

            // 6) Оновлюємо слоти
            block.Slots.Clear();
            foreach (var slot in slots)
                block.Slots.Add(slot);

            // 7) Оновлюємо матрицю
            await ScheduleEditVm.RefreshScheduleMatrixAsync(ct);
            ShowInfo("Slots generated. Review before saving.");
        }

        // =========================================================
        // 3) SYNC EMPLOYEES з availability group (не губимо MinHoursMonth)
        // =========================================================

        /// <summary>
        /// Приводить block.Employees у відповідність до складу availability group.
        ///
        /// Важливо:
        /// - НЕ затирає MinHoursMonth (якщо користувач уже вводив його в UI)
        /// - додає відсутніх і прибирає зайвих
        /// - підтягує Employee reference, якщо в existing employee null
        /// </summary>
        internal async Task SyncEmployeesFromAvailabilityGroupAsync(int groupId, CancellationToken ct = default)
        {
            if (ScheduleEditVm.SelectedBlock is null)
                return;

            var block = ScheduleEditVm.SelectedBlock;

            var loaded = await _availabilityGroupService.LoadFullAsync(groupId, ct).ConfigureAwait(false);
            var members = loaded.Item2 ?? new List<AvailabilityGroupMemberModel>();

            if (ct.IsCancellationRequested)
                return;

            // Якщо блок вже закрили/прибрали — виходимо раніше (до UI)
            if (!ScheduleEditVm.Blocks.Contains(block))
                return;

            // Normalize members: один запис на EmployeeId (пріоритет тому, де Employee != null)
            var memberByEmpId = new Dictionary<int, AvailabilityGroupMemberModel>(capacity: Math.Max(16, members.Count));
            foreach (var m in members)
            {
                var empId = m.EmployeeId;

                if (memberByEmpId.TryGetValue(empId, out var existing))
                {
                    if (existing.Employee == null && m.Employee != null)
                        memberByEmpId[empId] = m;
                }
                else
                {
                    memberByEmpId[empId] = m;
                }
            }

            bool changed = false;

            await RunOnUiThreadAsync(() =>
            {
                // Якщо блок вже закрили між await — не чіпаємо
                if (!ScheduleEditVm.Blocks.Contains(block))
                    return;

                // 1) Зберігаємо старі MinHoursMonth (перший запис на EmployeeId)
                var oldMin = new Dictionary<int, int?>();
                foreach (var e in block.Employees)
                {
                    if (!oldMin.ContainsKey(e.EmployeeId))
                        oldMin[e.EmployeeId] = e.MinHoursMonth;
                }

                // 2) Індекс існуючих працівників у блоці
                var existingById = new Dictionary<int, ScheduleEmployeeModel>();
                foreach (var e in block.Employees)
                {
                    if (!existingById.ContainsKey(e.EmployeeId))
                        existingById[e.EmployeeId] = e;
                }

                // 3) Прибираємо тих, кого нема в групі
                for (int i = block.Employees.Count - 1; i >= 0; i--)
                {
                    var e = block.Employees[i];
                    if (!memberByEmpId.ContainsKey(e.EmployeeId))
                    {
                        block.Employees.RemoveAt(i);
                        changed = true;
                    }
                }

                // 4) Додаємо відсутніх + підтягуємо Employee reference
                foreach (var kv in memberByEmpId)
                {
                    var empId = kv.Key;
                    var m = kv.Value;

                    if (existingById.TryGetValue(empId, out var existing))
                    {
                        // Підтягнути reference Employee, якщо його не було
                        if (existing.Employee == null && m.Employee != null)
                        {
                            existing.Employee = m.Employee;
                            changed = true;
                        }
                        continue;
                    }

                    oldMin.TryGetValue(empId, out var min);

                    block.Employees.Add(new ScheduleEmployeeModel
                    {
                        EmployeeId = empId,
                        Employee = m.Employee,
                        MinHoursMonth = min
                    });

                    changed = true;
                }
            }).ConfigureAwait(false);

            // Refresh матриці тільки якщо реально були зміни і блок ще активний
            if (changed && !ct.IsCancellationRequested && ReferenceEquals(ScheduleEditVm.SelectedBlock, block))
            {
                await ScheduleEditVm.RefreshScheduleMatrixAsync(ct).ConfigureAwait(false);
            }
        }

        // =========================================================
        // 4) VALIDATION + NORMALIZATION HELPERS (без дублювання)
        // =========================================================

        /// <summary>
        /// Валідація schedule + нормалізація shift рядків.
        ///
        /// Вихід:
        /// - errors: ключ = nameof(ContainerScheduleEditViewModel.ScheduleX), value = message
        /// - normalizedShift1/2: нормалізовані "HH:mm-HH:mm"
        ///
        /// Важливо:
        /// - тут ми НЕ використовуємо ScheduleTimeParsing (бо в тебе немає хелпера в каталозі Container)
        /// - використовуємо TryNormalizeShiftRange, який базується на ScheduleMatrixEngine.TryParseTime
        /// </summary>
        private static Dictionary<string, string> ValidateAndNormalizeSchedule(
            ScheduleModel model,
            out string? normalizedShift1,
            out string? normalizedShift2)
        {
            var errors = new Dictionary<string, string>();
            normalizedShift1 = null;
            normalizedShift2 = null;

            if (model.ContainerId <= 0)
                errors[nameof(ContainerScheduleEditViewModel.ScheduleContainerId)] = "Select a container.";
            if (model.ShopId <= 0)
                errors[nameof(ContainerScheduleEditViewModel.ScheduleShopId)] = "Select a shop.";
            if (string.IsNullOrWhiteSpace(model.Name))
                errors[nameof(ContainerScheduleEditViewModel.ScheduleName)] = "Name is required.";
            if (model.Year < 1900)
                errors[nameof(ContainerScheduleEditViewModel.ScheduleYear)] = "Year is invalid.";
            if (model.Month < 1 || model.Month > 12)
                errors[nameof(ContainerScheduleEditViewModel.ScheduleMonth)] = "Month must be 1-12.";
            if (model.PeoplePerShift <= 0)
                errors[nameof(ContainerScheduleEditViewModel.SchedulePeoplePerShift)] = "People per shift must be greater than zero.";
            if (model.MaxHoursPerEmpMonth <= 0)
                errors[nameof(ContainerScheduleEditViewModel.ScheduleMaxHoursPerEmp)] = "Max hours per employee must be greater than zero.";

            // Shift1 (required)
            if (string.IsNullOrWhiteSpace(model.Shift1Time))
            {
                errors[nameof(ContainerScheduleEditViewModel.ScheduleShift1)] = "Shift1 is required.";
            }
            else if (!TryNormalizeShiftRange(model.Shift1Time, out normalizedShift1, out var err1))
            {
                errors[nameof(ContainerScheduleEditViewModel.ScheduleShift1)] = err1 ?? "Invalid shift1 format.";
            }

            // Shift2 (у твоєму поточному коді — required)
            if (string.IsNullOrWhiteSpace(model.Shift2Time))
            {
                errors[nameof(ContainerScheduleEditViewModel.ScheduleShift2)] = "Shift2 is required.";
            }
            else if (!TryNormalizeShiftRange(model.Shift2Time, out normalizedShift2, out var err2))
            {
                errors[nameof(ContainerScheduleEditViewModel.ScheduleShift2)] = err2 ?? "Invalid shift2 format.";
            }

            // Note нормалізуємо: або null, або trim
            model.Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim();

            return errors;
        }

        /// <summary>
        /// Нормалізація shift-рядка до формату "HH:mm-HH:mm" (без пробілів).
        ///
        /// Приймаємо:
        /// - "9:00-18:00"
        /// - "09:00 - 18:00"
        ///
        /// Вихід:
        /// - normalized: "09:00-18:00"
        ///
        /// Парсинг часу робимо через ScheduleMatrixEngine.TryParseTime,
        /// щоб у всьому проекті був один “центр істини” щодо форматів часу.
        /// </summary>
        private static bool TryNormalizeShiftRange(string? input, out string normalized, out string? error)
        {
            normalized = string.Empty;
            error = null;

            input = (input ?? string.Empty).Trim();
            if (input.Length == 0)
            {
                error = "Shift is required.";
                return false;
            }

            // 1) Нормалізуємо єдиним парсером, який уже використовується в проекті
            if (!AvailabilityCodeParser.TryNormalizeInterval(input, out var normalizedCandidate))
            {
                error = "Shift format must be: HH:mm-HH:mm.";
                return false;
            }

            var parts = normalizedCandidate.Split('-', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                error = "Shift format must be: HH:mm-HH:mm.";
                return false;
            }

            // 2) Строгий парс + гарантія, що це в межах доби (00:00..23:59)
            if (!TimeSpan.TryParseExact(parts[0], @"hh\:mm", CultureInfo.InvariantCulture, out var from) ||
                !TimeSpan.TryParseExact(parts[1], @"hh\:mm", CultureInfo.InvariantCulture, out var to))
            {
                error = "Shift time must be HH:mm.";
                return false;
            }

            // на випадок якщо хтось протягне 24:00 як TimeSpan 1.00:00
            if (from < TimeSpan.Zero || from >= TimeSpan.FromHours(24) ||
                to < TimeSpan.Zero || to >= TimeSpan.FromHours(24))
            {
                error = "Shift time must be within 00:00..23:59 (24:00 is not allowed).";
                return false;
            }

            if (to <= from)
            {
                error = "Shift end must be later than shift start.";
                return false;
            }

            normalized = $"{from:hh\\:mm} - {to:hh\\:mm}";
            return true;

        }

        /// <summary>
        /// Будує текст “summary” для MessageBox, якщо є кілька невалідних блоків.
        ///
        /// invalidBlocks: список (Block, Errors), де Errors — словник property->message.
        /// </summary>
        private string BuildScheduleValidationSummary(
            IReadOnlyCollection<(ScheduleBlockVm Block, Dictionary<string, string> Errors)> invalidBlocks)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Fix the following schedule errors before saving:");

            foreach (var item in invalidBlocks)
            {
                var block = item.Block;
                var errors = item.Errors;

                var index = ScheduleEditVm.Blocks.IndexOf(block);
                var displayIndex = index >= 0 ? index + 1 : 0;

                var header = displayIndex > 0 ? $"Schedule #{displayIndex}" : "Schedule";

                var name = string.IsNullOrWhiteSpace(block.Model.Name) ? null : block.Model.Name.Trim();
                if (!string.IsNullOrWhiteSpace(name))
                    header = $"{header} \"{name}\"";

                foreach (var kv in errors)
                {
                    var label = GetScheduleFieldLabel(kv.Key);
                    sb.AppendLine($"- {header}: {label} — {kv.Value}");
                }
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Дружні “людські” назви полів для summary.
        /// Вхід propertyName — це ключ з errors (nameof(ContainerScheduleEditViewModel.ScheduleX)).
        /// </summary>
        private static string GetScheduleFieldLabel(string propertyName)
        {
            return propertyName switch
            {
                nameof(ContainerScheduleEditViewModel.ScheduleContainerId) => "Container",
                nameof(ContainerScheduleEditViewModel.ScheduleShopId) => "Shop",
                nameof(ContainerScheduleEditViewModel.ScheduleName) => "Name",
                nameof(ContainerScheduleEditViewModel.ScheduleYear) => "Year",
                nameof(ContainerScheduleEditViewModel.ScheduleMonth) => "Month",
                nameof(ContainerScheduleEditViewModel.SchedulePeoplePerShift) => "People per shift",
                nameof(ContainerScheduleEditViewModel.ScheduleMaxHoursPerEmp) => "Max hours per employee",
                nameof(ContainerScheduleEditViewModel.ScheduleShift1) => "Shift 1",
                nameof(ContainerScheduleEditViewModel.ScheduleShift2) => "Shift 2",
                _ => propertyName
            };
        }
    }
}
