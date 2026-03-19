/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerListViewModel у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System.Collections.ObjectModel;    
using BusinessLogicLayer.Contracts.Models;            
using WPFApp.MVVM.Commands;
using WPFApp.MVVM.Core;
using WPFApp.ViewModel.Container.Edit;             

namespace WPFApp.ViewModel.Container.List
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class ContainerListViewModel : ViewModelBase` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ContainerListViewModel : ViewModelBase
    {
        
        
        
        
        
        
        
        
        
        
        
        
        private readonly ContainerViewModel _owner;

        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<ContainerModel> Items { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<ContainerModel> Items { get; } = new();

        
        
        
        
        
        
        
        
        
        
        
        private ContainerModel? _selectedItem;
        /// <summary>
        /// Визначає публічний елемент `public ContainerModel? SelectedItem` та контракт його використання у шарі WPFApp.
        /// </summary>
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

        
        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ContainerListViewModel(ContainerViewModel owner)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ContainerListViewModel(ContainerViewModel owner)
        {
            _owner = owner;

            SearchCommand = new AsyncRelayCommand(() => _owner.SearchAsync());
            AddNewCommand = new AsyncRelayCommand(() => _owner.StartAddAsync());

            EditCommand = new AsyncRelayCommand(
                () => _owner.EditSelectedAsync(),
                () => SelectedItem != null);

            DeleteCommand = new AsyncRelayCommand(
                () => _owner.DeleteSelectedAsync(),
                () => SelectedItem != null);

            OpenProfileCommand = new AsyncRelayCommand(
                () => _owner.OpenProfileAsync(),
                () => SelectedItem != null);
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void SetItems(IEnumerable<ContainerModel> containers)` та контракт його використання у шарі WPFApp.
        /// </summary>
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
