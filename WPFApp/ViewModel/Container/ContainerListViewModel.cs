using System.Collections.ObjectModel;
using DataAccessLayer.Models;
using WPFApp.Infrastructure;

namespace WPFApp.ViewModel.Container
{
    public sealed class ContainerListViewModel : ViewModelBase
    {
        private readonly ContainerViewModel _owner;

        public ObservableCollection<ContainerModel> Items { get; } = new();

        private ContainerModel? _selectedItem;
        public ContainerModel? SelectedItem
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

        public ContainerListViewModel(ContainerViewModel owner)
        {
            _owner = owner;

            SearchCommand = new AsyncRelayCommand(() => _owner.SearchAsync());
            AddNewCommand = new AsyncRelayCommand(() => _owner.StartAddAsync());
            EditCommand = new AsyncRelayCommand(() => _owner.EditSelectedAsync());
            DeleteCommand = new AsyncRelayCommand(() => _owner.DeleteSelectedAsync());
            OpenProfileCommand = new AsyncRelayCommand(() => _owner.OpenProfileAsync());
        }

        public void SetItems(IEnumerable<ContainerModel> containers)
        {
            Items.Clear();
            foreach (var container in containers)
                Items.Add(container);
        }
    }
}
