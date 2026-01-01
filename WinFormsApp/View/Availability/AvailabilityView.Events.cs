using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace WinFormsApp.View.Availability
{
    public partial class AvailabilityView
    {
        private void AssociateAndRaiseViewEvents()
        {
            btnSearch.Click += async (_, __) => await RaiseSafeAsync(SearchEvent);
            btnAdd.Click += async (_, __) => await RaiseSafeAsync(AddEvent);
            btnEdit.Click += async (_, __) => await RaiseSafeAsync(EditEvent);
            btnDelete.Click += async (_, __) => await RaiseSafeAsync(DeleteEvent);
            btnSave.Click += async (_, __) => await RaiseSafeAsync(SaveEvent);

            btnCancel.Click += async (_, __) => await RaiseSafeAsync(CancelEvent);
            btnCancelProfile.Click += async (_, __) => await RaiseSafeAsync(CancelEvent);
            btnBackToAvailabilityList.Click += async (_, __) => await RaiseSafeAsync(CancelEvent);
            btnBackToAvailabilityListFromProfile.Click += async (_, __) => await RaiseSafeAsync(CancelEvent);
            btnCancelProfile2.Click += async (_, __) => await RaiseSafeAsync(CancelEvent);
            btnCacnelAvailabilityEdit2.Click += async (_, __) => await RaiseSafeAsync(CancelEvent);
            btnCacnelAvailabilityEdit3.Click += async (_, __) => await RaiseSafeAsync(CancelEvent);

            btnAddEmployeeToGroup.Click += async (_, __) => await RaiseSafeAsync(AddEmployeeToGroupEvent);
            btnRemoveEmployeeFromGroup.Click += async (_, __) => await RaiseSafeAsync(RemoveEmployeeFromGroupEvent);

            btnAddNewBind.Click += async (_, __) =>
            {
                await RaiseSafeAsync(AddBindEvent);

                if (dataGridBinds.Rows.Count > 0)
                {
                    dataGridBinds.CurrentCell = dataGridBinds.Rows[^1].Cells[ColBindValue];
                    dataGridBinds.BeginEdit(true);
                }
            };

            btnDeleteBind.Click += async (_, __) =>
            {
                var bind = dataGridBinds.CurrentRow?.DataBoundItem as BindModel;
                if (bind is null) return;

                if (!Confirm($"Delete bind '{bind.Key}'?", "Confirm")) return;

                await RaiseSafeAsync(DeleteBindEvent, bind);
            };

            inputSearch.KeyDown += async (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                    await RaiseSafeAsync(SearchEvent);
            };

            dataGrid.CellDoubleClick += async (_, e) =>
            {
                if (e.RowIndex < 0) return;
                dataGrid.CurrentCell = dataGrid.Rows[e.RowIndex].Cells[0];
                await RaiseSafeAsync(OpenProfileEvent);
            };
        }

        private void WireNewControls()
        {
            comboboxEmployee.SelectedIndexChanged += (_, __) =>
            {
                EmployeeId = comboboxEmployee.SelectedValue is int id ? id : 0;
            };

            inputAvailabilityMonthName.TextChanged += (_, __) => errorProvider.SetError(inputAvailabilityMonthName, "");
            NumbAvailabilityMonth.ValueChanged += (_, __) =>
            {
                errorProvider.SetError(NumbAvailabilityMonth, "");
                RegenerateGroupDays();
            };
            NumbAvailabilityYear.ValueChanged += (_, __) =>
            {
                errorProvider.SetError(NumbAvailabilityYear, "");
                RegenerateGroupDays();
            };
        }

        private async Task RaiseSafeAsync(Func<CancellationToken, Task>? ev)
        {
            try { await (ev?.Invoke(_lifetimeCts.Token) ?? Task.CompletedTask); }
            catch (OperationCanceledException)
            {
                // нормальна ситуація при закритті форми/скасуванні
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private async Task RaiseSafeAsync<T>(Func<T, CancellationToken, Task>? ev, T arg)
        {
            try { await (ev?.Invoke(arg, _lifetimeCts.Token) ?? Task.CompletedTask); }
            catch (OperationCanceledException)
            {
                // нормальна ситуація при закритті форми/скасуванні
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }
    }
}
