using System.Collections.ObjectModel;
using DataAccessLayer.Models;
using WPFApp.Infrastructure;

namespace WPFApp.ViewModel.Container
{
    public sealed class ContainerProfileViewModel : ViewModelBase
    {
        private readonly ContainerViewModel _owner;

        private int _containerId;
        public int ContainerId
        {
            get => _containerId;
            set => SetProperty(ref _containerId, value);
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string? _note;
        public string? Note
        {
            get => _note;
            set => SetProperty(ref _note, value);
        }

        public ContainerScheduleListViewModel ScheduleListVm { get; }

        public AsyncRelayCommand BackCommand { get; }
        public AsyncRelayCommand CancelProfileCommand { get; }
        public AsyncRelayCommand EditCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }

        public ContainerProfileViewModel(ContainerViewModel owner)
        {
            _owner = owner;
            ScheduleListVm = new ContainerScheduleListViewModel(owner);

            BackCommand = new AsyncRelayCommand(() => _owner.CancelAsync());
            CancelProfileCommand = new AsyncRelayCommand(() => _owner.CancelAsync());
            EditCommand = new AsyncRelayCommand(() => _owner.EditSelectedAsync());
            DeleteCommand = new AsyncRelayCommand(() => _owner.DeleteSelectedAsync());
        }

        public void SetProfile(ContainerModel model)
        {
            ContainerId = model.Id;
            Name = model.Name;
            Note = model.Note;
        }
    }
}
