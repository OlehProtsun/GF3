using System;
using System.Collections.Generic;
using System.Text;

namespace WinFormsApp.View.Container
{
    public partial class ContainerView
    {
        private void AssociateAndRaiseEvents()
        {
            async Task Raise(Func<CancellationToken, Task>? ev, CancellationToken ct = default)
            {
                if (ev == null) return;
                try { await ev(ct); }
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

            BindClick(btnCancel, () => CancelEvent);
            BindClick(btnBackToContainerList, () => CancelEvent);

            BindClick(btnBackToContainerListFromProfile, () => CancelEvent);
            BindClick(btnCancelProfile, () => CancelEvent);
            BindClick(btnCancelProfile2, () => CancelEvent);

            containerGrid.CellDoubleClick += async (_, __) => await Raise(OpenProfileEvent);

            // Schedule
            Action cancelEdit = () => CancelGridEditSafely(slotGrid);

            BindClick(btnScheduleSearch, () => ScheduleSearchEvent, cancelEdit);
            BindClick(btnScheduleAdd, () => ScheduleAddEvent, cancelEdit);

            scheduleGrid.CellDoubleClick += async (_, __) => await Raise(ScheduleOpenProfileEvent);

            BindClick(btnScheduleEdit, () => ScheduleEditEvent, cancelEdit);
            BindClick(btnScheduleDelete, () => ScheduleDeleteEvent, cancelEdit);

            BindClick(btnBackToContainerProfileFromSheduleProfile, () => ScheduleCancelEvent, cancelEdit);

            BindClick(btnGenerate, () => ScheduleGenerateEvent, cancelEdit);
            BindClick(btnScheduleCancel, () => ScheduleCancelEvent, cancelEdit);
            BindClick(btnBackToScheduleList, () => ScheduleCancelEvent, cancelEdit);

            btnScheduleSave.Click += async (_, __) =>
            {
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
