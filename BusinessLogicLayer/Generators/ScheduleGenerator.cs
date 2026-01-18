using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using System.Diagnostics;

namespace BusinessLogicLayer.Generators
{

    public interface IScheduleGenerator
    {
        Task<IList<ScheduleSlotModel>> GenerateAsync(
            ScheduleModel schedule,
            IEnumerable<AvailabilityGroupModel> availabilities,
            IEnumerable<ScheduleEmployeeModel> employees,
            CancellationToken ct = default);
    }


    public class ScheduleGenerator : IScheduleGenerator
    {
        private sealed class EmployeeStats
        {
            public double TotalHours { get; set; }

            public int? LastWorkedDay { get; set; }
            public int ConsecutiveDays { get; set; }

            public int FullDays { get; set; }
            public int? LastFullDay { get; set; }
            public int ConsecutiveFullDays { get; set; }
        }

        private sealed class ShiftTemplate
        {
            /// <summary>Технічний індекс (1,2) – відповідає Shift1 / Shift2.</summary>
            public int Index { get; init; }

            /// <summary>Час початку у форматі "HH:mm".</summary>
            public string From { get; init; } = null!;

            /// <summary>Час завершення у форматі "HH:mm".</summary>
            public string To { get; init; } = null!;

            /// <summary>Тривалість зміни в годинах.</summary>
            public double Hours { get; init; }
        }

        public Task<IList<ScheduleSlotModel>> GenerateAsync(
            ScheduleModel schedule,
            IEnumerable<AvailabilityGroupModel> availabilities,
            IEnumerable<ScheduleEmployeeModel> employees,
            CancellationToken ct = default)
        {
            var result = new List<ScheduleSlotModel>();

            // з кого взагалі можна будувати графік
            var employeeIds = new List<int>();
            var employeeIdSet = new HashSet<int>();
            foreach (var employee in employees)
            {
                if (!employeeIdSet.Add(employee.EmployeeId)) continue;
                employeeIds.Add(employee.EmployeeId);
            }

            if (employeeIds.Count == 0)
                return Task.FromResult<IList<ScheduleSlotModel>>(result);

            // шаблони змін із Schedule.Shift1Time / Shift2Time
            var shiftTemplates = GetShiftTemplates(schedule);
            if (shiftTemplates.Count == 0)
                return Task.FromResult<IList<ScheduleSlotModel>>(result);

            // черга для fair-розподілу
            var employeeQueue = new Queue<int>(employeeIds);

            // статистика по кожному співробітнику
            var stats = new Dictionary<int, EmployeeStats>(employeeIds.Count);
            foreach (var id in employeeIds)
                stats[id] = new EmployeeStats();

            var daysInMonth = DateTime.DaysInMonth(schedule.Year, schedule.Month);

            var availabilityIndex = BuildAvailabilityIndex(availabilities, schedule.Year, schedule.Month);


            for (var day = 1; day <= daysInMonth; day++)
            {
                ct.ThrowIfCancellationRequested();
#if DEBUG
                if (day <= 7)
                {
                    var date = new DateTime(schedule.Year, schedule.Month, day);
                    var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                    Debug.WriteLine($"[ScheduleGenerator] {date:yyyy-MM-dd} {date.DayOfWeek} weekend={isWeekend}");
                }
#endif

                // хто в принципі може працювати в цей день
                var availableToday = new List<int>(employeeIds.Count);
                foreach (var id in employeeIds)
                {
                    if (!availabilityIndex.TryGetValue(id, out var dayMap) ||
                        !dayMap.TryGetValue(day, out var kind) ||
                        kind != AvailabilityKind.NONE)
                    {
                        availableToday.Add(id);
                    }
                }

                var availableTodaySet = new HashSet<int>(availableToday);



                // скільки змін (0/1/2...) людина вже працює цього дня
                var shiftsToday = new Dictionary<int, int>(employeeIds.Count);
                foreach (var id in employeeIds)
                    shiftsToday[id] = 0;

                foreach (var shift in shiftTemplates)
                {
                    // один працівник може бути тільки раз у (day, конкретна зміна)
                    var assignedThisShift = new HashSet<int>();

                    for (var slotNo = 1; slotNo <= schedule.PeoplePerShift; slotNo++)
                    {
                        var slotModel = new ScheduleSlotModel
                        {
                            DayOfMonth = day,        // чисто технічне поле; UI може його навіть не показувати
                            SlotNo = slotNo,
                            FromTime = shift.From,
                            ToTime = shift.To,
                            Status = SlotStatus.UNFURNISHED
                        };

                        var emp = GetNextEmployee(employeeQueue, availableTodaySet, assignedThisShift, day, schedule, stats, shiftsToday, shift.Hours);


                        if (emp.HasValue)
                        {
                            var id = emp.Value;

                            slotModel.EmployeeId = id;
                            slotModel.Status = SlotStatus.ASSIGNED;

                            assignedThisShift.Add(id);
                            shiftsToday[id] = shiftsToday[id] + 1;

                            UpdateStatsOnAssignment(id, day, stats, shiftsToday, shift.Hours);
                        }

                        result.Add(slotModel);
                    }
                }
            }

            return Task.FromResult<IList<ScheduleSlotModel>>(result);
        }


