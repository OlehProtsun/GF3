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
            set => SetProperty(ref _selectedItem, value);
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
            EditCommand = new AsyncRelayCommand(() => _owner.EditSelectedAsync());
            DeleteCommand = new AsyncRelayCommand(() => _owner.DeleteSelectedAsync());
            OpenProfileCommand = new AsyncRelayCommand(() => _owner.OpenProfileAsync());
        }

        public void SetItems(IEnumerable<EmployeeModel> employees)
        {
            Items.Clear();
            foreach (var employee in employees)
                Items.Add(employee);
        }
    }
}
