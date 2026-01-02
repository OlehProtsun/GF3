using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Reflection;
using WinFormsApp.View.Shared;
using WinFormsApp.ViewModel;

namespace WinFormsApp.View.Container
{
    public partial class ContainerView : Form, IContainerView
    {
        public ContainerView()
        {
            InitializeComponent();

            this.DoubleBuffered = true;

            _scheduleInfoExpandedHeight = guna2GroupBox5.Height; // зараз 626 :contentReference[oaicite:4]{index=4}
            ApplyScheduleInfoState(expanded: true);


            // Для panel1 (бо в Panel DoubleBuffered protected)
            EnableDoubleBuffer(panel1);

            _busyController = new BusyOverlayController(this);
            WireAutoClearValidation();

            _containerErrorMap = CreateContainerErrorMap();
            _scheduleErrorMap = CreateScheduleErrorMap();

            ConfigureContainerGrid();
            ConfigureScheduleGrid();
            ConfigureSlotGrid();
            AssociateAndRaiseEvents();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try { _lifetimeCts.Cancel(); } catch { /* ignore */ }
            _lifetimeCts.Dispose();

            _busyController.Dispose();
            _gridVPen.Dispose();
            _gridHPen.Dispose();
            _conflictPen.Dispose();

            base.OnFormClosed(e);
        }

        private static void EnableDoubleBuffer(Control control)
        {
            typeof(Control)
                .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(control, true, null);

            // необов'язково, але інколи допомагає:
            control.Invalidate();
        }
    }
}
