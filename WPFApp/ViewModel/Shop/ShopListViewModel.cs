using System.Collections.ObjectModel;
using DataAccessLayer.Models;
using WPFApp.Infrastructure;

namespace WPFApp.ViewModel.Shop
{
    public sealed class ShopListViewModel : ViewModelBase
    {
        private readonly ShopViewModel _owner;

        public ObservableCollection<ShopModel> Items { get; } = new();

        private ShopModel? _selectedItem;
        public ShopModel? SelectedItem
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

        public ShopListViewModel(ShopViewModel owner)
        {
            _owner = owner;

            SearchCommand = new AsyncRelayCommand(() => _owner.SearchAsync());
            AddNewCommand = new AsyncRelayCommand(() => _owner.StartAddAsync());
            EditCommand = new AsyncRelayCommand(() => _owner.EditSelectedAsync());
            DeleteCommand = new AsyncRelayCommand(() => _owner.DeleteSelectedAsync());
            OpenProfileCommand = new AsyncRelayCommand(() => _owner.OpenProfileAsync());
        }

        public void SetItems(IEnumerable<ShopModel> shops)
        {
            Items.Clear();
            foreach (var shop in shops)
                Items.Add(shop);
        }
    }
}
