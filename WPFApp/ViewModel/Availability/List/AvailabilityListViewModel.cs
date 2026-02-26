/*
  Опис файлу: цей модуль містить реалізацію компонента AvailabilityListViewModel у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WPFApp.MVVM.Commands;
using WPFApp.MVVM.Core;
using WPFApp.ViewModel.Availability.Main;

namespace WPFApp.ViewModel.Availability.List
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class AvailabilityListViewModel : ViewModelBase` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class AvailabilityListViewModel : ViewModelBase
    {
        
        
        

        
        
        
        
        
        
        
        
        private readonly AvailabilityViewModel _owner;

        
        
        

        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<AvailabilityGroupModel> Items { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<AvailabilityGroupModel> Items { get; } = new();

        
        
        
        
        private AvailabilityGroupModel? _selectedItem;

        /// <summary>
        /// Визначає публічний елемент `public AvailabilityGroupModel? SelectedItem` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AvailabilityGroupModel? SelectedItem
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
        /// Визначає публічний елемент `public AvailabilityListViewModel(AvailabilityViewModel owner)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AvailabilityListViewModel(AvailabilityViewModel owner)
        {
            
            _owner = owner;

            
            
            SearchCommand = new AsyncRelayCommand(() => _owner.SearchAsync());

            
            AddNewCommand = new AsyncRelayCommand(() => _owner.StartAddAsync());

            
            
            EditCommand = new AsyncRelayCommand(
                execute: () => _owner.EditSelectedAsync(),
                canExecute: () => HasSelection);

            DeleteCommand = new AsyncRelayCommand(
                execute: () => _owner.DeleteSelectedAsync(),
                canExecute: () => HasSelection);

            OpenProfileCommand = new AsyncRelayCommand(
                execute: () => _owner.OpenProfileAsync(),
                canExecute: () => HasSelection);

            
            _selectionDependentCommands = new[]
            {
                EditCommand,
                DeleteCommand,
                OpenProfileCommand
            };
        }

        
        
        
        
        
        private bool HasSelection => SelectedItem != null;

        
        
        

        
        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void SetItems(IEnumerable<AvailabilityGroupModel> items)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void SetItems(IEnumerable<AvailabilityGroupModel> items)
        {
            
            items ??= Array.Empty<AvailabilityGroupModel>();

            
            
            var selectedId = SelectedItem?.Id ?? 0;

            
            
            
            if (items is not IList<AvailabilityGroupModel> list)
                list = items.ToList();

            
            
            if (IsSameList(Items, list))
            {
                
                return;
            }

            
            Items.Clear();

            
            foreach (var it in list)
                Items.Add(it);

            
            
            if (selectedId > 0)
            {
                
                var restored = Items.FirstOrDefault(x => x.Id == selectedId);

                
                
                SelectedItem = restored;
            }
            else
            {
                
                
            }
        }

        
        
        
        
        
        
        
        
        
        
        
        
        private static bool IsSameList(IList<AvailabilityGroupModel> current, IList<AvailabilityGroupModel> next)
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

                
                
                if (a.Month != b.Month)
                    return false;

                if (a.Year != b.Year)
                    return false;
            }

            
            return true;
        }

        
        
        

        
        
        
        
        
        
        
        
        private void UpdateSelectionCommands()
        {
            
            for (int i = 0; i < _selectionDependentCommands.Length; i++)
            {
                
                _selectionDependentCommands[i].RaiseCanExecuteChanged();
            }
        }
    }
}
