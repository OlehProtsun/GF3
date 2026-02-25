using BusinessLogicLayer.Contracts.Models;
using System;
using System.Data;
using WPFApp.ViewModel.Dialogs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using BusinessLogicLayer.Availability;
using WPFApp.UI.Dialogs;
using WPFApp.MVVM.Threading;
using WPFApp.Applications.Preview;



namespace WPFApp.ViewModel.Container.ScheduleEdit
{
    /// <summary>
    /// Selection.cs — частина (partial) ViewModel, яка відповідає ТІЛЬКИ за:
    /// 1) “реакції” на зміну вибраних значень (SelectedShop / SelectedAvailabilityGroup)
    /// 2) debounce (щоб не виконувати важку логіку на кожен швидкий клік)
    /// 3) інвалідацію (скидання) згенерованого розкладу, коли змінився контекст
    ///
    /// Чому це винесено:
    /// - у головному файлі VM повинні лишатися в основному властивості/команди,
    ///   а не поведінкові механізми “коли що перераховувати/скидати”.
    /// - ця логіка добре читається як окремий модуль.
    /// </summary>
    public sealed partial class ContainerScheduleEditViewModel
    {
        // ============================================================
        // 1) DEBOUNCE-НАЛАШТУВАННЯ ТА СТАН
        // ============================================================

        /// <summary>
        /// Скільки чекати перед “застосуванням” зміни selection.
        ///
        /// Навіщо:
        /// - користувач може швидко клацати по списку магазинів/груп,
        ///   і ми НЕ хочемо запускати логіку на кожен клік
        /// - ми хочемо виконати тільки останній вибір після короткої паузи
        ///
        /// 200ms — типове значення, яке:
        /// - майже не відчувається людиною
        /// - але сильно зменшує навантаження на UI/обчислення
        /// </summary>
        private static readonly TimeSpan SelectionDebounceDelay = TimeSpan.FromMilliseconds(200);

        /// <summary>
        /// Дебаунсер для зміни магазину (SelectedShop).
        /// Після затримки він застосує ScheduleShopId, якщо вибір не змінився.
        /// </summary>
        private readonly UiDebouncedAction _shopSelectionDebounce;

        /// <summary>
        /// Дебаунсер для зміни AvailabilityGroup (SelectedAvailabilityGroup).
        /// Після затримки він:
        /// - запише SelectedBlock.SelectedAvailabilityGroupId
        /// - і скине згенеровані слоти (InvalidateGeneratedSchedule)
        /// </summary>
        private readonly UiDebouncedAction _availabilitySelectionDebounce;

        /// <summary>
        /// Скасувати “відкладені” застосування selection.
        /// Корисно викликати при ResetForNew() або закритті форми.
        /// </summary>
        private void CancelSelectionDebounce()
        {
            _shopSelectionDebounce.Cancel();
            _availabilitySelectionDebounce.Cancel();
        }


        // ============================================================
        // 2) РЕАКЦІЯ НА ЗМІНУ SHOP (SelectedShop)
        // ============================================================

        /// <summary>
        /// Викликається із setter'а SelectedShop тоді, коли:
        /// - value реально змінилося (oldId != newId)
        /// - ми НЕ в режимі selection-sync (_selectionSyncDepth == 0)
        ///
        /// Що робить:
        /// - запускає debounce: чекаємо SelectionDebounceDelay
        /// - якщо за цей час користувач вибрав інший shop — нічого не робимо
        /// - якщо вибір все ще актуальний — записуємо ScheduleShopId = newId
        ///
        /// Важливо:
        /// - сам setter SelectedShop НЕ повинен одразу писати в модель,
        ///   бо це може викликати каскад оновлень (матриця, preview, тощо).
        /// - через debounce ми застосовуємо тільки “остаточний” вибір користувача.
        /// </summary>
        private void ScheduleShopSelectionChange(int newId)
        {
            _shopSelectionDebounce.Schedule(() =>
            {
                // 1) Перевірка актуальності.
                // Якщо користувач за час очікування вже вибрав інший shop,
                // то SelectedShop.Id буде іншим, і ми просто виходимо.
                if (SelectedShop?.Id != newId)
                    return;

                // 2) Записуємо в модель через властивість ScheduleShopId.
                // Це запускає SetScheduleValue(...) та пов'язані реакції/валідацію.
                ScheduleShopId = newId;
            });
        }


