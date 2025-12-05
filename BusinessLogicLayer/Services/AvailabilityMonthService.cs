using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services
{
    public class AvailabilityMonthService : GenericService<AvailabilityMonthModel>, IAvailabilityMonthService
    {
        private readonly IAvailabilityMonthRepository _monthRepo;
        private readonly IAvailabilityDayRepository _dayRepo;

        public AvailabilityMonthService(
            IAvailabilityMonthRepository monthRepo,
            IAvailabilityDayRepository dayRepo)
            : base(monthRepo)
        {
            _monthRepo = monthRepo;
            _dayRepo = dayRepo;
        }

        public async Task<List<AvailabilityMonthModel>> GetByValueAsync(
            string value,
            CancellationToken ct = default)
        {
            return await _monthRepo.GetByValueAsync(value, ct);
        }

        /// <summary>
        /// Зберігає місяць + повністю замінює список днів для нього.
        /// </summary>
        public async Task SaveWithDaysAsync(
            AvailabilityMonthModel month,
            IList<AvailabilityDayModel> days,
            CancellationToken ct = default)
        {
            // 1) Зберігаємо/оновлюємо сам місяць
            if (month.Id == 0)
            {
                // створення
                month = await _monthRepo.AddAsync(month, ct);
            }
            else
            {
                // оновлення
                await _monthRepo.UpdateAsync(month, ct);
            }

            var monthId = month.Id;

            // 2) Видаляємо існуючі дні для цього місяця
            var allDays = await _dayRepo.GetAllAsync(ct);
            var existingForMonth = allDays
                .Where(d => d.AvailabilityMonthId == monthId)
                .ToList();

            foreach (var d in existingForMonth)
            {
                await _dayRepo.DeleteAsync(d.Id, ct);
            }

            // 3) Додаємо нові дні
            foreach (var d in days)
            {
                d.Id = 0;                    // на всякий випадок, щоб точно були Insert
                d.AvailabilityMonthId = monthId;

                await _dayRepo.AddAsync(d, ct);
            }
        }

        public async Task<List<AvailabilityDayModel>> GetDaysForMonthAsync(
            int availabilityMonthId,
            CancellationToken ct = default)
        {
            var all = await _dayRepo.GetAllAsync(ct); // можна оптимізувати окремим методом у репозиторії
            return all
                .Where(d => d.AvailabilityMonthId == availabilityMonthId)
                .OrderBy(d => d.DayOfMonth)
                .ToList();
        }
    }
}
