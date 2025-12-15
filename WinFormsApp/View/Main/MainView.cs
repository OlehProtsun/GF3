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

        public MainView()
        {
            InitializeComponent();

            btnEmployee.Click += async (_, __) =>
            {
                if (ShowEmployeeView is not null)
                    await ShowEmployeeView(CancellationToken.None);
            };

            btnAvailability.Click += async (_, __) =>
            {
                if (ShowAvailabilityView is not null)
                    await ShowAvailabilityView(CancellationToken.None);
            };

            btnShop.Click += async (_, __) =>
            {
                if (ShowShopView is not null)
                    await ShowShopView(CancellationToken.None);
            };

            btnContainer.Click += async (_, __) =>
            {
                if (ShowContainerView is not null)
                    await ShowContainerView(CancellationToken.None);
            };

            topPanel.MouseDown += (_, e) => ProgramMouseDown?.Invoke(this, e);
            topPanel.MouseMove += (_, e) => ProgramMouseMove?.Invoke(this, e);
            topPanel.MouseUp += (_, e) => ProgramMouseUp?.Invoke(this, e);

            btnCloseProgram.Click += async (_, __) =>
            {
                Environment.Exit(0);
            };

            btnProgramScale.Click += async (_, __) =>
            {
                this.WindowState = this.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
            };

            btnProgramMinimalize.Click += (_, __) =>
            {
                this.WindowState = FormWindowState.Minimized;
            };

        }

        public event Func<CancellationToken, Task>? ShowEmployeeView;
        public event Func<CancellationToken, Task>? ShowAvailabilityView;
        public event Func<CancellationToken, Task>? ShowShopView;
        public event Func<CancellationToken, Task>? ShowContainerView;
        public event EventHandler<MouseEventArgs>? ProgramMouseUp;
        public event EventHandler<MouseEventArgs>? ProgramMouseDown;
        public event EventHandler<MouseEventArgs>? ProgramMouseMove;

        public void BeginWindowDrag()
        {
            ReleaseCapture();
            SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
        }
    }
}
