using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using DataAccessLayer.Models;
using WPFApp.Infrastructure;

namespace WPFApp.ViewModel.Container
{
    public sealed class ContainerScheduleProfileViewModel : ViewModelBase
    {
        private readonly ContainerViewModel _owner;

        private int _scheduleId;
        public int ScheduleId
        {
            get => _scheduleId;
            set => SetProperty(ref _scheduleId, value);
        }

        private string _scheduleName = string.Empty;
        public string ScheduleName
        {
            get => _scheduleName;
            set => SetProperty(ref _scheduleName, value);
        }

        private string _scheduleMonthYear = string.Empty;
        public string ScheduleMonthYear
        {
            get => _scheduleMonthYear;
            set => SetProperty(ref _scheduleMonthYear, value);
        }

        private string _shopName = string.Empty;
        public string ShopName
        {
            get => _shopName;
            set => SetProperty(ref _shopName, value);
        }

        private string _note = string.Empty;
        public string Note
        {
            get => _note;
            set => SetProperty(ref _note, value);
        }

        private DataView _scheduleMatrix = new DataView();
        public DataView ScheduleMatrix
        {
            get => _scheduleMatrix;
            private set => SetProperty(ref _scheduleMatrix, value);
        }

        public ObservableCollection<ScheduleEmployeeModel> Employees { get; } = new();

        public AsyncRelayCommand BackCommand { get; }
        public AsyncRelayCommand CancelProfileCommand { get; }
        public AsyncRelayCommand EditCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }

        public event EventHandler? MatrixChanged;

        public ContainerScheduleProfileViewModel(ContainerViewModel owner)
        {
            _owner = owner;

            BackCommand = new AsyncRelayCommand(() => _owner.CancelScheduleAsync());
            CancelProfileCommand = new AsyncRelayCommand(() => _owner.CancelScheduleAsync());
            EditCommand = new AsyncRelayCommand(() => _owner.EditSelectedScheduleAsync());
            DeleteCommand = new AsyncRelayCommand(() => _owner.DeleteSelectedScheduleAsync());
        }

        public void SetProfile(ScheduleModel schedule, IList<ScheduleEmployeeModel> employees, IList<ScheduleSlotModel> slots)
        {
            ScheduleId = schedule.Id;
            ScheduleName = schedule.Name;
            ScheduleMonthYear = $"{schedule.Month:D2}.{schedule.Year}";
            ShopName = schedule.Shop?.Name ?? string.Empty;
            Note = schedule.Note ?? string.Empty;

            Employees.Clear();
            foreach (var emp in employees)
                Employees.Add(emp);

            var table = ContainerScheduleEditViewModel.BuildScheduleTable(
                schedule.Year,
                schedule.Month,
                slots,
                employees,
                out _);

            ScheduleMatrix = table.DefaultView;
            MatrixChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