        private static Dictionary<int, Dictionary<int, AvailabilityKind>> BuildAvailabilityIndex(
            IEnumerable<AvailabilityGroupModel> groups,
            int year,
            int month)
        {
            var index = new Dictionary<int, Dictionary<int, AvailabilityKind>>();

            foreach (var g in groups)
            {
                if (g.Year != year || g.Month != month) continue;

                var members = g.Members;
                if (members is null) continue;

                foreach (var m in members)
                {
                    if (!index.TryGetValue(m.EmployeeId, out var dayMap))
                    {
                        dayMap = new Dictionary<int, AvailabilityKind>();
                        index[m.EmployeeId] = dayMap;
                    }

                    var days = m.Days;
                    if (days is null) continue;

                    foreach (var d in days)
                    {
                        // якщо є дублікати — NONE має пріоритет (найжорсткіше правило)
                        if (dayMap.TryGetValue(d.DayOfMonth, out var existing))
                        {
                            if (existing == AvailabilityKind.NONE) continue;
                            if (d.Kind == AvailabilityKind.NONE) dayMap[d.DayOfMonth] = AvailabilityKind.NONE;
                            else dayMap[d.DayOfMonth] = d.Kind;
                        }
                        else
                        {
                            dayMap[d.DayOfMonth] = d.Kind;
                        }
                    }
                }
            }

            return index;
        }

        private static IEnumerable<int> GetAvailableEmployees(
            IReadOnlyDictionary<int, Dictionary<int, AvailabilityKind>> availabilityIndex,
            int day)
        {
            // стара логіка: якщо дня немає -> доступний; якщо Kind != NONE -> доступний
            foreach (var (employeeId, dayMap) in availabilityIndex)
            {
                if (!dayMap.TryGetValue(day, out var kind) || kind != AvailabilityKind.NONE)
                    yield return employeeId;
            }
        }


        private static int? GetNextEmployee(
            Queue<int> queue,
            ISet<int> allowedToday,
            HashSet<int> assignedThisShift,
            int day,
            ScheduleModel schedule,
            IDictionary<int, EmployeeStats> stats,
            IDictionary<int, int> shiftsToday,
            double shiftDurationHours)
        {
            if (allowedToday.Count == 0 || queue.Count == 0)
                return null;

            var rotations = queue.Count;
            while (rotations-- > 0)
            {
                var candidate = queue.Dequeue();
                queue.Enqueue(candidate);

                if (!allowedToday.Contains(candidate))
                    continue;

                if (assignedThisShift.Contains(candidate))
                    continue;

                if (!stats.TryGetValue(candidate, out var st))
                    continue;

                var shiftsAlreadyToday = shiftsToday.TryGetValue(candidate, out var v) ? v : 0;
                if (!CanAssign(day, schedule, st, shiftsAlreadyToday, shiftDurationHours))
                    continue;

                return candidate;
            }

            // нікого не вийшло призначити на цей слот
            return null;
        }

