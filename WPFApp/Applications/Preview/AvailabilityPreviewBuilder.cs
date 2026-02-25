using BusinessLogicLayer.Availability;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Threading;

namespace WPFApp.Applications.Preview
{
    /// <summary>
    /// AvailabilityPreviewBuilder — “двигун” побудови даних для preview-матриці доступності.
    ///
    /// ЩО він робить:
    /// - бере AvailabilityGroup members + days
    /// - перетворює їх у:
    ///   1) список ScheduleEmployeeModel (для колонок матриці)
    ///   2) список ScheduleSlotModel (для заповнення клітинок)
    ///
    /// НАВІЩО це винесено з ContainerViewModel:
    /// - це НЕ UI-логіка і НЕ навігація
    /// - це чиста трансформація даних (data -> data)
    /// - легко тестувати, повторно використовувати, і підтримувати
    ///
    /// Вхідні параметри shift1/shift2:
    /// - якщо AvailabilityKind.ANY, то day означає: “працівник доступний на всю зміну”
    /// - shift1/shift2 задають інтервали (from,to), які треба підставити як слоти
    /// - якщо shift2 == null, то додається тільки shift1
    /// </summary>
    public static class AvailabilityPreviewBuilder
    {
        /// <summary>
        /// Побудувати preview employees + slots.
        ///
        /// members: члени групи доступності (хто входить у групу)
        /// days: записи по днях (availability) для memberId
        /// shift1/shift2: інтервали змін, які використовуються для AvailabilityKind.ANY
        /// ct: токен скасування (щоб швидко зупиняти build при новому запиті)
        ///
        /// Повертає:
        /// - Employees: унікальні працівники (EmployeeId + reference Employee)
        /// - Slots: унікальні слоти (EmployeeId + DayOfMonth + FromTime/ToTime)
        /// </summary>
        public static (List<ScheduleEmployeeModel> Employees, List<ScheduleSlotModel> Slots)
            Build(
                IReadOnlyList<AvailabilityGroupMemberModel> members,
                IReadOnlyList<AvailabilityGroupDayModel> days,
                (string from, string to)? shift1,
                (string from, string to)? shift2,
                CancellationToken ct)
        {
            // employees: майбутні колонки матриці
            var employees = new List<ScheduleEmployeeModel>(capacity: Math.Max(16, members.Count));

            // slots: “клітинки” матриці (що буде показано у DataTable)
            var slots = new List<ScheduleSlotModel>(capacity: Math.Max(64, days.Count));

            // seenEmp: захист від дублювання EmployeeId у employees
            var seenEmp = new HashSet<int>();

            // seenSlot: захист від дублювання слотів (один і той же emp/day/from/to вдруге не додаємо)
            var seenSlot = new HashSet<(int empId, int day, string from, string to)>();

            // daysByMember: швидкий індекс memberId -> list of days
            // щоб не робити O(members*days)
            var daysByMember = new Dictionary<int, List<AvailabilityGroupDayModel>>(capacity: Math.Max(16, members.Count));

            // 1) Індексуємо days по memberId
            for (int i = 0; i < days.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var d = days[i];
                if (!daysByMember.TryGetValue(d.AvailabilityGroupMemberId, out var list))
                    daysByMember[d.AvailabilityGroupMemberId] = list = new List<AvailabilityGroupDayModel>(8);

                list.Add(d);
            }

            // 2) Проходимо по members і будуємо employees + slots
            for (int i = 0; i < members.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var m = members[i];

                // 2.1) employees: додаємо працівника 1 раз (по EmployeeId)
                if (seenEmp.Add(m.EmployeeId))
                {
                    employees.Add(new ScheduleEmployeeModel
                    {
                        EmployeeId = m.EmployeeId,
                        Employee = m.Employee
                    });
                }

                // 2.2) Якщо для цього member нема days — нічого не додаємо
                if (!daysByMember.TryGetValue(m.Id, out var mdays) || mdays.Count == 0)
                    continue;

                // 2.3) Перебираємо day записи для цього member
                for (int j = 0; j < mdays.Count; j++)
                {
                    ct.ThrowIfCancellationRequested();

                    var d = mdays[j];

                    // NONE: недоступний — пропускаємо
                    if (d.Kind == AvailabilityKind.NONE)
                        continue;

                    // INT: доступний на конкретний інтервал (з d.IntervalStr)
                    if (d.Kind == AvailabilityKind.INT)
                    {
                        if (string.IsNullOrWhiteSpace(d.IntervalStr))
                            continue;

                        // Normalize interval через AvailabilityCodeParser (ваш парсер форматів)
                        if (!AvailabilityCodeParser.TryNormalizeInterval(d.IntervalStr, out var normalized))
                            continue;

                        // normalized очікується як "HH:mm-HH:mm"
                        if (TrySplitInterval(normalized, out var f, out var t))
                            AddSlotUnique(m.EmployeeId, d.DayOfMonth, f, t);

                        continue;
                    }

                    // ANY: доступний на “повну зміну”
                    // тоді додаємо shift1/shift2 як слоти
                    if (d.Kind == AvailabilityKind.ANY)
                    {
                        if (shift1 != null)
                            AddSlotUnique(m.EmployeeId, d.DayOfMonth, shift1.Value.from, shift1.Value.to);

                        if (shift2 != null)
                            AddSlotUnique(m.EmployeeId, d.DayOfMonth, shift2.Value.from, shift2.Value.to);
                    }
                }
            }

            return (employees, slots);

            // -------------------------
            // Локальні helper-и
            // -------------------------

            void AddSlotUnique(int empId, int day, string from, string to)
            {
                // Якщо вже додавали такий слот — пропускаємо
                if (!seenSlot.Add((empId, day, from, to)))
                    return;

                // SlotNo тут завжди 1, бо це preview (а не реальний згенерований schedule)
                slots.Add(new ScheduleSlotModel
                {
                    EmployeeId = empId,
                    DayOfMonth = day,
                    FromTime = from,
                    ToTime = to,
                    SlotNo = 1,
                    Status = SlotStatus.UNFURNISHED
                });
            }

            static bool TrySplitInterval(string normalized, out string from, out string to)
            {
                from = to = string.Empty;

                // розбиваємо "HH:mm-HH:mm"
                var parts = normalized.Split('-', 2,
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (parts.Length != 2)
                    return false;

                from = parts[0];
                to = parts[1];
                return true;
            }
        }
    }
}
