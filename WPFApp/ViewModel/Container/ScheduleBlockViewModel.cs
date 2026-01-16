using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DataAccessLayer.Models;
using WPFApp.Infrastructure;

namespace WPFApp.ViewModel.Container
{
    public sealed class ScheduleBlockViewModel : ObservableObject
    {
        public Guid Id { get; } = Guid.NewGuid();

        private ScheduleModel _model = new();
        public ScheduleModel Model
        {
            get => _model;
            set => SetProperty(ref _model, value);
        }

        public ObservableCollection<ScheduleEmployeeModel> Employees { get; } = new();
        public ObservableCollection<ScheduleSlotModel> Slots { get; } = new();
        public ObservableCollection<ScheduleCellStyleModel> CellStyles { get; } = new();

        private int _selectedAvailabilityGroupId;
        public int SelectedAvailabilityGroupId
        {
            get => _selectedAvailabilityGroupId;
            set => SetProperty(ref _selectedAvailabilityGroupId, value);
        }

        public Dictionary<string, string> ValidationErrors { get; } = new();
    }
}
