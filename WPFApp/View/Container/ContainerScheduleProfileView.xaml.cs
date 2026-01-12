using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WPFApp.ViewModel.Container;

namespace WPFApp.View.Container
{
    /// <summary>
    /// Interaction logic for ContainerScheduleProfileView.xaml
    /// </summary>
    public partial class ContainerScheduleProfileView : UserControl
    {
        private ContainerScheduleProfileViewModel? _vm;

        public ContainerScheduleProfileView()
        {
            InitializeComponent();
            DataContextChanged += ContainerScheduleProfileView_DataContextChanged;
        }

        private void ContainerScheduleProfileView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_vm != null)
                _vm.MatrixChanged -= VmOnMatrixChanged;

            _vm = DataContext as ContainerScheduleProfileViewModel;
            if (_vm != null)
            {
                _vm.MatrixChanged += VmOnMatrixChanged;
                BuildMatrixColumns(_vm.ScheduleMatrix.Table, dataGridScheduleProfile);
            }
        }

        private void VmOnMatrixChanged(object? sender, System.EventArgs e)
        {
            if (_vm is null) return;
            BuildMatrixColumns(_vm.ScheduleMatrix.Table, dataGridScheduleProfile);
        }

        private static void BuildMatrixColumns(DataTable? table, DataGrid grid)
        {
            if (table is null) return;

            grid.AutoGenerateColumns = false;
            grid.Columns.Clear();

            grid.FrozenColumnCount = 1;

            foreach (DataColumn column in table.Columns)
            {
                var header = string.IsNullOrWhiteSpace(column.Caption) ? column.ColumnName : column.Caption;

                var binding = new Binding($"[{column.ColumnName}]")
                {
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    ValidatesOnDataErrors = true,
                    NotifyOnValidationError = true
                };

                var col = new DataGridTextColumn
                {
                    Header = header,
                    Binding = binding,
                    IsReadOnly = true
                };

                if (column.ColumnName == ContainerScheduleEditViewModel.DayColumnName)
                {
                    col.Width = 70;
                }
                else if (column.ColumnName == ContainerScheduleEditViewModel.ConflictColumnName)
                {
                    col.Width = 80;
                }
                else
                {
                    col.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                }

                var tbStyle = (Style)Application.Current.FindResource("MatrixCellTextBlockStyle");
                col.ElementStyle = tbStyle;

                grid.Columns.Add(col);
            }
        }
    }
}
