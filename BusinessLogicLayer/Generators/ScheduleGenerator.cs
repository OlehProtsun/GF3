using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Generators
{
    public interface IScheduleGenerator
    {
        Task<IList<ScheduleSlotModel>> GenerateAsync(
            ScheduleModel schedule,
            IEnumerable<AvailabilityMonthModel> availabilities,
            IEnumerable<ScheduleEmployeeModel> employees,
            CancellationToken ct = default);
    }

    /// <summary>
    /// Placeholder generator; implementation will be provided later.
    /// </summary>
    public class ScheduleGenerator : IScheduleGenerator
    {
        public Task<IList<ScheduleSlotModel>> GenerateAsync(
            ScheduleModel schedule,
            IEnumerable<AvailabilityMonthModel> availabilities,
            IEnumerable<ScheduleEmployeeModel> employees,
            CancellationToken ct = default)
        {
            var result = new List<ScheduleSlotModel>();
            var employeeQueue = new Queue<int>(employees.Select(e => e.EmployeeId));
            var daysInMonth = DateTime.DaysInMonth(schedule.Year, schedule.Month);

            for (var day = 1; day <= daysInMonth; day++)
            {
                ct.ThrowIfCancellationRequested();

                var availableToday = GetAvailableEmployees(availabilities, schedule.Year, schedule.Month, day)
                    .Where(employeeQueue.Contains)
                    .ToList();

                for (var shift = 1; shift <= 2; shift++)
                {
                    for (var slot = 1; slot <= schedule.PeoplePerShift; slot++)
                    {
                        var slotModel = new ScheduleSlotModel
                        {
                            ScheduleId = schedule.Id,
                            DayOfMonth = day,
                            ShiftNo = shift,
                            SlotNo = slot,
                            Status = SlotStatus.UNFURNISHED
                        };

                        if (availableToday.Count > 0)
                        {
                            var emp = RotateQueue(employeeQueue, availableToday);
                            if (emp.HasValue)
                            {
                                slotModel.EmployeeId = emp.Value;
                                slotModel.Status = SlotStatus.ASSIGNED;
                            }
                        }

                        result.Add(slotModel);
                    }
                }
            }

            return Task.FromResult<IList<ScheduleSlotModel>>(result);
        }

        private static IEnumerable<int> GetAvailableEmployees(
            IEnumerable<AvailabilityMonthModel> availabilities,
            int year,
            int month,
            int day)
        {
            var byDate = availabilities.Where(a => a.Year == year && a.Month == month);
            foreach (var availability in byDate)
            {
                var dayInfo = availability.Days.FirstOrDefault(d => d.DayOfMonth == day);
                if (dayInfo is null || dayInfo.Kind != AvailabilityKind.NONE)
                    yield return availability.EmployeeId;
            }
        }

        private static int? RotateQueue(Queue<int> queue, IList<int> allowed)
        {
            var rotations = queue.Count;
            while (rotations-- > 0)
            {
                var candidate = queue.Dequeue();
                queue.Enqueue(candidate);
                if (allowed.Contains(candidate))
                    return candidate;
            }

            return null;
        }
    }
}
