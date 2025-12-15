using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp.ViewModel;

namespace WinFormsApp.View.Employee
{
    public partial class EmployeeView : Form, IEmployeeView
    {
        private bool isEdit;
        private bool isSuccessful;
        private string message;

        public EmployeeView()
        {
            InitializeComponent();
            ConfigureGrid();
            AssociateAndRaiseViewEvents();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public EmployeeViewModel Mode { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public EmployeeViewModel CancelTarget { get; set; } = EmployeeViewModel.List;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int Id
        {
            get => (int)numberId.Value;
            set => numberId.Value = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string FirstName
        {
            get => inputFirstName.Text;
            set => inputFirstName.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public string LastName
        {
            get => inputLastName.Text;
            set => inputLastName.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public string? Email
        {
            get => inputEmail.Text;
            set => inputEmail.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public string? Phone
        {
            get => inputPhone.Text;
            set => inputPhone.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string SearchValue { get => inputSearch.Text; set => inputSearch.Text = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool IsEdit { get => isEdit; set => isEdit = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool IsSuccessful { get => isSuccessful; set => isSuccessful = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string Message { get => message; set => message = value; }

        public event Func<CancellationToken, Task>? SearchEvent;
        public event Func<CancellationToken, Task>? AddEvent;
        public event Func<CancellationToken, Task>? EditEvent;
        public event Func<CancellationToken, Task>? DeleteEvent;
        public event Func<CancellationToken, Task>? SaveEvent;
        public event Func<CancellationToken, Task>? CancelEvent;
        public event Func<CancellationToken, Task>? OpenProfileEvent;

        public void SetEmployeeListBindingSource(BindingSource employeeList)
        {
            dataGrid.AutoGenerateColumns = false;
            dataGrid.DataSource = employeeList;
        }

        private void AssociateAndRaiseViewEvents()
        {
            btnSearch.Click += async (_, __) => { if (SearchEvent != null) await SearchEvent(CancellationToken.None); };
            btnAdd.Click += async (_, __) => { if (AddEvent != null) await AddEvent(CancellationToken.None); };
            btnEdit.Click += async (_, __) => { if (EditEvent != null) await EditEvent(CancellationToken.None); };
            btnDelete.Click += async (_, __) => { if (DeleteEvent != null) await DeleteEvent(CancellationToken.None); };
            btnSave.Click += async (_, __) => { if (SaveEvent != null) await SaveEvent(CancellationToken.None); };
            btnCancel.Click += async (_, __) =>
            {
                if (CancelEvent != null)
                    await CancelEvent(CancellationToken.None);
            };

            btnCancelProfile.Click += async (_, __) =>
            {
                if (CancelEvent != null)
                    await CancelEvent(CancellationToken.None);
            };

            inputSearch.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && SearchEvent != null)
                    await SearchEvent(CancellationToken.None);
            };

            dataGrid.CellDoubleClick += async (_, __) =>
            {
                if (OpenProfileEvent is not null)
                    await OpenProfileEvent(CancellationToken.None);
            };

            btnBackToEmployeeList.Click += async (_, __) =>
            {
                if (CancelEvent != null)
                    await CancelEvent(CancellationToken.None);
            };

            btnBackToEmployeeListFromProfile.Click += async (_, __) =>
            {
                if (CancelEvent != null)
                    await CancelEvent(CancellationToken.None);
            };
        }

        public void SwitchToEditMode()
        {
            tabControl.SelectedTab = tabEditAdnCreate;
            Mode = EmployeeViewModel.Edit;
        }

        public void SwitchToListMode()
        {
            tabControl.SelectedTab = tabList;
            Mode = EmployeeViewModel.List;
        }

        public void ClearInputs()
        {
            Id = 0;
            FirstName = string.Empty;
            LastName = string.Empty;
            Email = string.Empty;
            Phone = string.Empty;
        }

        public void ClearValidationErrors()
        {
            if (errorProvider != null)
            {
                errorProvider.SetError(inputFirstName, "");
                errorProvider.SetError(inputLastName, "");
                errorProvider.SetError(inputEmail, "");
                errorProvider.SetError(inputPhone, "");
            }
        }

        public void SetValidationErrors(IReadOnlyDictionary<string, string> errors)
        {
            ClearValidationErrors();
            foreach (var kv in errors)
            {
                switch (kv.Key)
                {
                    case nameof(FirstName): errorProvider.SetError(inputFirstName, kv.Value); break;
                    case nameof(LastName): errorProvider.SetError(inputLastName, kv.Value); break;
                    case nameof(Email): errorProvider.SetError(inputEmail, kv.Value); break;
                    case nameof(Phone): errorProvider.SetError(inputPhone, kv.Value); break;
                }
            }
        }

        public void ShowInfo(string text)
        {
            MessageDialog.Icon = Guna.UI2.WinForms.MessageDialogIcon.Information;
            MessageDialog.Buttons = Guna.UI2.WinForms.MessageDialogButtons.OK;
            MessageDialog.Text = text;
            MessageDialog.Show();
        }

        public void ShowError(string text)
        {
            MessageDialog.Icon = Guna.UI2.WinForms.MessageDialogIcon.Error;
            MessageDialog.Buttons = Guna.UI2.WinForms.MessageDialogButtons.OK;
            MessageDialog.Text = text;
            MessageDialog.Show();
        }

        public bool Confirm(string text, string? caption = null)
        {
            if (!string.IsNullOrWhiteSpace(caption))
                MessageDialog.Caption = caption;

            MessageDialog.Icon = Guna.UI2.WinForms.MessageDialogIcon.Question;
            MessageDialog.Buttons = Guna.UI2.WinForms.MessageDialogButtons.YesNo;
            MessageDialog.Text = text;

            var result = MessageDialog.Show(); // DialogResult.Yes / No
            return result == DialogResult.Yes;
        }

        private void ConfigureGrid()
        {
            dataGrid.AutoGenerateColumns = false;
            dataGrid.Columns.Clear();

            dataGrid.ReadOnly = true;
            dataGrid.RowHeadersVisible = false;
            dataGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGrid.MultiSelect = false;
            dataGrid.AllowUserToAddRows = false;
            dataGrid.AllowUserToDeleteRows = false;
            dataGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dataGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colFirstName",
                HeaderText = "First name",
                DataPropertyName = "FirstName",
                FillWeight = 50
            });

            dataGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colLastName",
                HeaderText = "Last name",
                DataPropertyName = "LastName",
                FillWeight = 50
            });

            dataGrid.RowTemplate.DividerHeight = 6;      // скільки пікселів відступу під кожним рядком
            dataGrid.RowTemplate.Height = 36;            // сам рядок трохи вищий
            dataGrid.ThemeStyle.RowsStyle.Height = 36;

            // Трохи внутрішнього паддінгу в комірках
            dataGrid.DefaultCellStyle.Padding = new Padding(8, 6, 8, 6);
            dataGrid.ColumnHeadersHeight = 36;
        }

        public void SwitchToProfileMode()
        {
            tabControl.SelectedTab = tabProfile;
            Mode = EmployeeViewModel.Profile;
        }

        public void SetProfile(EmployeeModel m)
        {
            labelName.Text = $"{m.FirstName} {m.LastName}";
            labelEmail.Text = string.IsNullOrWhiteSpace(m.Email) ? "—" : m.Email;
            labelPhone.Text = string.IsNullOrWhiteSpace(m.Phone) ? "—" : m.Phone;
            labelId.Text = m.Id.ToString();
        }

        private void dataGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
