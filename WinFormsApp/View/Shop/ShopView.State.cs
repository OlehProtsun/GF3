using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;
using WinFormsApp.ViewModel;
using WinFormsApp.View.Shared;

namespace WinFormsApp.View.Shop
{
    public partial class ShopView : BusyForm, IShopView
    {
        private readonly CancellationTokenSource _lifetimeCts = new();
        private bool _gridConfigured;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public ShopViewModel Mode { get; set; } = ShopViewModel.List;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public ShopViewModel CancelTarget { get; set; } = ShopViewModel.List;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int Id
        {
            get => decimal.ToInt32(numberId.Value);
            set
            {
                var v = (decimal)value;
                if (v < numberId.Minimum) v = numberId.Minimum;
                if (v > numberId.Maximum) v = numberId.Maximum;
                numberId.Value = v;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string Name
        {
            get => inputFirstName.Text;
            set => inputFirstName.Text = value ?? string.Empty;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string Address
        {
            get => inputLastName.Text;
            set => inputLastName.Text = value ?? string.Empty;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string? Description
        {
            get => string.IsNullOrWhiteSpace(inputEmail.Text) ? null : inputEmail.Text;
            set => inputEmail.Text = value ?? string.Empty;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string SearchValue
        {
            get => inputSearch.Text;
            set => inputSearch.Text = value ?? string.Empty;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool IsEdit { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool IsSuccessful { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string Message { get; set; } = string.Empty;

        public event Func<CancellationToken, Task>? SearchEvent;
        public event Func<CancellationToken, Task>? AddEvent;
        public event Func<CancellationToken, Task>? EditEvent;
        public event Func<CancellationToken, Task>? DeleteEvent;
        public event Func<CancellationToken, Task>? SaveEvent;
        public event Func<CancellationToken, Task>? CancelEvent;
        public event Func<CancellationToken, Task>? OpenProfileEvent;

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try { _lifetimeCts.Cancel(); } catch { /* ignore */ }
            _lifetimeCts.Dispose();
            base.OnFormClosed(e);
        }
    }
}
