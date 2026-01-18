using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Abstractions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services
{
    public class ScheduleService : GenericService<ScheduleModel>, IScheduleService
    {
        private readonly IScheduleRepository _scheduleRepo;
        private readonly IScheduleEmployeeRepository _employeeRepo;
        private readonly IScheduleSlotRepository _slotRepo;
        private readonly IScheduleCellStyleRepository _cellStyleRepo;

        public ScheduleService(
            IScheduleRepository scheduleRepo,
            IScheduleEmployeeRepository employeeRepo,
            IScheduleSlotRepository slotRepo,
            IScheduleCellStyleRepository cellStyleRepo) : base(scheduleRepo)
        {
            _scheduleRepo = scheduleRepo;
            _employeeRepo = employeeRepo;
            _slotRepo = slotRepo;
            _cellStyleRepo = cellStyleRepo;
        }

        public Task<List<ScheduleModel>> GetByContainerAsync(int containerId, string? value = null, CancellationToken ct = default)
            => _scheduleRepo.GetByContainerAsync(containerId, value, ct);

        public Task<List<ScheduleModel>> GetByValueAsync(string value, CancellationToken ct = default)
            => _scheduleRepo.GetByValueAsync(value, ct);

        public override Task<ScheduleModel> CreateAsync(ScheduleModel entity, CancellationToken ct = default)
        {
            NormalizeSchedule(entity);
            return base.CreateAsync(entity, ct);
        }

        public override Task UpdateAsync(ScheduleModel entity, CancellationToken ct = default)
        {
            NormalizeSchedule(entity);
            return base.UpdateAsync(entity, ct);
        }

        public async Task SaveWithDetailsAsync(
            ScheduleModel schedule,
            IEnumerable<ScheduleEmployeeModel> employees,
            IEnumerable<ScheduleSlotModel> slots,
            IEnumerable<ScheduleCellStyleModel> cellStyles,
            CancellationToken ct = default)
        {
            NormalizeSchedule(schedule);

            if (schedule.Id == 0)
                schedule = await _scheduleRepo.AddAsync(schedule, ct).ConfigureAwait(false);
            else
                await _scheduleRepo.UpdateAsync(schedule, ct).ConfigureAwait(false);

            var scheduleId = schedule.Id;

            // ВАЖЛИВО: не даємо EF інсертити Employee ще раз
            foreach (var e in employees)
            {
                e.Employee = null!; // навігація не потрібна при збереженні
            }

            foreach (var s in slots)
            {
                s.Employee = null;
            }

            var cellStyleList = cellStyles?.ToList() ?? new List<ScheduleCellStyleModel>();
#if DEBUG
            Debug.WriteLine($"[ScheduleService] Saving {cellStyleList.Count} cell styles for schedule {scheduleId}");
            foreach (var style in cellStyleList.Take(3))
            {
                Debug.WriteLine(
                    $"[ScheduleService] Style save day={style.DayOfMonth} emp={style.EmployeeId} " +
                    $"bg={style.BackgroundHex ?? "none"} fg={style.ForegroundHex ?? "none"}");
            }
#endif

            // replace employees
            var existingEmployees = await _employeeRepo.GetByScheduleAsync(scheduleId, ct).ConfigureAwait(false);
            foreach (var e in existingEmployees)
                await _employeeRepo.DeleteAsync(e.Id, ct).ConfigureAwait(false);

            foreach (var e in employees)
            {
                e.Id = 0;
                e.ScheduleId = scheduleId;
                await _employeeRepo.AddAsync(e, ct).ConfigureAwait(false);
            }

            // replace slots
            var existingSlots = await _slotRepo.GetByScheduleAsync(scheduleId, ct).ConfigureAwait(false);
            foreach (var s in existingSlots)
                await _slotRepo.DeleteAsync(s.Id, ct).ConfigureAwait(false);

            foreach (var s in slots)
            {
                s.Id = 0;
                s.ScheduleId = scheduleId;
                await _slotRepo.AddAsync(s, ct).ConfigureAwait(false);
            }

            // replace cell styles
            var existingStyles = await _cellStyleRepo.GetByScheduleAsync(scheduleId, ct).ConfigureAwait(false);
            foreach (var style in existingStyles)
                await _cellStyleRepo.DeleteAsync(style.Id, ct).ConfigureAwait(false);

            foreach (var style in cellStyleList)
            {
                style.Id = 0;
                style.ScheduleId = scheduleId;
                style.Schedule = null!;
                await _cellStyleRepo.AddAsync(style, ct).ConfigureAwait(false);
            }
        }


        public async Task<ScheduleModel?> GetDetailedAsync(int id, CancellationToken ct = default)
        {
            // Беремо сам графік (з Shop/Container, якщо репозиторій це вміє)
            var schedule = await _scheduleRepo.GetDetailedAsync(id, ct).ConfigureAwait(false);
            if (schedule is null)
                return null;

            // А тепер дочірні дані – працівники та слоти
            schedule.Employees = (await _employeeRepo.GetByScheduleAsync(id, ct).ConfigureAwait(false)).ToList();
            schedule.Slots = (await _slotRepo.GetByScheduleAsync(id, ct).ConfigureAwait(false)).ToList();
            schedule.CellStyles = (await _cellStyleRepo.GetByScheduleAsync(id, ct).ConfigureAwait(false)).ToList();

#if DEBUG
            Debug.WriteLine($"[ScheduleService] Loaded {schedule.CellStyles.Count} cell styles for schedule {id}");
            foreach (var style in schedule.CellStyles.Take(3))
            {
                Debug.WriteLine(
                    $"[ScheduleService] Style load day={style.DayOfMonth} emp={style.EmployeeId} " +
                    $"bg={style.BackgroundHex ?? "none"} fg={style.ForegroundHex ?? "none"}");
            }
#endif

            return schedule;
        }

        private static void NormalizeSchedule(ScheduleModel schedule)
        {
            if (schedule is null)
                return;

            schedule.Note = string.IsNullOrWhiteSpace(schedule.Note)
                ? null
                : schedule.Note.Trim();
        }
    }
}
