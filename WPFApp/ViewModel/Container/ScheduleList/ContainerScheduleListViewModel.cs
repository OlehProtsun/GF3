/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerScheduleListViewModel у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Models;                 
using System;                                 
using System.Collections.ObjectModel;         
using System.Linq;                            
using System.Windows.Input;                   
using WPFApp.MVVM.Commands;
using WPFApp.MVVM.Core;
using WPFApp.ViewModel.Container.Edit;
using WPFApp.ViewModel.Dialogs;
using RelayCommand = WPFApp.MVVM.Commands.RelayCommand;               

namespace WPFApp.ViewModel.Container.ScheduleList
{
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class ScheduleRowVm : ViewModelBase` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ScheduleRowVm : ViewModelBase
    {
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ScheduleModel Model { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ScheduleModel Model { get; }

        /// <summary>
        /// Визначає публічний елемент `public ScheduleRowVm(ScheduleModel model)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ScheduleRowVm(ScheduleModel model)
        {
            Model = model;
        }

        
        /// <summary>
        /// Визначає публічний елемент `public string Name => Model.Name;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Name => Model.Name;
        /// <summary>
        /// Визначає публічний елемент `public int Year => Model.Year;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int Year => Model.Year;
        /// <summary>
        /// Визначає публічний елемент `public int Month => Model.Month;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int Month => Model.Month;
        /// <summary>
        /// Визначає публічний елемент `public ShopModel? Shop => Model.Shop;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ShopModel? Shop => Model.Shop;

        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public event Action? CheckedChanged;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public event Action? CheckedChanged;

        
        
        
        
        
        
        
        
        
        
        private bool _isChecked;
        /// <summary>
        /// Визначає публічний елемент `public bool IsChecked` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                
                
                
                
                if (SetProperty(ref _isChecked, value))
                {
                    
                    
                    
                    CheckedChanged?.Invoke();
                }
            }
        }
    }


    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class ContainerScheduleListViewModel : ViewModelBase` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ContainerScheduleListViewModel : ViewModelBase
    {
        
        
        
        
        private readonly ContainerViewModel _owner;

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<ScheduleRowVm> Items { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<ScheduleRowVm> Items { get; } = new();

        
        
        
        
        private ScheduleRowVm? _selectedItem;
        /// <summary>
        /// Визначає публічний елемент `public ScheduleRowVm? SelectedItem` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ScheduleRowVm? SelectedItem
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

        
        
        
        
        
        
        
        
        
        
        
        private bool _isMultiOpenEnabled;
        /// <summary>
        /// Визначає публічний елемент `public bool IsMultiOpenEnabled` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool IsMultiOpenEnabled
        {
            get => _isMultiOpenEnabled;
            set
            {
                if (_isMultiOpenEnabled == value)
                    return;

                _isMultiOpenEnabled = value;
                OnPropertyChanged(nameof(IsMultiOpenEnabled));

                
                
                
                
                
                if (!_isMultiOpenEnabled)
                {
                    foreach (var it in Items)
                        it.IsChecked = false;
                }

                
                
                
                MultiOpenCommand.RaiseCanExecuteChanged();
            }
        }

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ICommand ToggleMultiOpenCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ICommand ToggleMultiOpenCommand { get; }

        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand MultiOpenCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand MultiOpenCommand { get; }

        
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

        /// <summary>
        /// Визначає публічний елемент `public ContainerScheduleListViewModel(ContainerViewModel owner)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ContainerScheduleListViewModel(ContainerViewModel owner)
        {
            _owner = owner;

            
            SearchCommand = new AsyncRelayCommand(() => _owner.SearchScheduleAsync());
            AddNewCommand = new AsyncRelayCommand(() => _owner.StartScheduleAddAsync());

            
            EditCommand = new AsyncRelayCommand(() => _owner.EditSelectedScheduleAsync(), () => SelectedItem != null);
            DeleteCommand = new AsyncRelayCommand(() => _owner.DeleteSelectedScheduleAsync(), () => SelectedItem != null);
            OpenProfileCommand = new AsyncRelayCommand(() => _owner.OpenScheduleProfileAsync(), () => SelectedItem != null);

            
            ToggleMultiOpenCommand = new RelayCommand(() =>
            {
                IsMultiOpenEnabled = !IsMultiOpenEnabled;
            });

            
            
            
            
            
            
            
            
            
            MultiOpenCommand = new AsyncRelayCommand(
                async () =>
                {
                    
                    var selectedModels = Items
                        .Where(x => x.IsChecked)
                        .Select(x => x.Model)
                        .ToList();

                    
                    if (selectedModels.Count == 0)
                        return;

                    
                    
                    var names = selectedModels
                        .Select((model, index) => FormatScheduleName(model, index + 1))
                        .Select(name => $"'{name}'")
                        .ToList();

                    
                    var noun = selectedModels.Count == 1 ? "schedule" : "schedules";
                    var message = $"Open {selectedModels.Count} {noun}: {string.Join(", ", names)}";

                    
                    if (!_owner.Confirm(message))
                        return;

                    
                    await _owner.MultiOpenSchedulesAsync(selectedModels);
                },
                canExecute: () =>
                    IsMultiOpenEnabled && Items.Any(x => x.IsChecked)
            );
        }

        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void SetItems(IEnumerable<ScheduleModel> schedules)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void SetItems(IEnumerable<ScheduleModel> schedules)
        {
            
            foreach (var oldItem in Items)
                oldItem.CheckedChanged -= OnRowCheckedChanged;

            
            Items.Clear();

            
            foreach (var schedule in schedules)
            {
                var row = new ScheduleRowVm(schedule);
                row.CheckedChanged += OnRowCheckedChanged;
                Items.Add(row);
            }

            
            MultiOpenCommand.RaiseCanExecuteChanged();
        }

        
        
        
        
        
        
        
        private void OnRowCheckedChanged()
        {
            MultiOpenCommand.RaiseCanExecuteChanged();
        }

        
        
        
        
        private void UpdateSelectionCommands()
        {
            EditCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
            OpenProfileCommand.RaiseCanExecuteChanged();
        }

        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void ToggleRowSelection(ScheduleRowVm row)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void ToggleRowSelection(ScheduleRowVm row)
        {
            if (!IsMultiOpenEnabled)
                return;

            row.IsChecked = !row.IsChecked;
        }

        
        
        
        
        
        
        
        private static string FormatScheduleName(ScheduleModel model, int index)
        {
            if (!string.IsNullOrWhiteSpace(model.Name))
                return model.Name;

            return $"(Unnamed schedule #{index})";
        }
    }
}
