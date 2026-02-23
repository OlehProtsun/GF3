using BusinessLogicLayer.Common;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Abstractions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Globalization;
using System.Text.RegularExpressions;


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
            EnsureGenerated(schedule, slots);

            var employeeList = employees?.ToList() ?? new List<ScheduleEmployeeModel>();
            var slotList = slots?.ToList() ?? new List<ScheduleSlotModel>();
            var cellStyleList = cellStyles?.ToList() ?? new List<ScheduleCellStyleModel>();

            if (schedule.Id == 0)
                schedule = await _scheduleRepo.AddAsync(schedule, ct).ConfigureAwait(false);
            else
                await _scheduleRepo.UpdateAsync(schedule, ct).ConfigureAwait(false);

            var scheduleId = schedule.Id;

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

            var normalizedStyles = (cellStyles ?? Enumerable.Empty<ScheduleCellStyleModel>())
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

            schedule.Shift1Time = NormalizeShift(schedule.Shift1Time, "Shift1");
            schedule.Shift2Time = NormalizeShift(schedule.Shift2Time, "Shift2");
        }

        private static string NormalizeShift(string? value, string label)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ValidationException($"{label} is required.");

            var s = value.Trim()
                .Replace('–', '-')
                .Replace('—', '-')
                .Replace('−', '-');

            var parts = s.Split('-', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                throw new ValidationException($"{label} format must be HH:mm - HH:mm.");

            if (!TimeSpan.TryParseExact(parts[0], new[] { @"h\:mm", @"hh\:mm" }, CultureInfo.InvariantCulture, out var from) ||
                !TimeSpan.TryParseExact(parts[1], new[] { @"h\:mm", @"hh\:mm" }, CultureInfo.InvariantCulture, out var to))
                throw new ValidationException($"{label} format must be HH:mm - HH:mm.");

            if (to <= from)
                throw new ValidationException($"{label} end must be later than start.");

            // ВАЖЛИВО: формат БД (з пробілами)
            return $"{from:hh\\:mm} - {to:hh\\:mm}";
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
    }
}
