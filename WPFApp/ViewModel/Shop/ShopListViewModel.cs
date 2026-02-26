/*
  Опис файлу: цей модуль містить реалізацію компонента ShopListViewModel у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Shops;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WPFApp.MVVM.Commands;
using WPFApp.MVVM.Core;

namespace WPFApp.ViewModel.Shop
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class ShopListViewModel : ViewModelBase` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ShopListViewModel : ViewModelBase
    {
        
        private readonly ShopViewModel _owner;

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<ShopDto> Items { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<ShopDto> Items { get; } = new();

        private ShopDto? _selectedItem;

        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ShopDto? SelectedItem` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ShopDto? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                    UpdateSelectionCommands();
            }
        }

        private string _searchText = string.Empty;

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public string SearchText` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand SearchCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand SearchCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand AddNewCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand AddNewCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand EditCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand EditCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand DeleteCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand DeleteCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand OpenProfileCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand OpenProfileCommand { get; }

        
        private readonly AsyncRelayCommand[] _selectionDependentCommands;

        /// <summary>
        /// Визначає публічний елемент `public ShopListViewModel(ShopViewModel owner)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ShopListViewModel(ShopViewModel owner)
        {
            
            _owner = owner;

            
            SearchCommand = new AsyncRelayCommand(() => _owner.SearchAsync());
            AddNewCommand = new AsyncRelayCommand(() => _owner.StartAddAsync());

            
            EditCommand = new AsyncRelayCommand(() => _owner.EditSelectedAsync(), () => HasSelection);
            DeleteCommand = new AsyncRelayCommand(() => _owner.DeleteSelectedAsync(), () => HasSelection);
            OpenProfileCommand = new AsyncRelayCommand(() => _owner.OpenProfileAsync(), () => HasSelection);

            
            _selectionDependentCommands = new[]
            {
                EditCommand,
                DeleteCommand,
                OpenProfileCommand
            };
        }

        private bool HasSelection => SelectedItem != null;

        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void SetItems(IEnumerable<ShopDto> shops)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void SetItems(IEnumerable<ShopDto> shops)
        {
            
            shops ??= Array.Empty<ShopDto>();

            
            var selectedId = SelectedItem?.Id ?? 0;

            
            if (shops is not IList<ShopDto> list)
                list = shops.ToList();

            
            if (IsSameList(Items, list))
                return;

            
            Items.Clear();
            foreach (var s in list)
                Items.Add(s);

            
            if (selectedId > 0)
                SelectedItem = Items.FirstOrDefault(x => x.Id == selectedId);
        }

        private static bool IsSameList(IList<ShopDto> current, IList<ShopDto> next)
        {
            
            if (current.Count != next.Count)
                return false;

            
            for (int i = 0; i < next.Count; i++)
            {
                var a = current[i];
                var b = next[i];

                if (a is null || b is null)
                    return false;

                
                if (a.Id != b.Id)
                    return false;

                
                if (!string.Equals(a.Name ?? string.Empty, b.Name ?? string.Empty, StringComparison.Ordinal))
                    return false;

                if (!string.Equals(a.Address ?? string.Empty, b.Address ?? string.Empty, StringComparison.Ordinal))
                    return false;

                if (!string.Equals(a.Description ?? string.Empty, b.Description ?? string.Empty, StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        private void UpdateSelectionCommands()
        {
            
            for (int i = 0; i < _selectionDependentCommands.Length; i++)
                _selectionDependentCommands[i].RaiseCanExecuteChanged();
        }
    }
}