        // ============================================================
        // 3) РЕАКЦІЯ НА ЗМІНУ AVAILABILITY GROUP (SelectedAvailabilityGroup)
        // ============================================================

        /// <summary>
        /// Викликається із setter'а SelectedAvailabilityGroup тоді, коли:
        /// - value реально змінилося (oldId != newId)
        /// - SelectedBlock != null
        /// - ми НЕ в режимі selection-sync (_selectionSyncDepth == 0)
        /// - і НЕ активний suppress-флаг (_suppressAvailabilityGroupUpdate == false)
        ///
        /// Що робить після debounce:
        /// 1) ще раз перевіряє, що користувач не вибрав іншу групу за цей час
        /// 2) записує groupId у SelectedBlock.SelectedAvailabilityGroupId
        /// 3) скидає згенеровані слоти, бо вони залежать від групи доступності
        /// </summary>
        private void ScheduleAvailabilitySelectionChange(int newId)
        {
            _availabilitySelectionDebounce.Schedule(() =>
            {
                // 1) Перевірка актуальності.
                // Якщо вибір змінився знову — цей запуск застарів.
                if (SelectedAvailabilityGroup?.Id != newId)
                    return;

                // 2) Без SelectedBlock нема куди записувати.
                if (SelectedBlock is null)
                    return;

                // 3) Записуємо вибрану групу у блок (це “джерело правди”).
                SelectedBlock.SelectedAvailabilityGroupId = newId;

                // 4) Дуже важливо:
                // Зміна групи доступності змінює “правила”, за якими генерується розклад.
                // Тому старі згенеровані слоти стають недійсними.
                InvalidateGeneratedSchedule(clearPreviewMatrix: true);
                SafeForget(LoadAvailabilityContextAsync(newId));
            });
        }


        // ============================================================
        // 4) ІНВАЛІДАЦІЯ ЗГЕНЕРОВАНОГО РОЗКЛАДУ
        // ============================================================

        /// <summary>
        /// Скидає (інвалідить) вже згенерований розклад/матриці, коли змінилася база для генерації:
        /// - змінили рік/місяць
        /// - змінили availability group
        /// - інші параметри, які впливають на структуру слотів
        ///
        /// Параметр clearPreviewMatrix:
        /// - true  => очищаємо також AvailabilityPreviewMatrix та _availabilityPreviewKey
        /// - false => залишаємо preview як є (якщо сценарій дозволяє)
        ///
        /// Що робимо по кроках:
        /// 1) очищаємо SelectedBlock.Slots (бо вони більше не валідні)
        /// 2) очищаємо ScheduleMatrix (UI одразу стане “порожнім”)
        /// 3) за потреби очищаємо Preview матрицю та ключ актуальності preview
        /// 4) перераховуємо totals
        /// 5) піднімаємо MatrixChanged, щоб UI/слухачі оновилися
        /// </summary>
        internal void InvalidateGeneratedSchedule(bool clearPreviewMatrix = true)
        {
            if (SelectedBlock is null)
                return;

            // 1) Скидаємо вже згенеровані слоти
            if (SelectedBlock.Slots.Count > 0)
                SelectedBlock.Slots.Clear();

            // 2) Скидаємо основну матрицю (UI таблиця)
            ScheduleMatrix = new DataView();

            // 3) Опційно: скидаємо preview матрицю і її ключ актуальності
            if (clearPreviewMatrix)
            {
                AvailabilityPreviewMatrix = new DataView();
                _availabilityPreviewKey = null; // _availabilityPreviewKey живе в Matrix partial
            }

            // 4) Оновлюємо totals (години/кількість працівників)
            RecalculateTotals();

            // 5) Нотифікація, що матриця/стан змінився
            MatrixChanged?.Invoke(this, EventArgs.Empty);
        }

