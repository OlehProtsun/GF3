using System.Collections.ObjectModel;
using DataAccessLayer.Models;
using WPFApp.Infrastructure;

namespace WPFApp.ViewModel.Employee
{
    public sealed class EmployeeListViewModel : ViewModelBase
    {
        private readonly EmployeeViewModel _owner;

        public ObservableCollection<EmployeeModel> Items { get; } = new();

        private EmployeeModel? _selectedItem;
        public EmployeeModel? SelectedItem
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

        public EmployeeListViewModel(EmployeeViewModel owner)
        {
            _owner = owner;

            SearchCommand = new AsyncRelayCommand(() => _owner.SearchAsync());
            AddNewCommand = new AsyncRelayCommand(() => _owner.StartAddAsync());
            EditCommand = new AsyncRelayCommand(() => _owner.EditSelectedAsync(), () => SelectedItem != null);
            DeleteCommand = new AsyncRelayCommand(() => _owner.DeleteSelectedAsync(), () => SelectedItem != null);
            OpenProfileCommand = new AsyncRelayCommand(() => _owner.OpenProfileAsync(), () => SelectedItem != null);
        }

        public void SetItems(IEnumerable<EmployeeModel> employees)
        {
            Items.Clear();
            foreach (var employee in employees)
                Items.Add(employee);
        }

        private void UpdateSelectionCommands()
        {
            EditCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
            OpenProfileCommand.RaiseCanExecuteChanged();
        }
    }
}
