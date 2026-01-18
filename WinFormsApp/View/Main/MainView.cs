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
using WinFormsApp.View.Shared;

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

        private CancellationTokenSource _navCts = new();

        private readonly BusyOverlayController _busyController;


        private NavPage _activePage = NavPage.None;
        public NavPage ActivePage => _activePage;

        public Control ContentHost => PanelContentMain;

        private readonly Color _navActiveFill = Color.DarkGray;
        private readonly Color _navNormalFill = Color.White;

        public MainView()
        {
            InitializeComponent();
            WindowState = FormWindowState.Maximized;
            _busyController = new BusyOverlayController(this);

            btnEmployee.Click += async (_, __) => await InvokeNavAsync(ShowEmployeeView);
            btnShop.Click += async (_, __) => await InvokeNavAsync(ShowShopView);
            btnAvailability.Click += async (_, __) => await InvokeNavAsync(ShowAvailabilityView);
            btnContainer.Click += async (_, __) => await InvokeNavAsync(ShowContainerView);

            btnCloseProgram.Click += (_, __) => Application.Exit(); // краще ніж Environment.Exit(0)
            btnProgramMinimalize.Click += (_, __) => WindowState = FormWindowState.Minimized;
        }

        public event Func<CancellationToken, Task>? ShowEmployeeView;
        public event Func<CancellationToken, Task>? ShowShopView;
        public event Func<CancellationToken, Task>? ShowAvailabilityView;
        public event Func<CancellationToken, Task>? ShowContainerView;

        private async Task InvokeNavAsync(Func<CancellationToken, Task>? handler)
        {
            var h = handler;
            if (h is null) return;

            ResetNavigationToken();

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
                btnShop.Enabled = false;
                btnAvailability.Enabled = false;
                btnContainer.Enabled = false;
                return;
            }

            // Увімкнути все, крім активної сторінки
            btnEmployee.Enabled = _activePage != NavPage.Employee;
            btnShop.Enabled = _activePage != NavPage.Shop;
            btnAvailability.Enabled = _activePage != NavPage.Availability;
            btnContainer.Enabled = _activePage != NavPage.Container;
        }
        public void BeginWindowDrag()
        {
            ReleaseCapture();
            SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
        }

        public CancellationToken LifetimeToken => _navCts.Token;

        public void ShowBusy(string? text = null)
            => _busyController.ShowBusy(text);
        public void HideBusy()
            => _busyController.HideBusy();
        public async Task RunBusyAsync(Func<CancellationToken, Task> action, CancellationToken ct, string? text = null)
            => await _busyController.RunBusyAsync(action, ct, text, SetNavButtonsEnabled);
        public async Task RunBusyAsync(Func<CancellationToken, IProgress<int>?, Task> action, CancellationToken ct, string? text = null)
            => await _busyController.RunBusyAsync(action, ct, text, SetNavButtonsEnabled);
        public void SetActivePage(NavPage page)
        {
            _activePage = page;

            ApplyNavStyle(btnEmployee, page == NavPage.Employee);
            ApplyNavStyle(btnShop, page == NavPage.Shop);
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

                // щоб DisabledState не робив сірою, коли ми вимкнемо кнопку в іншому місці
                btn.DisabledState.FillColor = _navActiveFill;
            }
            else
            {
                btn.FillColor = _navNormalFill;
            }
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try { _navCts.Cancel(); } catch { /* ignore */ }
            _navCts.Dispose();

            _busyController.Dispose();

            base.OnFormClosed(e);
        }

        private void btnAvailability_Click(object sender, EventArgs e)
        {

        }

        private void ResetNavigationToken()
        {
            try { _navCts.Cancel(); } catch { /* ignore */ }
            _navCts.Dispose();
            _navCts = new CancellationTokenSource();
        }
    }
}