        private async Task LoadAvailabilityContextAsync(int availabilityGroupId)
        {
            if (SelectedBlock is null)
                return;

            if (availabilityGroupId <= 0)
            {
                await _owner.RunOnUiThreadAsync(() =>
                {
                    AvailabilityPreviewMatrix = new DataView();
                    _availabilityPreviewKey = null;
                    MatrixChanged?.Invoke(this, EventArgs.Empty);
                }).ConfigureAwait(false);

                return;
            }

            var year = ScheduleYear;
            var month = ScheduleMonth;
            if (year < 1 || month < 1 || month > 12)
                return;

            var expectedGroupId = availabilityGroupId;
            var expectedYear = year;
            var expectedMonth = month;

            var previewKey = $"AV|{availabilityGroupId}|{year}|{month}";

            try
            {
                // 1) Тягнемо group + members + days через BLL service
                var (group, members, days) =
                    await _availabilityGroupService.LoadFullAsync(availabilityGroupId, CancellationToken.None)
                                                   .ConfigureAwait(false);

                // Guard: користувач міг змінити selection поки тягнули дані
                if (SelectedAvailabilityGroup?.Id != expectedGroupId)
                    return;
                if (ScheduleYear != expectedYear || ScheduleMonth != expectedMonth)
                    return;

                // 2) Employees для "hours per employee" — завжди оновлюємо
                var employees = await ResolveEmployeesForMembersAsync(members, CancellationToken.None)
                    .ConfigureAwait(false);

                List<ScheduleEmployeeModel> scheduleEmployeesSnapshot = new();

                await _owner.RunOnUiThreadAsync(() =>
                {
                    ReplaceScheduleEmployeesFromAvailability(employees);
                    scheduleEmployeesSnapshot = SelectedBlock.Employees.ToList();
                }).ConfigureAwait(false);

                // 3) Якщо період group != schedule (year/month) — preview НЕ будуємо
                var periodMatched = (group.Year == year && group.Month == month);
                if (!periodMatched)
                {
                    await _owner.RunOnUiThreadAsync(() =>
                    {
                        AvailabilityPreviewMatrix = new DataView();
                        _availabilityPreviewKey = null;
                        MatrixChanged?.Invoke(this, EventArgs.Empty);
                    }).ConfigureAwait(false);

                    return;
                }

                // 4) Build availability slots для preview
                var shift1 = TryParseShiftIntervalText(ScheduleShift1);
                var shift2 = TryParseShiftIntervalText(ScheduleShift2);

                var (_, availabilitySlots) = AvailabilityPreviewBuilder.Build(
                    members,
                    days,
                    shift1,
                    shift2,
                    CancellationToken.None);

                // Guard ще раз перед важкою побудовою матриці
                if (SelectedAvailabilityGroup?.Id != expectedGroupId)
                    return;
                if (ScheduleYear != expectedYear || ScheduleMonth != expectedMonth)
                    return;

                // 5) Build preview matrix
                await RefreshAvailabilityPreviewMatrixAsync(
                        year, month,
                        availabilitySlots,
                        scheduleEmployeesSnapshot,
                        previewKey: previewKey)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _owner.RunOnUiThreadAsync(() =>
                {
                    CustomMessageBox.Show("Error", ex.Message, CustomMessageBoxIcon.Error, okText: "OK");
                }).ConfigureAwait(false);
            }
        }

        private void ReplaceScheduleEmployeesFromAvailability(IEnumerable<EmployeeModel> employees)
        {

            SetAvailabilityEmployees(employees);


            if (SelectedBlock is null)
                return;

            var availabilityIds = (employees ?? Enumerable.Empty<EmployeeModel>())
                .Where(e => e != null)
                .Select(e => e.Id)
                .Distinct()
                .ToHashSet();

            // manual = ті, кого вже було в schedule, але нема в availability group
            var manualEmployees = SelectedBlock.Employees
                .Where(se => se != null && !availabilityIds.Contains(se.EmployeeId))
                .ToList();

            // preserve existing MinHoursMonth by EmployeeId (тільки для availability employees)
            var oldMin = SelectedBlock.Employees
                .Where(se => se != null && availabilityIds.Contains(se.EmployeeId))
                .GroupBy(x => x.EmployeeId)
                .ToDictionary(g => g.Key, g => g.First().MinHoursMonth);

            SelectedBlock.Employees.Clear();

            foreach (var emp in employees
                .Where(e => e != null)
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName))
            {
                var min = oldMin.TryGetValue(emp.Id, out var v) ? (v ?? 0) : 0;

                SelectedBlock.Employees.Add(new ScheduleEmployeeModel
                {
                    EmployeeId = emp.Id,
                    Employee = emp,
                    MinHoursMonth = min
                });
            }

