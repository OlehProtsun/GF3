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
    /// Interaction logic for AvailabilityEditView.xaml
    /// </summary>
    public partial class AvailabilityEditView : UserControl
    {
        private AvailabilityEditViewModel? _vm;

        public AvailabilityEditView()
        {
            InitializeComponent();
            DataContextChanged += AvailabilityEditView_DataContextChanged;

            dataGridAvailabilityDays.PreviewKeyDown += DataGridAvailabilityDays_PreviewKeyDown;
            dataGridBinds.AutoGeneratingColumn += DataGridBinds_AutoGeneratingColumn;
            dataGridBinds.PreviewKeyDown += DataGridBinds_PreviewKeyDown;
            dataGridBinds.RowEditEnding += DataGridBinds_RowEditEnding;
            textBoxSearchValueFromAvailabilityEdit.KeyDown += TextBoxSearchValueFromAvailabilityEdit_KeyDown;
        }

        private void AvailabilityEditView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_vm != null)
                _vm.MatrixChanged -= VmOnMatrixChanged;

            _vm = DataContext as AvailabilityEditViewModel;

            if (_vm != null)
            {
                _vm.MatrixChanged += VmOnMatrixChanged;
                BuildMatrixColumns(_vm.AvailabilityDays.Table, dataGridAvailabilityDays);
            }
        }

        private void VmOnMatrixChanged(object? sender, EventArgs e)
        {
            if (_vm is null) return;
            BuildMatrixColumns(_vm.AvailabilityDays.Table, dataGridAvailabilityDays);
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
                    IsReadOnly = column.ReadOnly
                };

                if (column.ColumnName == "DayOfMonth")
                {
                    col.Width = 60;
                    col.IsReadOnly = true;
                }

                grid.Columns.Add(col);
            }
        }

        private void TextBoxSearchValueFromAvailabilityEdit_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (DataContext is AvailabilityEditViewModel vm && vm.SearchEmployeeCommand.CanExecute(null))
                vm.SearchEmployeeCommand.Execute(null);
        }

        private void DataGridBinds_RowEditEnding(object? sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;
            if (_vm is null) return;

            var row = e.Row.Item as BindRow;
            _ = _vm.UpsertBindAsync(row);
        }

        private void DataGridBinds_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == nameof(BindRow.Key))
                e.Column.IsReadOnly = true;
        }

        private void DataGridBinds_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_vm is null) return;
            if (dataGridBinds.CurrentColumn is not DataGridBoundColumn boundColumn) return;

            var binding = boundColumn.Binding as Binding;
            if (binding?.Path?.Path != nameof(BindRow.Key)) return;

            var keyText = _vm.FormatKeyGesture(e.Key == Key.System ? e.SystemKey : e.Key, Keyboard.Modifiers);
            if (string.IsNullOrWhiteSpace(keyText)) return;

            if (dataGridBinds.CurrentItem is BindRow row)
                row.Key = keyText;

            e.Handled = true;
        }

        private void DataGridAvailabilityDays_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_vm is null) return;
            if (dataGridAvailabilityDays.CurrentColumn is not DataGridBoundColumn boundColumn) return;

            var binding = boundColumn.Binding as Binding;
            var columnName = binding?.Path?.Path?.Trim('[', ']') ?? string.Empty;
            if (string.IsNullOrWhiteSpace(columnName)) return;

            var keyText = _vm.FormatKeyGesture(e.Key == Key.System ? e.SystemKey : e.Key, Keyboard.Modifiers);
            if (string.IsNullOrWhiteSpace(keyText)) return;

            if (!_vm.TryGetBindValue(keyText, out var bindValue)) return;

            var rowIndex = dataGridAvailabilityDays.Items.IndexOf(dataGridAvailabilityDays.CurrentItem);
            if (!_vm.TryApplyBindToCell(columnName, rowIndex, bindValue, out var nextRowIndex))
                return;

            if (nextRowIndex.HasValue && nextRowIndex.Value < dataGridAvailabilityDays.Items.Count)
            {
                dataGridAvailabilityDays.CurrentCell = new DataGridCellInfo(dataGridAvailabilityDays.Items[nextRowIndex.Value], boundColumn);
                dataGridAvailabilityDays.BeginEdit();
            }

            e.Handled = true;
        }
    }
}
