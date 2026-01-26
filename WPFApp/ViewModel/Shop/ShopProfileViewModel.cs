using DataAccessLayer.Models;
using WPFApp.Infrastructure;
using WPFApp.ViewModel.Shop.Helpers;

namespace WPFApp.ViewModel.Shop
{
    /// <summary>
    /// ShopProfileViewModel — read-only профіль магазину.
    ///
    /// Оптимізації:
    /// - BackCommand і CancelProfileCommand — це одна команда (без дублювання).
    /// - Edit/Delete доступні лише якщо ShopId > 0.
    /// - SetProfile синхронізує owner.ListVm.SelectedItem = model,
    ///   щоб owner.EditSelectedAsync/DeleteSelectedAsync працювали стабільно з профілю.
    /// - Address/Description показуються як "—" якщо порожні (через helper).
    /// </summary>
    public sealed class ShopProfileViewModel : ViewModelBase
    {
        private readonly ShopViewModel _owner;

        private int _shopId;
        public int ShopId
        {
            get => _shopId;
            set
            {
                // При зміні Id оновлюємо canExecute для залежних команд.
                if (SetProperty(ref _shopId, value))
                    UpdateCommands();
            }
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

        private readonly AsyncRelayCommand[] _idDependentCommands;

        public ShopProfileViewModel(ShopViewModel owner)
        {
            _owner = owner;

            // Back/Cancel — одна логіка.
            BackCommand = new AsyncRelayCommand(() => _owner.CancelAsync());
            CancelProfileCommand = BackCommand;

            // Edit/Delete активні лише коли профіль завантажено.
            EditCommand = new AsyncRelayCommand(() => _owner.EditSelectedAsync(), () => ShopId > 0);
            DeleteCommand = new AsyncRelayCommand(() => _owner.DeleteSelectedAsync(), () => ShopId > 0);

            _idDependentCommands = new[] { EditCommand, DeleteCommand };
        }

        public void SetProfile(ShopModel model)
        {
            // 1) Синхронізуємо selection у owner, щоб owner-методи працювали по “правильному” об’єкту.
            _owner.ListVm.SelectedItem = model;

            // 2) Заповнюємо поля.
            ShopId = model.Id;
            Name = model.Name ?? string.Empty;

            // 3) Address/Description: "—" якщо пусто.
            Address = ShopDisplayHelper.TextOrDash(model.Address);
            Description = ShopDisplayHelper.TextOrDash(model.Description);
        }

        private void UpdateCommands()
        {
            for (int i = 0; i < _idDependentCommands.Length; i++)
                _idDependentCommands[i].RaiseCanExecuteChanged();
        }
    }
}
