using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using WinFormsApp.ViewModel;

namespace WinFormsApp.View.Container
{
    public partial class ContainerView : Form, IContainerView
    {
        public ContainerView()
        {
            InitializeComponent();
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

            _gridVPen.Dispose();
            _gridHPen.Dispose();
            _conflictPen.Dispose();

            base.OnFormClosed(e);
        }
    }
}
