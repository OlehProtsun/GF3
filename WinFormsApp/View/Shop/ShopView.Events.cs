using System;
using System.Collections.Generic;
using System.Text;
using WinFormsApp.ViewModel;

namespace WinFormsApp.View.Shop
{
    public partial class ShopView
    {
        private async Task RaiseAsync(Func<CancellationToken, Task>? handler)
        {
            if (handler is null) return;

            try
            {
                await handler(_lifetimeCts.Token);
            }
            catch (OperationCanceledException)
            {
                // нормальна ситуація при закритті форми/скасуванні
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void BindClick(Control control, Func<Func<CancellationToken, Task>?> handlerFactory, Action? before = null)
        {
            control.Click += async (_, __) =>
            {
                before?.Invoke();
                await RaiseAsync(handlerFactory());
            };
        }

        private void AssociateAndRaiseViewEvents()
        {
            BindClick(btnSearch, () => SearchEvent);
            BindClick(btnAdd, () => AddEvent);

            BindClick(btnEdit, () => EditEvent);
            BindClick(btnDelete, () => DeleteEvent);
            BindClick(btnSave, () => SaveEvent);

            BindClick(btnCancel, () => CancelEvent, () => CancelTarget = ShopViewModel.List);
            BindClick(btnCancelProfile, () => CancelEvent, () => CancelTarget = ShopViewModel.List);
            BindClick(btnBackToShopList, () => CancelEvent, () => CancelTarget = ShopViewModel.List);
            BindClick(btnBackToShopListFromProfile, () => CancelEvent, () => CancelTarget = ShopViewModel.List);

            inputSearch.KeyDown += async (_, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;

                e.Handled = true;
                e.SuppressKeyPress = true;
                await RaiseAsync(SearchEvent);
            };

            dataGrid.CellDoubleClick += async (_, e) =>
            {
                if (e.RowIndex < 0) return;
                await RaiseAsync(OpenProfileEvent);
            };

            inputFirstName.TextChanged += (_, __) => errorProvider.SetError(inputFirstName, "");
            inputLastName.TextChanged += (_, __) => errorProvider.SetError(inputLastName, "");
            inputEmail.TextChanged += (_, __) => errorProvider.SetError(inputEmail, "");
        }
    }
}
