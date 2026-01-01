using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace WinFormsApp.View.Employee
{
    public partial class EmployeeView
    {
        private static void EnableDoubleBuffering(DataGridView grid)
        {
            typeof(DataGridView).InvokeMember(
                "DoubleBuffered",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                null,
                grid,
                new object[] { true });
        }

        private void ConfigureGrid()
        {
            if (_gridConfigured) return;
            _gridConfigured = true;

            dataGrid.SuspendLayout();
            try
            {
                EnableDoubleBuffering(dataGrid);

                dataGrid.AutoGenerateColumns = false;
                dataGrid.Columns.Clear();

                dataGrid.ReadOnly = true;
                dataGrid.ThemeStyle.ReadOnly = true;

                dataGrid.RowHeadersVisible = false;
                dataGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dataGrid.MultiSelect = false;

                dataGrid.AllowUserToAddRows = false;
                dataGrid.AllowUserToDeleteRows = false;

                dataGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dataGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
                dataGrid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;

                dataGrid.ColumnHeadersHeight = 36;
                dataGrid.ThemeStyle.HeaderStyle.Height = 36;
                dataGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

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

                dataGrid.RowTemplate.DividerHeight = 6;
                dataGrid.RowTemplate.Height = 36;
                dataGrid.ThemeStyle.RowsStyle.Height = 36;

                dataGrid.DefaultCellStyle.Padding = new System.Windows.Forms.Padding(8, 6, 8, 6);
            }
            finally
            {
                dataGrid.ResumeLayout();
            }
        }
    }
}
