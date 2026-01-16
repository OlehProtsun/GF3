using DataAccessLayer.Models;
using System.Collections.ObjectModel;
using System.Linq;
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

                MultiOpenCommand.RaiseCanExecuteChanged();
            }
        }



        public ICommand ToggleMultiOpenCommand { get; }
        public AsyncRelayCommand MultiOpenCommand { get; }

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

            ToggleMultiOpenCommand = new Infrastructure.RelayCommand(() =>
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

        public void SetItems(IEnumerable<ScheduleModel> schedules)
        {
            Items.Clear();
            foreach (var schedule in schedules)
                Items.Add(new ScheduleRowVm(schedule));

            WireMultiSelectHooks();
            MultiOpenCommand.RaiseCanExecuteChanged();
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
                    MultiOpenCommand.RaiseCanExecuteChanged();
                };
            }
        }

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
