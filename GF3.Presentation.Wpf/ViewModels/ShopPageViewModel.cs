using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;

namespace GF3.Presentation.Wpf.ViewModels
{
    public class ShopPageViewModel : ValidatableViewModelBase
    {
        private readonly IShopService _shopService;
        private ShopModel? _selectedShop;
        private string _searchText = string.Empty;
        private EntityViewMode _mode;

        public ShopPageViewModel(IShopService shopService)
        {
            _shopService = shopService ?? throw new ArgumentNullException(nameof(shopService));

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            SearchCommand = new AsyncRelayCommand(SearchAsync);
            AddCommand = new RelayCommand(BeginAdd);
            EditCommand = new RelayCommand(BeginEdit, () => SelectedShop is not null);
            DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => SelectedShop is not null);
            SaveCommand = new AsyncRelayCommand(SaveAsync, () => Mode == EntityViewMode.Edit);
            CancelCommand = new RelayCommand(CancelEdit);

            _ = LoadAsync();
        }

        public ObservableCollection<ShopModel> Shops { get; } = new();

        public ShopModel? SelectedShop
        {
            get => _selectedShop;
            set
            {
                _selectedShop = value;
                RaisePropertyChanged();
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                RaisePropertyChanged();
            }
        }

        public EntityViewMode Mode
        {
            get => _mode;
            private set
            {
                _mode = value;
                RaisePropertyChanged();
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public AsyncRelayCommand LoadCommand { get; }
        public AsyncRelayCommand SearchCommand { get; }
        public RelayCommand AddCommand { get; }
        public RelayCommand EditCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }
        public AsyncRelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }

        private async Task LoadAsync()
        {
            var shops = await _shopService.GetAllAsync();
            Shops.Clear();
            foreach (var shop in shops)
            {
                Shops.Add(shop);
            }
        }

        private async Task SearchAsync()
        {
            var shops = await _shopService.GetAllAsync();
            var filtered = shops
                .Where(x => string.IsNullOrWhiteSpace(SearchText)
                    || x.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Shops.Clear();
            foreach (var shop in filtered)
            {
                Shops.Add(shop);
            }
        }

        private void BeginAdd()
        {
            SelectedShop = new ShopModel();
            Mode = EntityViewMode.Edit;
        }

        private void BeginEdit()
        {
            if (SelectedShop is null)
            {
                return;
            }

            Mode = EntityViewMode.Edit;
        }

        private async Task DeleteAsync()
        {
            if (SelectedShop is null)
            {
                return;
            }

            await _shopService.DeleteAsync(SelectedShop.Id);
            await LoadAsync();
        }

        private async Task SaveAsync()
        {
            if (SelectedShop is null)
            {
                return;
            }

            if (!ValidateShop(SelectedShop))
            {
                return;
            }

            if (SelectedShop.Id == 0)
            {
                await _shopService.AddAsync(SelectedShop);
            }
            else
            {
                await _shopService.UpdateAsync(SelectedShop);
            }

            Mode = EntityViewMode.List;
            await LoadAsync();
        }

        private void CancelEdit()
        {
            Mode = EntityViewMode.List;
            ClearErrors(nameof(SelectedShop));
        }

        private bool ValidateShop(ShopModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                SetErrors(nameof(SelectedShop), "Shop name is required.");
                return false;
            }

            ClearErrors(nameof(SelectedShop));
            return true;
        }
    }
}
