using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp.View.Shared;

namespace WinFormsApp.View.Availability
{
    public partial class AvailabilityView
    {
        private readonly BusyOverlayController _busyController = null!;
        private readonly Dictionary<Control, bool> _busyEnabled = new();

        public CancellationToken LifetimeToken => _lifetimeCts.Token;

        public void ShowBusy(string? text = null) => _busyController.ShowBusy(text);

        public void HideBusy() => _busyController.HideBusy();

        public Task RunBusyAsync(Func<CancellationToken, Task> action, CancellationToken ct, string? text = null)
            => _busyController.RunBusyAsync(action, ct, text, SetBusyState);

        private void SetBusyState(bool enabled)
        {
            if (!enabled)
            {
                _busyEnabled.Clear();
                foreach (var control in BusyControls())
                {
                    _busyEnabled[control] = control.Enabled;
                    control.Enabled = false;
                }
                return;
            }

            foreach (var kvp in _busyEnabled)
                kvp.Key.Enabled = kvp.Value;
            _busyEnabled.Clear();
        }

        private IEnumerable<Control> BusyControls()
        {
            yield return btnSearch;
            yield return btnAdd;
            yield return btnEdit;
            yield return btnDelete;
            yield return btnSave;
            yield return btnCancel;
            yield return btnCancelProfile;
            yield return btnBackToAvailabilityList;
            yield return btnBackToAvailabilityListFromProfile;
            yield return btnCancelProfile2;
            yield return btnCacnelAvailabilityEdit2;
            yield return btnCacnelAvailabilityEdit3;
            yield return btnAddEmployeeToGroup;
            yield return btnRemoveEmployeeFromGroup;
            yield return btnAddNewBind;
            yield return btnDeleteBind;
        }
    }
}