            // додаємо назад manual employees (в кінець)
            foreach (var se in manualEmployees)
            {
                if (se == null) continue;
                if (se.MinHoursMonth == null) se.MinHoursMonth = 0; // <-- додай
                if (SelectedBlock.Employees.Any(x => x.EmployeeId == se.EmployeeId))
                    continue;

                SelectedBlock.Employees.Add(se);
            }

            // оновлюємо manual ids і refresh view для MinHours
            _manualEmployeeIds.Clear();
            foreach (var se in manualEmployees)
                if (se != null && se.EmployeeId > 0)
                    _manualEmployeeIds.Add(se.EmployeeId);

            RebindMinHoursEmployeesView();
        }


        private void OnSchedulePeriodChanged()
        {
            // Якщо availability group вже вибраний — треба одразу оновити:
            // 1) список працівників (Min hours grid)
            // 2) Availability preview (якщо для цього року/місяця є дані)
            var groupId = SelectedAvailabilityGroup?.Id ?? 0;
            if (groupId <= 0)
                return;

            // Використовуємо той самий debounce, щоб не спамити запити при швидкій зміні значень.
            ScheduleAvailabilitySelectionChange(groupId);
        }

        private static bool TryGetAvailabilityGroupPeriod(AvailabilityGroupModel? group, out int year, out int month)
        {
            year = 0;
            month = 0;
            if (group is null) return false;

            static int ReadInt(object obj, string propName)
            {
                var p = obj.GetType().GetProperty(propName);
                if (p == null) return 0;

                var v = p.GetValue(obj);
                if (v is int i) return i;
                if (v is null) return 0;

                // інколи можуть прилітати строки (залежить від моделі/мапінгу)
                if (v is string s && int.TryParse(s, out var parsed))
                    return parsed;

                return 0;
            }

            year = ReadInt(group, "Year");
            month = ReadInt(group, "Month");

            return year > 0 && month is >= 1 and <= 12;
        }


        private bool IsAvailabilityPeriodMismatch(AvailabilityGroupModel? group, int scheduleYear, int scheduleMonth,
            out int groupYear, out int groupMonth)
        {
            groupYear = 0;
            groupMonth = 0;

            if (scheduleYear <= 0 || scheduleMonth is < 1 or > 12)
                return false;

            if (!TryGetAvailabilityGroupPeriod(group, out groupYear, out groupMonth))
                return false;

            return groupYear != scheduleYear || groupMonth != scheduleMonth;
        }

        private async Task<List<EmployeeModel>> ResolveEmployeesForMembersAsync(
    List<AvailabilityGroupMemberModel> members,
    CancellationToken ct)
        {
            // спроба взяти Employee прямо з members (якщо repo робив Include)
            var empById = new Dictionary<int, EmployeeModel>();
            var neededIds = new HashSet<int>();

            foreach (var m in members)
            {
                neededIds.Add(m.EmployeeId);

                if (m.Employee != null)
                    empById[m.EmployeeId] = m.Employee;
            }

            // якщо якихось Employee не вистачає — добираємо через EmployeeService.GetAllAsync і фільтруємо
            if (empById.Count != neededIds.Count)
            {
                var all = await _employeeService.GetAllAsync(ct).ConfigureAwait(false);
                foreach (var e in all)
                {
                    if (neededIds.Contains(e.Id))
                        empById[e.Id] = e;
                }

                // прокинемо Employee назад у member, щоб AvailabilityPreviewBuilder мав m.Employee
                foreach (var m in members)
                {
                    if (m.Employee == null && empById.TryGetValue(m.EmployeeId, out var e))
                        m.Employee = e;
                }
            }

            return empById.Values
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToList();
        }

        private static (string from, string to)? TryParseShiftIntervalText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            // нормалізуємо до "HH:mm-HH:mm"
            if (!AvailabilityCodeParser.TryNormalizeInterval(text, out var normalized))
                return null;

            var parts = normalized.Split('-', 2,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length != 2)
                return null;

            return (parts[0], parts[1]);
        }



    }
}
