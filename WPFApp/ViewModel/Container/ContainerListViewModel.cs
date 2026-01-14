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

        public ContainerListViewModel(ContainerViewModel owner)
        {
            _owner = owner;

            SearchCommand = new AsyncRelayCommand(() => _owner.SearchAsync());
            AddNewCommand = new AsyncRelayCommand(() => _owner.StartAddAsync());
            EditCommand = new AsyncRelayCommand(() => _owner.EditSelectedAsync(), () => SelectedItem != null);
            DeleteCommand = new AsyncRelayCommand(() => _owner.DeleteSelectedAsync(), () => SelectedItem != null);
            OpenProfileCommand = new AsyncRelayCommand(() => _owner.OpenProfileAsync(), () => SelectedItem != null);
        }

        public void SetItems(IEnumerable<ContainerModel> containers)
        {
            Items.Clear();
            foreach (var container in containers)
                Items.Add(container);
        }

        private void UpdateSelectionCommands()
        {
            EditCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
            OpenProfileCommand.RaiseCanExecuteChanged();
        }
    }
}
