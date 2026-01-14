using System.Collections.ObjectModel;
using DataAccessLayer.Models;
using WPFApp.Infrastructure;

namespace WPFApp.ViewModel.Container
{
    public sealed class ContainerScheduleListViewModel : ViewModelBase
    {
        private readonly ContainerViewModel _owner;

        public ObservableCollection<ScheduleModel> Items { get; } = new();

        private ScheduleModel? _selectedItem;
        public ScheduleModel? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                    UpdateSelectionCommands();
            }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public AsyncRelayCommand SearchCommand { get; }
        public AsyncRelayCommand AddNewCommand { get; }
        public AsyncRelayCommand EditCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }
        public AsyncRelayCommand OpenProfileCommand { get; }

        public ContainerScheduleListViewModel(ContainerViewModel owner)
        {
            _owner = owner;

            SearchCommand = new AsyncRelayCommand(() => _owner.SearchScheduleAsync());
            AddNewCommand = new AsyncRelayCommand(() => _owner.StartScheduleAddAsync());
            EditCommand = new AsyncRelayCommand(() => _owner.EditSelectedScheduleAsync(), () => SelectedItem != null);
            DeleteCommand = new AsyncRelayCommand(() => _owner.DeleteSelectedScheduleAsync(), () => SelectedItem != null);
            OpenProfileCommand = new AsyncRelayCommand(() => _owner.OpenScheduleProfileAsync(), () => SelectedItem != null);
        }

        public void SetItems(IEnumerable<ScheduleModel> schedules)
        {
            Items.Clear();
            foreach (var schedule in schedules)
                Items.Add(schedule);
        }

        private void UpdateSelectionCommands()
        {
            EditCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
            OpenProfileCommand.RaiseCanExecuteChanged();
        }
    }
}