        /// <summary>
        /// Перевіряє, чи не порушимо ми ліміти, якщо додамо ще одну зміну цьому працівнику.
        /// </summary>
        private static bool CanAssign(
            int day,
            ScheduleModel schedule,
            EmployeeStats st,
            int shiftsAlreadyToday,
            double shiftDurationHours)
        {
            // 1) обмеження по годинах за місяць
            if (schedule.MaxHoursPerEmpMonth > 0)
            {
                var newHours = st.TotalHours + shiftDurationHours;
                if (newHours > schedule.MaxHoursPerEmpMonth + 1e-6)
                    return false;
            }

            // 2) поспіль робочі дні – перевіряємо тільки коли це перша зміна в новий день
            if (schedule.MaxConsecutiveDays > 0 && shiftsAlreadyToday == 0)
            {
                int newConsecutive;
                if (st.LastWorkedDay == day - 1)
                    newConsecutive = (st.ConsecutiveDays <= 0 ? 0 : st.ConsecutiveDays) + 1;
                else if (st.LastWorkedDay == day)
                    newConsecutive = st.ConsecutiveDays; // теоретично не повинно статись
                else
                    newConsecutive = 1;

                if (newConsecutive > schedule.MaxConsecutiveDays)
                    return false;
            }

            // 3) повні дні (2 зміни) – перевіряємо, якщо зараз буде друга зміна за день
            if (shiftsAlreadyToday == 1 &&
                (schedule.MaxFullPerMonth > 0 || schedule.MaxConsecutiveFull > 0))
            {
                var newFullDays = st.FullDays + 1;

                if (schedule.MaxFullPerMonth > 0 &&
                    newFullDays > schedule.MaxFullPerMonth)
                    return false;

                int newConsecutiveFull;
                if (st.LastFullDay == day - 1)
                    newConsecutiveFull = (st.ConsecutiveFullDays <= 0 ? 0 : st.ConsecutiveFullDays) + 1;
                else
                    newConsecutiveFull = 1;

                if (schedule.MaxConsecutiveFull > 0 &&
                    newConsecutiveFull > schedule.MaxConsecutiveFull)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Оновлюємо статистику після того, як зміна вже призначена.
        /// </summary>
        private static void UpdateStatsOnAssignment(
            int employeeId,
            int day,
            IDictionary<int, EmployeeStats> allStats,
            IDictionary<int, int> shiftsToday,
            double shiftDurationHours)
        {
            if (!allStats.TryGetValue(employeeId, out var st))
                return;

            st.TotalHours += shiftDurationHours;

            var shiftsCountToday = shiftsToday.TryGetValue(employeeId, out var v) ? v : 0;

            // оновлюємо лічильник робочих днів,
            // але тільки коли це перша зміна в цьому дні
            if (shiftsCountToday == 1)
            {
                if (st.LastWorkedDay == day - 1)
                    st.ConsecutiveDays = (st.ConsecutiveDays <= 0 ? 0 : st.ConsecutiveDays) + 1;
                else if (st.LastWorkedDay != day)
                    st.ConsecutiveDays = 1;

                st.LastWorkedDay = day;
            }

            // повний день – коли у людини вже 2 зміни за день
            if (shiftsCountToday == 2)
            {
                st.FullDays += 1;

                if (st.LastFullDay == day - 1)
                    st.ConsecutiveFullDays = (st.ConsecutiveFullDays <= 0 ? 0 : st.ConsecutiveFullDays) + 1;
                else
                    st.ConsecutiveFullDays = 1;

                st.LastFullDay = day;
            }
        }

        /// <summary>
        /// Створює список шаблонів змін з Schedule.Shift1Time / Shift2Time.
        /// ShiftXTime очікуємо у форматі "HH:mm-HH:mm" або "HH:mm - HH:mm".
        /// Зберігаємо час у нормалізованому вигляді "HH:mm".
        /// </summary>
        private static List<ShiftTemplate> GetShiftTemplates(ScheduleModel schedule)
        {
            var list = new List<ShiftTemplate>();

            if (TryCreateShiftTemplate(schedule.Shift1Time, 1, out var t1))
                list.Add(t1);

            if (TryCreateShiftTemplate(schedule.Shift2Time, 2, out var t2))
                list.Add(t2);

            return list;
        }

        private static bool TryCreateShiftTemplate(string? shiftText, int index, out ShiftTemplate template)
        {
            template = null!;

            if (string.IsNullOrWhiteSpace(shiftText))
                return false;

            // Допускаємо "09:00-15:00" і "09:00 - 15:00"
            var cleaned = shiftText.Replace(" ", string.Empty);
            var parts = cleaned.Split('-', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
                throw new InvalidOperationException(
                    $"Invalid shift format '{shiftText}'. Expected 'HH:mm-HH:mm' or 'HH:mm - HH:mm'.");

            if (!TimeSpan.TryParse(parts[0], out var start) ||
                !TimeSpan.TryParse(parts[1], out var end))
            {
                throw new InvalidOperationException(
                    $"Invalid shift time '{shiftText}'. Cannot parse start/end as time.");
            }

            // Працюємо в межах одного дня: кінець має бути строго пізніше початку
            if (end <= start)
            {
                throw new InvalidOperationException(
                    $"Invalid shift time '{shiftText}'. End time must be after start time within the same day.");
            }

            var fromStr = start.ToString(@"hh\:mm");
            var toStr = end.ToString(@"hh\:mm");
            var hours = (end - start).TotalHours;

            template = new ShiftTemplate
            {
                Index = index,
                From = fromStr,
                To = toStr,
                Hours = hours
            };

            return true;
        }
    }
}
