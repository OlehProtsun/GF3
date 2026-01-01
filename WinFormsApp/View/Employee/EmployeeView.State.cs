using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using WinFormsApp.ViewModel;

namespace WinFormsApp.View.Employee
{
    public partial class EmployeeView : Form, IEmployeeView
    {
        private readonly CancellationTokenSource _lifetimeCts = new();
        private bool _gridConfigured;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public EmployeeViewModel Mode { get; set; } = EmployeeViewModel.List;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public EmployeeViewModel CancelTarget { get; set; } = EmployeeViewModel.List;

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
        public string FirstName
        {
            get => inputFirstName.Text;
            set => inputFirstName.Text = value ?? string.Empty;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string LastName
        {
            get => inputLastName.Text;
            set => inputLastName.Text = value ?? string.Empty;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string? Email
        {
            get => string.IsNullOrWhiteSpace(inputEmail.Text) ? null : inputEmail.Text;
            set => inputEmail.Text = value ?? string.Empty;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string? Phone
        {
            get => string.IsNullOrWhiteSpace(inputPhone.Text) ? null : inputPhone.Text;
            set => inputPhone.Text = value ?? string.Empty;
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
            _busyController.Dispose();
            base.OnFormClosed(e);
        }
    }
}
