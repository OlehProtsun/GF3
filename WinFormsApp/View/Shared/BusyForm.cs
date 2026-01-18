using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsApp.View.Shared
{
    public abstract class BusyForm : Form, IBusyView
    {
        private readonly BusyOverlayController _busyController;
        private readonly Dictionary<Control, bool> _busyEnabled = new();

        protected BusyForm()
        {
            _busyController = new BusyOverlayController(this);
        }

        public abstract CancellationToken LifetimeToken { get; }

        public void ShowBusy(string? text = null) => _busyController.ShowBusy(text);

        public void HideBusy() => _busyController.HideBusy();

        public Task RunBusyAsync(Func<CancellationToken, Task> action, CancellationToken ct, string? text = null)
            => _busyController.RunBusyAsync(action, ct, text, SetBusyState);

        public Task RunBusyAsync(Func<CancellationToken, IProgress<int>?, Task> action, CancellationToken ct, string? text = null)
            => _busyController.RunBusyAsync(action, ct, text, SetBusyState);

        protected abstract IEnumerable<Control> BusyControls();

        protected virtual void SetBusyState(bool enabled)
        {
            if (!enabled)
            {
                _busyEnabled.Clear();
                foreach (var control in BusyControls())
                {
                    if (control == null)
                    {
                        continue;
                    }

                    _busyEnabled[control] = control.Enabled;
                    control.Enabled = false;
                }
                return;
            }

            foreach (var kvp in _busyEnabled)
            {
                kvp.Key.Enabled = kvp.Value;
            }
            _busyEnabled.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _busyController.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
