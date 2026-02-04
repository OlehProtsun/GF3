using BusinessLogicLayer.Common;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories.Abstractions;
using System.Collections.Generic;
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
        private readonly AppDbContext _db;

        public ScheduleService(
            IScheduleRepository scheduleRepo,
            IScheduleEmployeeRepository employeeRepo,
            IScheduleSlotRepository slotRepo,
            IScheduleCellStyleRepository cellStyleRepo,
            AppDbContext db) : base(scheduleRepo)
        {
            _scheduleRepo = scheduleRepo;
            _employeeRepo = employeeRepo;
            _slotRepo = slotRepo;
            _cellStyleRepo = cellStyleRepo;
            _db = db;
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
            EnsureGenerated(schedule, slots);

            var employeeList = employees?.ToList() ?? new List<ScheduleEmployeeModel>();
            var slotList = slots?.ToList() ?? new List<ScheduleSlotModel>();
            var cellStyleList = cellStyles?.ToList() ?? new List<ScheduleCellStyleModel>();

            ValidateScheduleDetails(schedule, employeeList, slotList, cellStyleList);

            await using var transaction = await _db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

            try
            {
                if (schedule.Id == 0)
                {
                    var created = await _scheduleRepo.AddAsync(schedule, ct).ConfigureAwait(false);
                    schedule.Id = created.Id;
                }
                else
                {
                    await _scheduleRepo.UpdateAsync(schedule, ct).ConfigureAwait(false);
                }

                var scheduleId = schedule.Id;
                if (scheduleId <= 0)
                    throw new ValidationException("Schedule could not be saved. Please try again.");

                AssignScheduleId(scheduleId, employeeList, slotList, cellStyleList);
                ValidateAssignedScheduleId(scheduleId, employeeList, slotList, cellStyleList);

                // ВАЖЛИВО: не даємо EF інсертити Employee ще раз
                foreach (var e in employeeList)
                {
                    e.Employee = null!; // навігація не потрібна при збереженні
                }

                foreach (var s in slotList)
                {
                    s.Employee = null;
                }


                // replace employees
                var existingEmployees = await _employeeRepo.GetByScheduleAsync(scheduleId, ct).ConfigureAwait(false);
                foreach (var e in existingEmployees)
                    await _employeeRepo.DeleteAsync(e.Id, ct).ConfigureAwait(false);

                foreach (var e in employeeList)
                {
                    e.Id = 0;
                    e.ScheduleId = scheduleId;
                    await _employeeRepo.AddAsync(e, ct).ConfigureAwait(false);
                }

                // replace slots
                var existingSlots = await _slotRepo.GetByScheduleAsync(scheduleId, ct).ConfigureAwait(false);
                foreach (var s in existingSlots)
                    await _slotRepo.DeleteAsync(s.Id, ct).ConfigureAwait(false);

                foreach (var s in slotList)
                {
                    s.Id = 0;
                    s.ScheduleId = scheduleId;
                    await _slotRepo.AddAsync(s, ct).ConfigureAwait(false);
                }

                var normalizedStyles = (cellStyleList ?? Enumerable.Empty<ScheduleCellStyleModel>())
                    .Where(cs => cs.BackgroundColorArgb.HasValue || cs.TextColorArgb.HasValue)
                    .ToList();

                foreach (var style in normalizedStyles)
                {
                    style.Schedule = null!;
                    style.Employee = null!;
                }

                var existingStyles = await _cellStyleRepo.GetByScheduleAsync(scheduleId, ct).ConfigureAwait(false);
                foreach (var style in existingStyles)
                    await _cellStyleRepo.DeleteAsync(style.Id, ct).ConfigureAwait(false);

                foreach (var style in normalizedStyles)
                {
                    style.Id = 0;
                    style.ScheduleId = scheduleId;
                    await _cellStyleRepo.AddAsync(style, ct).ConfigureAwait(false);
                }

                await transaction.CommitAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                await transaction.RollbackAsync(ct).ConfigureAwait(false);
                throw;
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

        private static void EnsureGenerated(ScheduleModel schedule, IEnumerable<ScheduleSlotModel> slots)
        {
            if (schedule is null)
                throw new ValidationException("Schedule is required.");

            if (schedule.AvailabilityGroupId is null || schedule.AvailabilityGroupId <= 0)
                throw new ValidationException("You can’t save a schedule until something has been generated. Please run generation first.");

            if (slots == null || !slots.Any())
                throw new ValidationException("You can’t save a schedule until something has been generated. Please run generation first.");
        }

        private static void ValidateScheduleDetails(
            ScheduleModel schedule,
            IReadOnlyCollection<ScheduleEmployeeModel> employees,
            IReadOnlyCollection<ScheduleSlotModel> slots,
            IReadOnlyCollection<ScheduleCellStyleModel> cellStyles)
        {
            if (schedule is null)
                throw new ValidationException("Schedule is required.");

            if (string.IsNullOrWhiteSpace(schedule.Name))
                throw new ValidationException("Schedule name is required.");

            if (schedule.ContainerId <= 0 || schedule.ShopId <= 0)
                throw new ValidationException("Schedule must be linked to a container and shop.");

            if (string.IsNullOrWhiteSpace(schedule.Shift1Time) || string.IsNullOrWhiteSpace(schedule.Shift2Time))
                throw new ValidationException("Schedule shift times are required.");

            var duplicateEmployees = employees
                .GroupBy(e => e.EmployeeId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateEmployees.Count > 0)
                throw new ValidationException($"Duplicate employees in schedule: {string.Join(", ", duplicateEmployees)}");

            var duplicateSlots = slots
                .GroupBy(s => new { s.DayOfMonth, s.FromTime, s.ToTime, s.SlotNo })
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateSlots.Count > 0)
                throw new ValidationException("Duplicate slots detected for the same day/time/slot number.");

            var duplicateEmployeeSlots = slots
                .Where(s => s.EmployeeId.HasValue)
                .GroupBy(s => new { s.DayOfMonth, s.FromTime, s.ToTime, s.EmployeeId })
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateEmployeeSlots.Count > 0)
                throw new ValidationException("Duplicate employee slots detected for the same time.");

            var duplicateCellStyles = cellStyles
                .GroupBy(cs => new { cs.DayOfMonth, cs.EmployeeId })
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateCellStyles.Count > 0)
                throw new ValidationException("Duplicate cell styles detected for the same day/employee.");

            foreach (var slot in slots)
            {
                if (slot.DayOfMonth <= 0)
                    throw new ValidationException("Schedule slot day must be between 1 and 31.");

                if (string.IsNullOrWhiteSpace(slot.FromTime) || string.IsNullOrWhiteSpace(slot.ToTime))
                    throw new ValidationException("Schedule slot times are required.");

                if (slot.Status == SlotStatus.ASSIGNED && slot.EmployeeId is null)
                    throw new ValidationException("Assigned slots must have an employee.");

                if (slot.Status == SlotStatus.UNFURNISHED && slot.EmployeeId is not null)
                    throw new ValidationException("Unfurnished slots cannot have an employee.");
            }

            foreach (var style in cellStyles)
            {
                if (style.DayOfMonth <= 0)
                    throw new ValidationException("Cell style day must be between 1 and 31.");

                if (style.EmployeeId <= 0)
                    throw new ValidationException("Cell style employee is required.");
            }
        }

        private static void AssignScheduleId(
            int scheduleId,
            IEnumerable<ScheduleEmployeeModel> employees,
            IEnumerable<ScheduleSlotModel> slots,
            IEnumerable<ScheduleCellStyleModel> cellStyles)
        {
            foreach (var employee in employees)
                employee.ScheduleId = scheduleId;

            foreach (var slot in slots)
                slot.ScheduleId = scheduleId;

            foreach (var style in cellStyles)
                style.ScheduleId = scheduleId;
        }

        private static void ValidateAssignedScheduleId(
            int scheduleId,
            IEnumerable<ScheduleEmployeeModel> employees,
            IEnumerable<ScheduleSlotModel> slots,
            IEnumerable<ScheduleCellStyleModel> cellStyles)
        {
            if (employees.Any(e => e.ScheduleId != scheduleId))
                throw new ValidationException("Schedule employees reference the wrong schedule.");

            if (slots.Any(s => s.ScheduleId != scheduleId))
                throw new ValidationException("Schedule slots reference the wrong schedule.");

            if (cellStyles.Any(cs => cs.ScheduleId != scheduleId))
                throw new ValidationException("Schedule cell styles reference the wrong schedule.");
        }
    }
}
