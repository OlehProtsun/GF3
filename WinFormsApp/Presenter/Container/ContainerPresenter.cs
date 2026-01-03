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
            _view.SearchEvent += ct => RunBusySafeAsync(OnSearchCoreAsync, ct, "Searching containers...");
            _view.AddEvent += ct => SafeAsync(OnAddCoreAsync, ct);
            _view.EditEvent += ct => SafeAsync(OnEditCoreAsync, ct);
            _view.DeleteEvent += ct => RunBusySafeAsync(OnDeleteCoreAsync, ct, "Deleting container...");
            _view.SaveEvent += ct => RunBusySafeAsync(OnSaveCoreAsync, ct, "Saving container...");
            _view.CancelEvent += ct => SafeAsync(OnCancelCoreAsync, ct);
            _view.OpenProfileEvent += ct => RunBusySafeAsync(OnOpenProfileCoreAsync, ct, "Loading container...");

            // Schedule
            _view.ScheduleSearchEvent += ct => RunBusySafeAsync(OnScheduleSearchCoreAsync, ct, "Searching schedules...");
            _view.ScheduleAddEvent += ct => RunBusySafeAsync(OnScheduleAddCoreAsync, ct, "Loading schedule...");
            _view.ScheduleEditEvent += ct => RunBusySafeAsync(OnScheduleEditCoreAsync, ct, "Loading schedule...");
            _view.ScheduleDeleteEvent += ct => RunBusySafeAsync(OnScheduleDeleteCoreAsync, ct, "Deleting schedule...");
            _view.ScheduleSaveEvent += ct => RunBusySafeAsync(OnScheduleSaveCoreAsync, ct, "Saving schedule...");
            _view.ScheduleCancelEvent += ct => SafeAsync(OnScheduleCancelCoreAsync, ct);
            _view.ScheduleOpenProfileEvent += ct => RunBusySafeAsync(OnScheduleOpenProfileCoreAsync, ct, "Loading schedule...");
            _view.ScheduleGenerateEvent += ct => RunBusySafeAsync(OnScheduleGenerateCoreAsync, ct, "Generating schedule...");

            _view.AvailabilitySelectionChangedEvent += ct => SafeAsync(OnAvailabilitySelectionChangedCoreAsync, ct);


            _view.SetContainerBindingSource(_containerBinding);
            _view.SetScheduleBindingSource(_scheduleBinding);
            _view.SetSlotBindingSource(_slotBinding);
        }

        public Task InitializeAsync()
            => _view.RunBusyAsync(async ct =>
            {
                await LoadContainersAsync(ct);
                await LoadLookupsAsync(ct);
            }, _view.LifetimeToken, "Loading containers...");

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

        private Task RunBusySafeAsync(Func<CancellationToken, Task> action, CancellationToken ct, string? busyText)
            => _view.RunBusyAsync(async innerCt =>
            {
                try
                {
                    await action(innerCt);
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
            }, ct, busyText);

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
