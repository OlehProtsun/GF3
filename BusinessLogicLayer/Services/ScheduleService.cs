using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Abstractions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services
{
    public class ScheduleService : GenericService<ScheduleModel>, IScheduleService
    {
        private readonly IScheduleRepository _scheduleRepo;
        private readonly IScheduleEmployeeRepository _employeeRepo;
        private readonly IScheduleSlotRepository _slotRepo;

        public ScheduleService(
            IScheduleRepository scheduleRepo,
            IScheduleEmployeeRepository employeeRepo,
            IScheduleSlotRepository slotRepo) : base(scheduleRepo)
        {
            _scheduleRepo = scheduleRepo;
            _employeeRepo = employeeRepo;
            _slotRepo = slotRepo;
        }

        public Task<List<ScheduleModel>> GetByContainerAsync(int containerId, string? value = null, CancellationToken ct = default)
            => _scheduleRepo.GetByContainerAsync(containerId, value, ct);

        public Task<List<ScheduleModel>> GetByValueAsync(string value, CancellationToken ct = default)
            => _scheduleRepo.GetByValueAsync(value, ct);

        public async Task SaveWithDetailsAsync(
            ScheduleModel schedule,
            IEnumerable<ScheduleEmployeeModel> employees,
            IEnumerable<ScheduleSlotModel> slots,
            CancellationToken ct = default)
        {
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

            return schedule;
        }
    }
}
