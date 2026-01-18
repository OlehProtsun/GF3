using System.Windows.Forms;

namespace WinFormsApp.View.Availability
{
    public partial class AvailabilityView
    {
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try { _lifetimeCts.Cancel(); } catch { /* ignore */ }
            _lifetimeCts.Dispose();

            _matrixVPen.Dispose();
            _matrixHPen.Dispose();

            base.OnFormClosed(e);
        }
    }
}
