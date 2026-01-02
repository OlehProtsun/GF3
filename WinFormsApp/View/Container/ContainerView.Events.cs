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
                catch (OperationCanceledException)
                {
                    // нормальна ситуація при закритті форми/скасуванні
                }
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
            BindClick(
                btnAdd,
                () => AddEvent,
                before: () => { CancelTarget = ContainerViewModel.List; });

            BindClick(
                btnEdit,
                () => EditEvent,
                before: () =>
                {
                    // якщо Edit натиснули з профайлу — Cancel має повернути в профайл
                    CancelTarget = tabControl.SelectedTab == tabProfile
                        ? ContainerViewModel.Profile
                        : ContainerViewModel.List;
                });
            BindClick(btnDelete, () => DeleteEvent);
            BindClick(btnSave, () => SaveEvent);

            // Cancel/back — явно кажемо “куди”
            BindClick(
                btnCancel,
                () => CancelEvent,
                before: () =>
                {
                    ClearValidationErrors();
                });

            BindClick(
                btnBackToContainerList,
                () => CancelEvent,
                before: () =>
                {
                    // нічого не перетираємо — використовуємо CancelTarget,
                    // який був встановлений при вході в Edit (btnEdit)
                    ClearValidationErrors(); // опційно
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
            BindClick(
                btnScheduleAdd,
                () => ScheduleAddEvent,
                before: () =>
                {
                    cancelEdit();
                    ScheduleCancelTarget = ScheduleViewModel.List;
                });

            scheduleGrid.CellDoubleClick += async (_, __) => await Raise(ScheduleOpenProfileEvent);

            BindClick(
                btnScheduleEdit,
                () => ScheduleEditEvent,
                before: () =>
                {
                    cancelEdit();
                    ScheduleCancelTarget = ScheduleViewModel.Profile;
                });
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
                    ClearScheduleValidationErrors();
                });

            BindClick(
                btnBackToScheduleList,
                () => ScheduleCancelEvent,
                before: () =>
                {
                    cancelEdit();
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

            inputYear.ValueChanged += (_, __) =>
            {
                RequestScheduleGridRefresh();
                _ = Raise(AvailabilitySelectionChangedEvent);
            };

            inputMonth.ValueChanged += (_, __) =>
            {
                RequestScheduleGridRefresh();
                _ = Raise(AvailabilitySelectionChangedEvent);
            };

            btnShowHideInfo.Click += (_, __) => ToggleScheduleInfo();

            checkedAvailabilities.ItemCheck += (_, __) =>
            {
                // ItemCheck спрацьовує ДО того, як CheckedItems реально оновився.
                // Тому відкладемо в message loop:
                BeginInvoke(new Action(async () =>
                    await SafeRaiseAsync(AvailabilitySelectionChangedEvent)));
            };

            BindClick(
                btnScheduleProfileCancel,
                () => ScheduleCancelEvent,
                before: () =>
                {
                    cancelEdit();
                    ScheduleCancelTarget = ScheduleViewModel.List; // зі schedule profile назад у schedule list (tabProfile)
                });

        }

        private async Task SafeRaiseAsync(Func<CancellationToken, Task>? ev)
        {
            if (ev == null) return;

            try { await ev(_lifetimeCts.Token); }
            catch (OperationCanceledException) { }
            catch (Exception ex) { ShowError(ex.Message); }
        }
    }
}
