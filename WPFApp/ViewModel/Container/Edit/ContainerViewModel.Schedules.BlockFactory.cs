using BusinessLogicLayer.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using WPFApp.ViewModel.Container.ScheduleEdit.Helpers;

namespace WPFApp.ViewModel.Container.Edit
{
    /// <summary>
    /// ContainerViewModel.Schedules.BlockFactory — частина (partial) ContainerViewModel,
    /// яка відповідає ТІЛЬКИ за створення/копіювання schedule-блоків (табів) для ScheduleEditVm.
    ///
    /// Це “фабрика”:
    /// - CreateDefaultBlock(containerId) створює новий блок для Add mode
    /// - CreateBlockFromSchedule(...) створює блок з існуючої моделі (Edit mode / MultiOpen)
    ///
    /// Також тут логічно тримати допоміжні перевірки:
    /// - HasGeneratedContent(...) — чи є в schedule/блоці згенеровані слоти
    /// - GetDefaultAvailabilityGroupId(year, month) — дефолтна група для нового блоку
    ///
    /// Чому це винесено:
    /// - це “schedule domain helpers”, а не UI і не навігація
    /// - головний ContainerViewModel.cs має лишитися максимально коротким
    /// </summary>
    public sealed partial class ContainerViewModel
    {
        /// <summary>
        /// Створити дефолтний блок schedule для нового розкладу (Add mode).
        ///
        /// containerId:
        /// - ідентифікатор контейнера, до якого належить schedule
        ///
        /// Що відбувається:
        /// 1) Створюємо новий ScheduleModel з дефолтними значеннями
        /// 2) Створюємо ScheduleBlockViewModel, в який кладемо модель
        /// 3) Ставимо SelectedAvailabilityGroupId:
        ///    - або дефолтну групу по month/year (якщо є)
        ///    - або 0 (якщо нема)
        /// </summary>
        private ScheduleBlockViewModel CreateDefaultBlock(int containerId)
        {
            var model = new ScheduleModel
            {
                ContainerId = containerId,
                Year = DateTime.Today.Year,
                Month = DateTime.Today.Month,

                // Мінімальні “безпечні” дефолти:
                PeoplePerShift = 1,

                // Ліміти (як у твоєму поточному коді):
                MaxHoursPerEmpMonth = 1,
                MaxConsecutiveDays = 1,
                MaxConsecutiveFull = 1,
                MaxFullPerMonth = 1,

                // Shift-и пусті — користувач має ввести
                Shift1Time = string.Empty,
                Shift2Time = string.Empty,

                Note = string.Empty
            };

            var block = new ScheduleBlockViewModel
            {
                Model = model,
                SelectedAvailabilityGroupId = 0,
            };

            return block;
        }

        /// <summary>
        /// Створити блок schedule з існуючої моделі.
        ///
        /// Навіщо “copy”:
        /// - щоб редагування у VM не змінювало випадково той самий object instance,
        ///   який може ще десь використовуватись (наприклад у списку)
        /// - ми робимо “копію полів” ScheduleModel
        ///
        /// employees/slots/cellStyles:
        /// - наповнюють блок даними для матриці
        /// </summary>
        private ScheduleBlockViewModel CreateBlockFromSchedule(
            ScheduleModel model,
            IList<ScheduleEmployeeModel> employees,
            IList<ScheduleSlotModel> slots,
            IList<ScheduleCellStyleModel>? cellStyles = null)
        {
            // Копія “плоских” полів ScheduleModel
            var copy = new ScheduleModel
            {
                Id = model.Id,
                ContainerId = model.ContainerId,
                ShopId = model.ShopId,
                Name = model.Name,
                Year = model.Year,
                Month = model.Month,
                PeoplePerShift = model.PeoplePerShift,
                Shift1Time = model.Shift1Time,
                Shift2Time = model.Shift2Time,
                MaxHoursPerEmpMonth = model.MaxHoursPerEmpMonth,
                MaxConsecutiveDays = model.MaxConsecutiveDays,
                MaxConsecutiveFull = model.MaxConsecutiveFull,
                MaxFullPerMonth = model.MaxFullPerMonth,
                Note = model.Note,
                Shop = model.Shop,

                // ВАЖЛИВО: зберігаємо AvailabilityGroupId, якщо він був.
                // Це впливає на Edit/Generate/Preview.
                AvailabilityGroupId = model.AvailabilityGroupId
            };

            var block = new ScheduleBlockViewModel
            {
                Model = copy,

                // Якщо schedule вже був згенерований — беремо groupId з нього,
                // інакше — беремо дефолтну групу по month/year.
                SelectedAvailabilityGroupId =
                    copy.AvailabilityGroupId
                    ?? GetDefaultAvailabilityGroupId(copy.Year, copy.Month)
            };

            // Наповнюємо працівників
            foreach (var emp in employees)
                block.Employees.Add(emp);

            // Наповнюємо слоти
            foreach (var slot in slots)
                block.Slots.Add(slot);

            // Наповнюємо стилі клітинок (якщо є)
            if (cellStyles != null)
            {
                foreach (var style in cellStyles)
                    block.CellStyles.Add(style);
            }

            return block;
        }

        /// <summary>
        /// Чи є у schedule “згенерований контент”.
        ///
        /// Для ScheduleModel (detailed) ми орієнтуємось на:
        /// - AvailabilityGroupId встановлений
        /// - Slots існують і не порожні
        /// </summary>
        private static bool HasGeneratedContent(ScheduleModel schedule)
        {
            return schedule.AvailabilityGroupId is not null
                && schedule.AvailabilityGroupId > 0
                && schedule.Slots != null
                && schedule.Slots.Count > 0;
        }

        /// <summary>
        /// Чи є у блока згенерований контент.
        ///
        /// Для блока достатньо:
        /// - SelectedAvailabilityGroupId > 0
        /// - Slots.Count > 0
        /// </summary>
        private static bool HasGeneratedContent(ScheduleBlockViewModel block)
        {
            return block.SelectedAvailabilityGroupId > 0
                && block.Slots.Count > 0;
        }

        /// <summary>
        /// Отримати “дефолтну” availability group для заданого year/month.
        ///
        /// Логіка:
        /// - беремо _allAvailabilityGroups (кеш з Lookups partial)
        /// - шукаємо першу групу з відповідним month/year
        /// - повертаємо її Id або 0 (якщо нічого нема)
        ///
        /// Важливий нюанс:
        /// - цей метод залежить від того, що lookups вже були завантажені.
        /// - якщо викликати його до LoadLookupsAsync — поверне 0.
        /// </summary>
        private int GetDefaultAvailabilityGroupId(int year, int month)
        {
            return _allAvailabilityGroups
                .Where(g => g.Year == year && g.Month == month)
                .Select(g => g.Id)
                .FirstOrDefault();
        }
    }
}
