using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace WinFormsApp.View.Availability
{
    public partial class AvailabilityView
    {
        private void ForceProfileGridStyle()
        {
            // важливі прапорці для DataGridView / Guna
            dataGridAvailabilityMonthProfile.EnableHeadersVisualStyles = false;

            // базовий стиль як у тебе
            ApplyAvailabilityGridLook(dataGridAvailabilityMonthProfile);

            // readonly + editmode для Guna
            dataGridAvailabilityMonthProfile.ReadOnly = true;
            dataGridAvailabilityMonthProfile.ThemeStyle.ReadOnly = true;
            dataGridAvailabilityMonthProfile.EditMode = DataGridViewEditMode.EditProgrammatically;

            // фіксимо типові скидання після DataSource
            dataGridAvailabilityMonthProfile.RowHeadersVisible = false;
            dataGridAvailabilityMonthProfile.AllowUserToAddRows = false;
            dataGridAvailabilityMonthProfile.AllowUserToDeleteRows = false;
            dataGridAvailabilityMonthProfile.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridAvailabilityMonthProfile.MultiSelect = false;
            dataGridAvailabilityMonthProfile.CellBorderStyle = DataGridViewCellBorderStyle.None;

            // інколи Guna знову ставить автохедер-стилі — дублюємо
            dataGridAvailabilityMonthProfile.ColumnHeadersHeight = 36;
            dataGridAvailabilityMonthProfile.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            dataGridAvailabilityMonthProfile.Refresh();
        }

        public void SetProfile(
            AvailabilityGroupModel group,
            List<AvailabilityGroupMemberModel> members,
            List<AvailabilityGroupDayModel> days)
        {
            lblAvailabilityName.Text = group.Name;
            lblAvailabilityId.Text = $"{group.Id}";
            lblAvailabilityMonthYear.Text = $"{group.Month}-{group.Year}";
            dataGridAvailabilityMonthProfile.CellPainting -= MatrixGrid_CellPainting;
            dataGridAvailabilityMonthProfile.DataBindingComplete -= ProfileGrid_DataBindingComplete;
            dataGridAvailabilityMonthProfile.DataBindingComplete += ProfileGrid_DataBindingComplete;
            dataGridAvailabilityMonthProfile.DataSource = null;
            dataGridAvailabilityMonthProfile.Columns.Clear();
            dataGridAvailabilityMonthProfile.Rows.Clear();
            _profileGroupTable.Clear();
            _profileGroupTable.Columns.Clear();

            _profileGroupTable.Columns.Add(DayCol, typeof(int));

            var memberIdToCol = new Dictionary<int, string>();

            foreach (var m in members)
            {
                var colName = $"emp_{m.EmployeeId}";
                memberIdToCol[m.Id] = colName;

                var header = m.Employee is null
                    ? $"Employee #{m.EmployeeId}"
                    : $"{m.Employee.FirstName} {m.Employee.LastName}";

                _profileGroupTable.Columns.Add(new DataColumn(colName, typeof(string)) { Caption = header });
            }

            var daysMap = days.ToDictionary(
                d => (d.AvailabilityGroupMemberId, d.DayOfMonth),
                d => d.Kind switch
                {
                    DataAccessLayer.Models.Enums.AvailabilityKind.ANY => "+",
                    DataAccessLayer.Models.Enums.AvailabilityKind.NONE => "-",
                    DataAccessLayer.Models.Enums.AvailabilityKind.INT => d.IntervalStr ?? "",
                    _ => ""
                });

            int dim = DateTime.DaysInMonth(group.Year, group.Month);
            for (int day = 1; day <= dim; day++)
            {
                var row = _profileGroupTable.NewRow();
                row[DayCol] = day;

                foreach (var m in members)
                {
                    var colName = memberIdToCol[m.Id];
                    row[colName] = daysMap.TryGetValue((m.Id, day), out var v) ? v : "-";
                }

                _profileGroupTable.Rows.Add(row);
            }

            dataGridAvailabilityMonthProfile.AutoGenerateColumns = true;
            dataGridAvailabilityMonthProfile.DataSource = _profileGroupTable;
            dataGridAvailabilityMonthProfile.CellPainting += MatrixGrid_CellPainting;
        }

        private void ProfileGrid_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            DataGridAvailabilityDays_DataBindingComplete(sender, e);
            ForceProfileGridStyle();
        }
    }
}
