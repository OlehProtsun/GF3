/*
  Опис файлу: цей модуль містить реалізацію компонента ShopProfileViewModel у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Shops;
using WPFApp.MVVM.Commands;
using WPFApp.MVVM.Core;
using WPFApp.ViewModel.Shop.Helpers;

namespace WPFApp.ViewModel.Shop
{
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class ShopProfileViewModel : ViewModelBase` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ShopProfileViewModel : ViewModelBase
    {
        private readonly ShopViewModel _owner;

        private int _shopId;
        /// <summary>
        /// Визначає публічний елемент `public int ShopId` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int ShopId
        {
            get => _shopId;
            set
            {
                
                if (SetProperty(ref _shopId, value))
                    UpdateCommands();
            }
        }

        private string _name = string.Empty;
        /// <summary>
        /// Визначає публічний елемент `public string Name` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _address = string.Empty;
        /// <summary>
        /// Визначає публічний елемент `public string Address` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        private string _description = string.Empty;
        /// <summary>
        /// Визначає публічний елемент `public string Description` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand BackCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand BackCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand CancelProfileCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand CancelProfileCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand EditCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand EditCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand DeleteCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand DeleteCommand { get; }

        private readonly AsyncRelayCommand[] _idDependentCommands;

        /// <summary>
        /// Визначає публічний елемент `public ShopProfileViewModel(ShopViewModel owner)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ShopProfileViewModel(ShopViewModel owner)
        {
            _owner = owner;

            
            BackCommand = new AsyncRelayCommand(() => _owner.CancelAsync());
            CancelProfileCommand = BackCommand;

            
            EditCommand = new AsyncRelayCommand(() => _owner.EditSelectedAsync(), () => ShopId > 0);
            DeleteCommand = new AsyncRelayCommand(() => _owner.DeleteSelectedAsync(), () => ShopId > 0);

            _idDependentCommands = new[] { EditCommand, DeleteCommand };
        }

        /// <summary>
        /// Визначає публічний елемент `public void SetProfile(ShopDto model)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void SetProfile(ShopDto model)
        {
            
            _owner.ListVm.SelectedItem = model;

            
            ShopId = model.Id;
            Name = model.Name ?? string.Empty;

            
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
