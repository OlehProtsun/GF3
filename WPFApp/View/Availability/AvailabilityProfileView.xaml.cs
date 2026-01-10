using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WPFApp.ViewModel.Availability;

namespace WPFApp.View.Availability
{
    /// <summary>
    /// Interaction logic for AvailabilityProfileView.xaml
    /// </summary>
    public partial class AvailabilityProfileView : UserControl
    {
        private AvailabilityProfileViewModel? _vm;

        public AvailabilityProfileView()
        {
            InitializeComponent();
            DataContextChanged += AvailabilityProfileView_DataContextChanged;
        }

        private void AvailabilityProfileView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_vm != null)
                _vm.MatrixChanged -= VmOnMatrixChanged;

            _vm = DataContext as AvailabilityProfileViewModel;
            if (_vm != null)
            {
                _vm.MatrixChanged += VmOnMatrixChanged;
                BuildMatrixColumns(_vm.ProfileAvailabilityMonths.Table, dataGridAvailabilityMonthProfile);
            }
        }

        private void VmOnMatrixChanged(object? sender, EventArgs e)
        {
            if (_vm is null) return;
            BuildMatrixColumns(_vm.ProfileAvailabilityMonths.Table, dataGridAvailabilityMonthProfile);
        }

        private static void BuildMatrixColumns(DataTable? table, DataGrid grid)
        {
            if (table is null) return;

            grid.AutoGenerateColumns = false;
            grid.Columns.Clear();

            foreach (DataColumn column in table.Columns)
            {
                var header = string.IsNullOrWhiteSpace(column.Caption) ? column.ColumnName : column.Caption;
                var col = new DataGridTextColumn
                {
                    Header = header,
                    Binding = new Binding($"[{column.ColumnName}]"),
                    IsReadOnly = true
                };

                if (column.ColumnName == "DayOfMonth")
                    col.Width = 60;

                grid.Columns.Add(col);
            }
        }
    }
}
