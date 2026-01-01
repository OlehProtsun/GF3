using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp.ViewModel;

namespace WinFormsApp.View.Main
{
    public partial class MainView : Form, IMainView
    {
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int HTCAPTION = 0x0002;

        private readonly CancellationTokenSource _navCts = new();

        private Panel? _busyOverlay;
        private Guna.UI2.WinForms.Guna2GroupBox? _busyBox;
        private Guna.UI2.WinForms.Guna2CircleProgressBar? _busyCircle;
        private Label? _busyText;
        private System.Windows.Forms.Timer? _busyTimer;


        private NavPage _activePage = NavPage.None;
        public NavPage ActivePage => _activePage;

        private readonly Color _navActiveFill = Color.RoyalBlue;
        private readonly Color _navActiveFore = Color.White;

        private readonly Color _navNormalFill = Color.DarkGray;
        private readonly Color _navNormalFore = Color.Black;
        //private readonly Color _navNormalBorder = Color.FromArgb(220, 220, 220);

        public MainView()
        {
            InitializeComponent();
            WindowState = FormWindowState.Maximized;

            btnEmployee.Click += async (_, __) => await InvokeNavAsync(ShowEmployeeView);
            btnAvailability.Click += async (_, __) => await InvokeNavAsync(ShowAvailabilityView);
            btnContainer.Click += async (_, __) => await InvokeNavAsync(ShowContainerView);

            btnCloseProgram.Click += (_, __) => Application.Exit(); // краще ніж Environment.Exit(0)
            btnProgramMinimalize.Click += (_, __) => WindowState = FormWindowState.Minimized;
        }

        public event Func<CancellationToken, Task>? ShowEmployeeView;
        public event Func<CancellationToken, Task>? ShowAvailabilityView;
        public event Func<CancellationToken, Task>? ShowContainerView;

        private async Task InvokeNavAsync(Func<CancellationToken, Task>? handler)
        {
            var h = handler;
            if (h is null) return;

            // опційно: захист від “дубль-кліку”
            SetNavButtonsEnabled(false);
            try
            {
                await h(_navCts.Token);
            }
            finally
            {
                SetNavButtonsEnabled(true);
            }
        }
        private void SetNavButtonsEnabled(bool enabled)
        {
            if (!enabled)
            {
                btnEmployee.Enabled = false;
                btnAvailability.Enabled = false;
                btnContainer.Enabled = false;
                return;
            }

            // Увімкнути все, крім активної сторінки
            btnEmployee.Enabled = _activePage != NavPage.Employee;
            btnAvailability.Enabled = _activePage != NavPage.Availability;
            btnContainer.Enabled = _activePage != NavPage.Container;
        }
        public void BeginWindowDrag()
        {
            ReleaseCapture();
            SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
        }
        public void ShowBusy(string? text = null)
        {
            if (_busyOverlay == null)
            {
                _busyOverlay = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    Visible = false
                };

                _busyCircle = new Guna.UI2.WinForms.Guna2CircleProgressBar
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

                _busyBox = new Guna.UI2.WinForms.Guna2GroupBox
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
                    Text = "" // важливо: щоб не було заголовка groupbox
                };

                _busyBox.ShadowDecoration.BorderRadius = 25;
                _busyBox.ShadowDecoration.Depth = 3;
                _busyBox.ShadowDecoration.Enabled = true;

                _busyBox.Controls.Add(_busyText);
                _busyBox.Controls.Add(_busyCircle);

                _busyOverlay.Controls.Add(_busyBox);

                // Центрування box по overlay
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

                Controls.Add(_busyOverlay);
                _busyOverlay.BringToFront();

                // Анімація "кручення" для circle progress
                _busyTimer = new System.Windows.Forms.Timer { Interval = 20 };
                _busyTimer.Tick += (_, __) =>
                {
                    if (_busyCircle == null) return;
                    int v = _busyCircle.Value + 2;
                    _busyCircle.Value = (v >= _busyCircle.Maximum) ? 0 : v;
                };
            }

            _busyText!.Text = string.IsNullOrWhiteSpace(text) ? "Loading..." : text;

            Cursor = Cursors.WaitCursor;

            _busyOverlay!.Visible = true;
            _busyOverlay.BringToFront();

            _busyCircle!.Value = 0;
            _busyTimer!.Start();

            // Без DoEvents — просто форсуємо перемалювання
            _busyOverlay.Update();
        }
        public void HideBusy()
        {
            _busyTimer?.Stop();

            if (_busyOverlay != null)
                _busyOverlay.Visible = false;

            Cursor = Cursors.Default;
        }
        public async Task RunBusyAsync(Func<CancellationToken, Task> action, CancellationToken ct, string? text = null)
        {
            ShowBusy(text);
            try
            {
                await Task.Yield(); // UI встигає намалювати overlay
                await action(ct);
            }
            finally
            {
                HideBusy();
            }
        }
        public void SetActivePage(NavPage page)
        {
            _activePage = page;

            ApplyNavStyle(btnEmployee, page == NavPage.Employee);
            ApplyNavStyle(btnAvailability, page == NavPage.Availability);
            ApplyNavStyle(btnContainer, page == NavPage.Container);

            // один центр контролю Enabled
            SetNavButtonsEnabled(true);
        }
        private void ApplyNavStyle(Guna2Button btn, bool isActive)
        {
            if (isActive)
            {
                btn.FillColor = _navActiveFill;
                btn.ForeColor = _navActiveFore;

                // щоб DisabledState не робив сірою, коли ми вимкнемо кнопку в іншому місці
                btn.DisabledState.FillColor = _navActiveFill;
                btn.DisabledState.ForeColor = _navActiveFore;
                btn.DisabledState.BorderColor = _navActiveFill;
                btn.DisabledState.CustomBorderColor = _navActiveFill;
            }
            else
            {
                btn.FillColor = _navNormalFill;
                btn.ForeColor = _navNormalFore;
            }
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try { _navCts.Cancel(); } catch { /* ignore */ }
            _navCts.Dispose();

            try { _busyTimer?.Stop(); } catch { /* ignore */ }
            _busyTimer?.Dispose();

            // overlay/контроли — теж прибираємо
            _busyOverlay?.Dispose();
            _busyOverlay = null;
            _busyBox = null;
            _busyCircle = null;
            _busyText = null;

            base.OnFormClosed(e);
        }

    }
}
