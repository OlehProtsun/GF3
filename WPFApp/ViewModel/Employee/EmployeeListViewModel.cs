/*
  Опис файлу: цей модуль містить реалізацію компонента EmployeeListViewModel у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Employees;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WPFApp.MVVM.Commands;
using WPFApp.MVVM.Core;

namespace WPFApp.ViewModel.Employee
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class EmployeeListViewModel : ViewModelBase` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class EmployeeListViewModel : ViewModelBase
    {
        
        private readonly EmployeeViewModel _owner;

        
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<EmployeeDto> Items { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<EmployeeDto> Items { get; } = new();

        
        private EmployeeDto? _selectedItem;

        /// <summary>
        /// Визначає публічний елемент `public EmployeeDto? SelectedItem` та контракт його використання у шарі WPFApp.
        /// </summary>
        public EmployeeDto? SelectedItem
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
        /// Визначає публічний елемент `public EmployeeListViewModel(EmployeeViewModel owner)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public EmployeeListViewModel(EmployeeViewModel owner)
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
        /// Визначає публічний елемент `public void SetItems(IEnumerable<EmployeeDto> employees)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void SetItems(IEnumerable<EmployeeDto> employees)
        {
            
            employees ??= Array.Empty<EmployeeDto>();

            
            var selectedId = SelectedItem?.Id ?? 0;

            
            if (employees is not IList<EmployeeDto> list)
                list = employees.ToList();

            
            if (IsSameList(Items, list))
                return;

            
            Items.Clear();
            foreach (var e in list)
                Items.Add(e);

            
            if (selectedId > 0)
                SelectedItem = Items.FirstOrDefault(x => x.Id == selectedId);
        }

        
        
        
        
        private static bool IsSameList(IList<EmployeeDto> current, IList<EmployeeDto> next)
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

                
                if (!string.Equals(a.FirstName ?? string.Empty, b.FirstName ?? string.Empty, StringComparison.Ordinal))
                    return false;

                if (!string.Equals(a.LastName ?? string.Empty, b.LastName ?? string.Empty, StringComparison.Ordinal))
                    return false;

                if (!string.Equals(a.Email ?? string.Empty, b.Email ?? string.Empty, StringComparison.Ordinal))
                    return false;

                if (!string.Equals(a.Phone ?? string.Empty, b.Phone ?? string.Empty, StringComparison.Ordinal))
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
