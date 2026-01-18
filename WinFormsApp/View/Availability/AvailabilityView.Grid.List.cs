using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace WinFormsApp.View.Availability
{
    public partial class AvailabilityView
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


        public void SetListBindingSource(BindingSource availabilityList)
        {
            dataGrid.AutoGenerateColumns = false;
            dataGrid.DataSource = availabilityList;
        }

        private void ConfigureGrid()
        {
            ConfigureMonthGrid(dataGrid);
        }

        private void ConfigureMonthGrid(DataGridView grid)
        {
            grid.SuspendLayout();
            try
            {
                EnableDoubleBuffering(grid);

                grid.AutoGenerateColumns = false;
                grid.Columns.Clear();

                grid.ReadOnly = true;
                grid.RowHeadersVisible = false;
                grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                grid.MultiSelect = false;
                grid.AllowUserToAddRows = false;
                grid.AllowUserToDeleteRows = false;
                grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "colAvailabilityMonthName",
                    HeaderText = "Availability Name",
                    DataPropertyName = nameof(AvailabilityGroupModel.Name),
                    FillWeight = 100
                });

                grid.RowTemplate.DividerHeight = 6;
                grid.RowTemplate.Height = 36;

                grid.DefaultCellStyle.Padding = new Padding(8, 6, 8, 6);
                grid.ColumnHeadersHeight = 36;
            }
            finally
            {
                grid.ResumeLayout();
            }
        }

    }
}
