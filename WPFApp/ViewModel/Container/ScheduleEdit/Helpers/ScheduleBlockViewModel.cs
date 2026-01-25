using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DataAccessLayer.Models;
using WPFApp.Infrastructure;

namespace WPFApp.ViewModel.Container.ScheduleEdit.Helpers
{
    /// <summary>
    /// ScheduleBlockViewModel — ViewModel одного “блоку” розкладу.
    ///
    /// У твоєму UI, судячи з логіки:
    /// - можна відкривати кілька розкладів (Blocks / OpenedSchedules)
    /// - кожен блок має свою модель, працівників, слоти, стилі
    ///
    /// Тобто це “пакет даних” для одного таба/картки/вкладки.
    /// </summary>
    public sealed class ScheduleBlockViewModel : ObservableObject
    {
        /// <summary>
        /// Внутрішній GUID-ідентифікатор блока.
        ///
        /// Навіщо:
        /// - навіть якщо ScheduleId (Model.Id) ще 0 (новий schedule),
        ///   блок у UI все одно має мати стабільний ідентифікатор (наприклад для selection).
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary>
        /// Основна модель розкладу (дані, які потім зберігаються в БД).
        /// Приклад: Name, Year, Month, PeoplePerShift, Shift1/Shift2 тощо.
        ///
        /// Це “джерело правди” для властивостей Schedule* у головному VM.
        /// </summary>
        private ScheduleModel _model = new();
        public ScheduleModel Model
        {
            get => _model;
            set => SetProperty(ref _model, value);
        }

        /// <summary>
        /// Працівники, які беруть участь у цьому розкладі.
        ///
        /// Тип: ScheduleEmployeeModel (зв’язка schedule <-> employee).
        /// </summary>
        public ObservableCollection<ScheduleEmployeeModel> Employees { get; } = new();

        /// <summary>
        /// Слоти розкладу (коли/хто працює).
        ///
        /// Це те, що:
        /// - генерується генератором
        /// - редагується через матрицю (cell edit)
        /// </summary>
        public ObservableCollection<ScheduleSlotModel> Slots { get; } = new();

        /// <summary>
        /// Стилі клітинок (фон/текст).
        /// Зберігаються окремо, бо це UI-декорація, але ти її теж зберігаєш як дані.
        /// </summary>
        public ObservableCollection<ScheduleCellStyleModel> CellStyles { get; } = new();

        /// <summary>
        /// Вибрана група доступності (AvailabilityGroup) для цього блока.
        ///
        /// Чому в Block, а не тільки в головному VM:
        /// - у тебе може бути кілька блоків відкрито
        /// - кожен блок може мати свою групу
        /// </summary>
        private int _selectedAvailabilityGroupId;
        public int SelectedAvailabilityGroupId
        {
            get => _selectedAvailabilityGroupId;
            set => SetProperty(ref _selectedAvailabilityGroupId, value);
        }
    }
}
