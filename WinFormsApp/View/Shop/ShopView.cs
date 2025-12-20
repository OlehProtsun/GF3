using DataAccessLayer.Models;
using System.ComponentModel;
using System.Windows.Forms;
using WinFormsApp.ViewModel;

namespace WinFormsApp.View.Shop
{
    public partial class ShopView : Form, IShopView
    {
        private bool isEdit;
        private bool isSuccessful;
        private string message = string.Empty;

        public ShopView()
        {
            InitializeComponent();
            ConfigureGrid();
            AssociateAndRaiseViewEvents();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public ShopViewModel Mode { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public ShopViewModel CancelTarget { get; set; } = ShopViewModel.List;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int ShopId
        {
            get => (int)numberShopId.Value;
            set => numberShopId.Value = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string ShopName
        {
            get => inputShopName.Text;
            set => inputShopName.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string? ShopDescription
        {
            get => inputShopDescription.Text;
            set => inputShopDescription.Text = value;
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

        public void SetListBindingSource(BindingSource shops)
        {
            dataGrid.AutoGenerateColumns = false;
            dataGrid.DataSource = shops;
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

            btnBackToShopList.Click += async (_, __) =>
            {
                if (CancelEvent != null)
                    await CancelEvent(CancellationToken.None);
            };

            btnCancelProfile.Click += async (_, __) =>
            {
                if (CancelEvent != null)
                    await CancelEvent(CancellationToken.None);
            };

            btnBackToShopListFromProfile.Click += async (_, __) =>
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
        }

        public void SwitchToEditMode()
        {
            tabControl.SelectedTab = tabEditAdnCreate;
            Mode = ShopViewModel.Edit;
        }

        public void SwitchToListMode()
        {
            tabControl.SelectedTab = tabList;
            Mode = ShopViewModel.List;
        }

        public void ClearInputs()
        {
            ShopId = 0;
            ShopName = string.Empty;
            ShopDescription = string.Empty;
        }

        public void ClearValidationErrors()
        {
            if (errorProvider != null)
            {
                errorProvider.SetError(inputShopName, "");
                errorProvider.SetError(inputShopDescription, "");
            }
        }

        public void SetValidationErrors(IReadOnlyDictionary<string, string> errors)
        {
            ClearValidationErrors();
            foreach (var kv in errors)
            {
                switch (kv.Key)
                {
                    case nameof(ShopName): errorProvider.SetError(inputShopName, kv.Value); break;
                    case nameof(ShopDescription): errorProvider.SetError(inputShopDescription, kv.Value); break;
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

            var result = MessageDialog.Show();
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
                Name = "colName",
                HeaderText = "Name",
                DataPropertyName = "Name",
                FillWeight = 50
            });

            dataGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDescription",
                HeaderText = "Description",
                DataPropertyName = "Description",
                FillWeight = 50
            });

            dataGrid.RowTemplate.DividerHeight = 6;
            dataGrid.RowTemplate.Height = 36;
            dataGrid.ThemeStyle.RowsStyle.Height = 36;

            dataGrid.DefaultCellStyle.Padding = new Padding(8, 6, 8, 6);
            dataGrid.ColumnHeadersHeight = 36;
        }

        public void SwitchToProfileMode()
        {
            tabControl.SelectedTab = tabProfile;
            Mode = ShopViewModel.Profile;
        }

        public void SetProfile(ShopModel m)
        {
            labelName.Text = m.Name;
            labelDescription.Text = string.IsNullOrWhiteSpace(m.Description) ? "—" : m.Description;
            labelId.Text = m.Id.ToString();
        }
    }
}
