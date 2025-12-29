using BusinessLogicLayer.Generators;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp.View.Container;
using WinFormsApp.ViewModel;

namespace WinFormsApp.Presenter.Container
{
    public sealed partial class ContainerPresenter
    {
        private readonly IContainerView _view;
        private readonly IContainerService _containerService;
        private readonly IScheduleService _scheduleService;
        private readonly IAvailabilityGroupService _availabilityGroupService;
        private readonly IScheduleGenerator _generator;

        private readonly BindingSource _containerBinding = new();
        private readonly BindingSource _scheduleBinding = new();
        private readonly BindingSource _slotBinding = new();

        public ContainerPresenter(
            IContainerView view,
            IContainerService containerService,
            IScheduleService scheduleService,
            IAvailabilityGroupService availabilityGroupService,
            IScheduleGenerator generator)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _containerService = containerService ?? throw new ArgumentNullException(nameof(containerService));
            _scheduleService = scheduleService ?? throw new ArgumentNullException(nameof(scheduleService));
            _availabilityGroupService = availabilityGroupService ?? throw new ArgumentNullException(nameof(availabilityGroupService));
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));

            // Container
            _view.SearchEvent += ct => SafeAsync(OnSearchCoreAsync, ct);
            _view.AddEvent += ct => SafeAsync(OnAddCoreAsync, ct);
            _view.EditEvent += ct => SafeAsync(OnEditCoreAsync, ct);
            _view.DeleteEvent += ct => SafeAsync(OnDeleteCoreAsync, ct);
            _view.SaveEvent += ct => SafeAsync(OnSaveCoreAsync, ct);
            _view.CancelEvent += ct => SafeAsync(OnCancelCoreAsync, ct);
            _view.OpenProfileEvent += ct => SafeAsync(OnOpenProfileCoreAsync, ct);

            // Schedule
            _view.ScheduleSearchEvent += ct => SafeAsync(OnScheduleSearchCoreAsync, ct);
            _view.ScheduleAddEvent += ct => SafeAsync(OnScheduleAddCoreAsync, ct);
            _view.ScheduleEditEvent += ct => SafeAsync(OnScheduleEditCoreAsync, ct);
            _view.ScheduleDeleteEvent += ct => SafeAsync(OnScheduleDeleteCoreAsync, ct);
            _view.ScheduleSaveEvent += ct => SafeAsync(OnScheduleSaveCoreAsync, ct);
            _view.ScheduleCancelEvent += ct => SafeAsync(OnScheduleCancelCoreAsync, ct);
            _view.ScheduleOpenProfileEvent += ct => SafeAsync(OnScheduleOpenProfileCoreAsync, ct);
            _view.ScheduleGenerateEvent += ct => SafeAsync(OnScheduleGenerateCoreAsync, ct);

            _view.SetContainerBindingSource(_containerBinding);
            _view.SetScheduleBindingSource(_scheduleBinding);
            _view.SetSlotBindingSource(_slotBinding);
        }

        public async Task InitializeAsync()
        {
            await LoadContainersAsync(CancellationToken.None);
            await LoadLookupsAsync(CancellationToken.None);
        }

        // -------------------------
        // Shared infrastructure
        // -------------------------

        private async Task SafeAsync(Func<CancellationToken, Task> action, CancellationToken ct)
        {
            try
            {
                await action(ct);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                _view.ShowError(msg);
            }
        }

        private ContainerModel? CurrentContainer => _containerBinding.Current as ContainerModel;
        private ScheduleModel? CurrentSchedule => _scheduleBinding.Current as ScheduleModel;

        private ContainerModel? CurrentContainerOrError()
        {
            var container = CurrentContainer;
            if (container is null)
                _view.ShowError("Select a container first.");
            return container;
        }

        private void SwitchBackFromContainerEdit()
        {
            _view.ClearValidationErrors();

            if (_view.CancelTarget == ContainerViewModel.Profile)
                _view.SwitchToProfileMode();
            else
                _view.SwitchToListMode();
        }

        private void SwitchBackFromScheduleEdit()
        {
            _view.ClearScheduleValidationErrors();

            if (_view.ScheduleCancelTarget == ScheduleViewModel.Profile)
                _view.SwitchToScheduleProfileMode();
            else
                _view.SwitchToScheduleListMode();
        }
    }
}
