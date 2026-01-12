using DataAccessLayer.Models;
using WPFApp.Infrastructure;

namespace WPFApp.ViewModel.Shop
{
    public sealed class ShopProfileViewModel : ViewModelBase
    {
        private readonly ShopViewModel _owner;

        private int _shopId;
        public int ShopId
        {
            get => _shopId;
            set => SetProperty(ref _shopId, value);
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _address = string.Empty;
        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public AsyncRelayCommand BackCommand { get; }
        public AsyncRelayCommand CancelProfileCommand { get; }
        public AsyncRelayCommand EditCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }

        public ShopProfileViewModel(ShopViewModel owner)
        {
            _owner = owner;

            BackCommand = new AsyncRelayCommand(() => _owner.CancelAsync());
            CancelProfileCommand = new AsyncRelayCommand(() => _owner.CancelAsync());
            EditCommand = new AsyncRelayCommand(() => _owner.EditSelectedAsync());
            DeleteCommand = new AsyncRelayCommand(() => _owner.DeleteSelectedAsync());
        }

        public void SetProfile(ShopModel model)
        {
            ShopId = model.Id;
            Name = model.Name;
            Address = string.IsNullOrWhiteSpace(model.Address) ? "—" : model.Address;
            Description = string.IsNullOrWhiteSpace(model.Description) ? "—" : model.Description;
        }
    }
}
