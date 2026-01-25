using DataAccessLayer.Models;
using System.Collections.ObjectModel;
using WPFApp.Infrastructure;

namespace WPFApp.ViewModel.Availability
{
    public sealed class AvailabilityListViewModel : ViewModelBase
    {
        private readonly AvailabilityViewModel _owner;

        public ObservableCollection<AvailabilityGroupModel> Items { get; } = new();

        private AvailabilityGroupModel? _selectedItem;
        public AvailabilityGroupModel? SelectedItem
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

        public AvailabilityListViewModel(AvailabilityViewModel owner)
        {
            _owner = owner;

            SearchCommand = new AsyncRelayCommand(() => _owner.SearchAsync());
            AddNewCommand = new AsyncRelayCommand(() => _owner.StartAddAsync());
            EditCommand = new AsyncRelayCommand(() => _owner.EditSelectedAsync(), () => SelectedItem != null);
            DeleteCommand = new AsyncRelayCommand(() => _owner.DeleteSelectedAsync(), () => SelectedItem != null);
            OpenProfileCommand = new AsyncRelayCommand(() => _owner.OpenProfileAsync(), () => SelectedItem != null);
        }

        public void SetItems(IEnumerable<AvailabilityGroupModel> items)
        {
            Items.Clear();
            foreach (var item in items)
                Items.Add(item);
        }

        private void UpdateSelectionCommands()
        {
            EditCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
            OpenProfileCommand.RaiseCanExecuteChanged();
        }
    }
}
