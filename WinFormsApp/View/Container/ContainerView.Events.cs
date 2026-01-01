using System;
using System.Collections.Generic;
using System.Text;
using WinFormsApp.ViewModel;


namespace WinFormsApp.View.Container
{
    public partial class ContainerView
    {
        private void AssociateAndRaiseEvents()
        {
            async Task Raise(Func<CancellationToken, Task>? ev)
            {
                if (ev == null) return;
                try { await ev(_lifetimeCts.Token); }
                catch (Exception ex) { ShowError(ex.Message); }
            }

            void BindClick(Control c, Func<Func<CancellationToken, Task>?> evAccessor, Action? before = null)
                => c.Click += async (_, __) =>
                {
                    before?.Invoke();
                    await Raise(evAccessor());
                };

            // Container
            BindClick(btnSearch, () => SearchEvent);
            BindClick(btnAdd, () => AddEvent);
            BindClick(btnEdit, () => EditEvent);
            BindClick(btnDelete, () => DeleteEvent);
            BindClick(btnSave, () => SaveEvent);

            // Cancel/back — явно кажемо “куди”
            BindClick(
                btnCancel,
                () => CancelEvent,
                before: () =>
                {
                    CancelTarget = WinFormsApp.ViewModel.ContainerViewModel.List;
                    ClearValidationErrors();
                });

            BindClick(
                btnBackToContainerList,
                () => CancelEvent,
                before: () =>
                {
                    CancelTarget = ContainerViewModel.List;
                });

            BindClick(
                btnBackToContainerListFromProfile,
                () => CancelEvent,
                before: () =>
                {
                    CancelTarget = ContainerViewModel.List;
                });

            BindClick(
                btnCancelProfile,
                () => CancelEvent,
                before: () =>
                {
                    CancelTarget = ContainerViewModel.List;
                });

            BindClick(
                btnCancelProfile2,
                () => CancelEvent,
                before: () =>
                {
                    CancelTarget = ContainerViewModel.List;
                });

            containerGrid.CellDoubleClick += async (_, __) => await Raise(OpenProfileEvent);

            // Schedule
            Action cancelEdit = () => CancelGridEditSafely(slotGrid);

            BindClick(btnScheduleSearch, () => ScheduleSearchEvent, cancelEdit);
            BindClick(btnScheduleAdd, () => ScheduleAddEvent, cancelEdit);

            scheduleGrid.CellDoubleClick += async (_, __) => await Raise(ScheduleOpenProfileEvent);

            BindClick(btnScheduleEdit, () => ScheduleEditEvent, cancelEdit);
            BindClick(btnScheduleDelete, () => ScheduleDeleteEvent, cancelEdit);


            BindClick(
                btnGenerate,
                () => ScheduleGenerateEvent,
                before: () =>
                {
                    cancelEdit();
                    ClearScheduleValidationErrors();   // ✅ прибирає старі хрестики
                });

            BindClick(
                btnBackToContainerProfileFromSheduleProfile,
                () => ScheduleCancelEvent,
                before: () =>
                {
                    cancelEdit();
                    ScheduleCancelTarget = ScheduleViewModel.List;
                });

            BindClick(
                btnScheduleCancel,
                () => ScheduleCancelEvent,
                before: () =>
                {
                    cancelEdit();
                    ScheduleCancelTarget = ScheduleViewModel.List;
                    ClearScheduleValidationErrors();
                });

            BindClick(
                btnBackToScheduleList,
                () => ScheduleCancelEvent,
                before: () =>
                {
                    cancelEdit();
                    ScheduleCancelTarget = ScheduleViewModel.List;
                });

            btnScheduleSave.Click += async (_, __) =>
            {
                cancelEdit();
                ClearScheduleValidationErrors(); // ✅ очищає старі значки

                var ok = true;
                try { ok = slotGrid.EndEdit(); } catch { ok = false; }
                if (!ok || slotGrid.IsCurrentCellInEditMode) return;

                await Raise(ScheduleSaveEvent);
            };

            inputYear.ValueChanged += (_, __) => RequestScheduleGridRefresh();
            inputMonth.ValueChanged += (_, __) => RequestScheduleGridRefresh();
        }
    }
}
