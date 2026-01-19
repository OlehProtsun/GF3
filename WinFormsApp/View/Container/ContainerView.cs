using System.Reflection;
using System.Windows.Forms;
using WinFormsApp.View.Shared;

namespace WinFormsApp.View.Container
{
    public partial class ContainerView : BusyForm, IContainerView
    {
        public ContainerView()
        {
            InitializeComponent();

            this.DoubleBuffered = true;

            // Для panel1 (бо в Panel DoubleBuffered protected)
            EnableDoubleBuffer(panel1);

            WireAutoClearValidation();

            _containerErrorMap = CreateContainerErrorMap();
            _scheduleErrorMap = CreateScheduleErrorMap();

            ConfigureContainerGrid();
            ConfigureScheduleGrid();
            ConfigureSlotGrid();
            InitializeScheduleStyleMenu();
            AssociateAndRaiseEvents();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try { _lifetimeCts.Cancel(); } catch { /* ignore */ }
            _lifetimeCts.Dispose();
            CancelAndDispose(ref _scheduleMatrixBuildCts);
            CancelAndDispose(ref _scheduleProfileBuildCts);
            CancelAndDispose(ref _availabilityPreviewBuildCts);

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
