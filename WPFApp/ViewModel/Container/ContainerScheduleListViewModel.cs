using DataAccessLayer.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;
using WPFApp.Infrastructure;
using WPFApp.ViewModel.Dialogs;

namespace WPFApp.ViewModel.Container
{
    public sealed class ScheduleRowVm : ViewModelBase
    {
        public ScheduleModel Model { get; }

        public ScheduleRowVm(ScheduleModel model)
        {
            Model = model;
        }

        // прокидаємо поля для DataGrid, щоб XAML майже не міняти
        public string Name => Model.Name;
        public int Year => Model.Year;
        public int Month => Model.Month;
        public ShopModel? Shop => Model.Shop;

        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (SetProperty(ref _isChecked, value))
                    MultiSelectedChanged?.Invoke();
            }
        }

        public Action? MultiSelectedChanged { get; set; }
    }


    public sealed class ContainerScheduleListViewModel : ViewModelBase
    {
        private readonly ContainerViewModel _owner;

        public ObservableCollection<ScheduleRowVm> Items { get; } = new();

        private ScheduleRowVm? _selectedItem;
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
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }


        private bool _isMultiOpenEnabled;
        public bool IsMultiOpenEnabled
        {
            get => _isMultiOpenEnabled;
            set
            {
                if (_isMultiOpenEnabled == value) return;
                _isMultiOpenEnabled = value;
                OnPropertyChanged(nameof(IsMultiOpenEnabled));

                if (!_isMultiOpenEnabled)
                {
                    foreach (var it in Items)
                        it.IsChecked = false;
                }

                ((RelayCommand)MultiOpenCommand).RaiseCanExecuteChanged();
            }
        }



        public ICommand ToggleMultiOpenCommand { get; }
        public ICommand MultiOpenCommand { get; }

        public AsyncRelayCommand SearchCommand { get; }
        public AsyncRelayCommand AddNewCommand { get; }
        public AsyncRelayCommand EditCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }
        public AsyncRelayCommand OpenProfileCommand { get; }

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

            MultiOpenCommand = new RelayCommand(
                execute: () =>
                {
                    var selectedModels = Items
                        .Where(x => x.IsChecked)
                        .Select(x => x.Model)
                        .ToList();

                    var ids = string.Join(", ", selectedModels.Select(x => x.Id));
                    _owner.ShowInfo(
                        $"MultiOpen placeholder: {selectedModels.Count} schedule(s) would be opened. IDs: {ids}");
                },
                canExecute: () =>
                    IsMultiOpenEnabled && Items.Any(x => x.IsChecked)
            );



        }

        public void SetItems(IEnumerable<ScheduleModel> schedules)
        {
            Items.Clear();
            foreach (var schedule in schedules)
                Items.Add(new ScheduleRowVm(schedule));

            WireMultiSelectHooks();
            ((RelayCommand)MultiOpenCommand).RaiseCanExecuteChanged();
        }


        private void UpdateSelectionCommands()
        {
            EditCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
            OpenProfileCommand.RaiseCanExecuteChanged();
        }

        private void WireMultiSelectHooks()
        {
            foreach (var it in Items)
            {
                it.MultiSelectedChanged = () =>
                {
                    ((RelayCommand)MultiOpenCommand).RaiseCanExecuteChanged();
                };
            }
        }

    }
}
