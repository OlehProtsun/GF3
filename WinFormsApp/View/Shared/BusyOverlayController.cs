using Guna.UI2.WinForms;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsApp.View.Shared
{
    public sealed class BusyOverlayController : IDisposable
    {
        private readonly Form _host;
        private Panel? _busyOverlay;
        private Guna2GroupBox? _busyBox;
        private Guna2CircleProgressBar? _busyCircle;
        private Label? _busyText;
        private Timer? _busyTimer;
        private bool _disposed;

        public BusyOverlayController(Form host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public void ShowBusy(string? text = null)
        {
            _ = InvokeAsync(() => ShowBusyCore(text));
        }

        public void HideBusy()
        {
            _ = InvokeAsync(HideBusyCore);
        }

        public async Task RunBusyAsync(
            Func<CancellationToken, Task> action,
            CancellationToken ct,
            string? text = null,
            Action<bool>? setUiEnabled = null)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));
            if (ct.IsCancellationRequested) return;

            await InvokeAsync(() =>
            {
                setUiEnabled?.Invoke(false);
                ShowBusyCore(text);
            });

            try
            {
                await Task.Yield();
                await action(ct);
            }
            finally
            {
                await InvokeAsync(() =>
                {
                    HideBusyCore();
                    setUiEnabled?.Invoke(true);
                });
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try { _busyTimer?.Stop(); } catch { /* ignore */ }
            _busyTimer?.Dispose();

            _busyOverlay?.Dispose();
            _busyOverlay = null;
            _busyBox = null;
            _busyCircle = null;
            _busyText = null;
        }

        private void ShowBusyCore(string? text)
        {
            if (_disposed || _host.IsDisposed || _host.Disposing) return;

            EnsureOverlay();

            _busyText!.Text = string.IsNullOrWhiteSpace(text) ? "Loading..." : text;
            _host.Cursor = Cursors.WaitCursor;

            _busyOverlay!.Visible = true;
            _busyOverlay.BringToFront();

            _busyCircle!.Value = 0;
            _busyTimer!.Start();

            _busyOverlay.Update();
        }

        private void HideBusyCore()
        {
            if (_disposed || _host.IsDisposed || _host.Disposing) return;

            _busyTimer?.Stop();

            if (_busyOverlay != null)
                _busyOverlay.Visible = false;

            _host.Cursor = Cursors.Default;
        }

        private void EnsureOverlay()
        {
            if (_busyOverlay != null) return;

            _busyOverlay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Visible = false
            };

            _busyCircle = new Guna2CircleProgressBar
            {
                FillColor = Color.FromArgb(200, 213, 218, 223),
                Font = new Font("Segoe UI", 12F),
                ForeColor = Color.White,
                Location = new Point(8, 11),
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Name = "busyCircle",
                Size = new Size(32, 32),
            };
            _busyCircle.ShadowDecoration.Mode = Guna.UI2.WinForms.Enums.ShadowMode.Circle;

            _busyText = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0),
                ForeColor = Color.Black,
                Location = new Point(42, 14),
                Name = "busyLabel",
                Text = ""
            };

            _busyBox = new Guna2GroupBox
            {
                BackColor = Color.Transparent,
                BorderColor = Color.White,
                BorderRadius = 25,
                BorderThickness = 0,
                CustomBorderColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.White,
                Name = "busyBox",
                Size = new Size(250, 53),
                Text = ""
            };

            _busyBox.ShadowDecoration.BorderRadius = 25;
            _busyBox.ShadowDecoration.Depth = 3;
            _busyBox.ShadowDecoration.Enabled = true;

            _busyBox.Controls.Add(_busyText);
            _busyBox.Controls.Add(_busyCircle);

            _busyOverlay.Controls.Add(_busyBox);

            void CenterBox()
            {
                if (_busyOverlay == null || _busyBox == null) return;
                _busyBox.Location = new Point(
                    (_busyOverlay.ClientSize.Width - _busyBox.Width) / 2,
                    (_busyOverlay.ClientSize.Height - _busyBox.Height) / 2
                );
            }

            _busyOverlay.Resize += (_, __) => CenterBox();
            CenterBox();

            _host.Controls.Add(_busyOverlay);
            _busyOverlay.BringToFront();

            _busyTimer = new Timer { Interval = 20 };
            _busyTimer.Tick += (_, __) =>
            {
                if (_busyCircle == null) return;
                int v = _busyCircle.Value + 2;
                _busyCircle.Value = (v >= _busyCircle.Maximum) ? 0 : v;
            };
        }

        private Task InvokeAsync(Action action)
        {
            if (_disposed || _host.IsDisposed || _host.Disposing) return Task.CompletedTask;

            if (!_host.InvokeRequired)
            {
                action();
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            _host.BeginInvoke(new Action(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }));
            return tcs.Task;
        }
    }
}
