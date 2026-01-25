using System.Data;
using System.Text.RegularExpressions;
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

        private static readonly Regex _digitsOnly = new Regex("^[0-9]+$");
        private static readonly Regex _intervalNoSpace = new Regex(
            @"^\s*(?<h1>[01]?\d|2[0-3]):(?<m1>[0-5]\d)-(?<h2>[01]?\d|2[0-3]):(?<m2>[0-5]\d)\s*$",
            RegexOptions.Compiled);



        public AvailabilityEditView()
        {
            InitializeComponent();
            DataContextChanged += AvailabilityEditView_DataContextChanged;
            Loaded += AvailabilityEditView_Loaded;
            Unloaded += AvailabilityEditView_Unloaded;

            dataGridAvailabilityDays.PreviewKeyDown += DataGridAvailabilityDays_PreviewKeyDown;
            dataGridAvailabilityDays.CellEditEnding += DataGridAvailabilityDays_CellEditEnding;
            dataGridBinds.AutoGeneratingColumn += DataGridBinds_AutoGeneratingColumn;
            dataGridBinds.PreviewKeyDown += DataGridBinds_PreviewKeyDown;
            dataGridBinds.RowEditEnding += DataGridBinds_RowEditEnding;
            textBoxSearchValueFromAvailabilityEdit.KeyDown += TextBoxSearchValueFromAvailabilityEdit_KeyDown;
        }

        private void AvailabilityEditView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AttachViewModel(DataContext as AvailabilityEditViewModel);
        }

        private void AvailabilityEditView_Loaded(object sender, RoutedEventArgs e)
        {
            AttachViewModel(DataContext as AvailabilityEditViewModel);
        }

        private void AvailabilityEditView_Unloaded(object sender, RoutedEventArgs e)
        {
            DetachViewModel();
        }

        private void AttachViewModel(AvailabilityEditViewModel? viewModel)
        {
            if (_vm != null)
                _vm.MatrixChanged -= VmOnMatrixChanged;

            _vm = viewModel;

            if (_vm != null)
            {
                _vm.MatrixChanged += VmOnMatrixChanged;
                BuildMatrixColumns(_vm.AvailabilityDays.Table, dataGridAvailabilityDays);
            }
        }

        private void DetachViewModel()
        {
            if (_vm == null) return;
            _vm.MatrixChanged -= VmOnMatrixChanged;
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

            // Day "заморожена"
            grid.FrozenColumnCount = 1;

            foreach (DataColumn column in table.Columns)
            {
                var header = string.IsNullOrWhiteSpace(column.Caption) ? column.ColumnName : column.Caption;

                var b = new Binding($"[{column.ColumnName}]")
                {
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    ValidatesOnDataErrors = true,
                    NotifyOnValidationError = true
                };

                var col = new DataGridTextColumn
                {
                    Header = header,
                    Binding = b,
                    IsReadOnly = column.ReadOnly
                };

                // Day column
                if (column.ColumnName == "DayOfMonth")
                {
                    col.Width = 60;
                    col.IsReadOnly = true;
                }
                else
                {
                    col.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                }

                var tbStyle = (Style)Application.Current.FindResource("MatrixCellTextBlockStyle");
                col.ElementStyle = tbStyle;

                var editStyle = (Style)Application.Current.FindResource("MatrixCellTextBoxStyle");
                col.EditingElementStyle = editStyle;

                grid.Columns.Add(col);
            }
        }

        private void TextBoxSearchValueFromAvailabilityEdit_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (DataContext is AvailabilityEditViewModel vm && vm.SearchEmployeeCommand.CanExecute(null))
                vm.SearchEmployeeCommand.Execute(null);
        }

        private async void DataGridBinds_RowEditEnding(object? sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;
            if (_vm is null) return;

            var row = e.Row.Item as BindRow;
            await _vm.UpsertBindAsync(row);
        }

        private void DataGridBinds_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == nameof(BindRow.Key))
                e.Column.IsReadOnly = true;
        }

        private void DataGridBinds_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_vm is null) return;

            // ENTER: комітимо клітинку+рядок (RowEditEnding викличе Upsert)
            if (e.Key == Key.Enter)
            {
                dataGridBinds.CommitEdit(DataGridEditingUnit.Cell, true);
                dataGridBinds.CommitEdit(DataGridEditingUnit.Row, true);
                e.Handled = true;
                return;
            }

            // Якщо зараз редагуємо TextBox — даємо вводити вручну
            if (IsCurrentCellEditing(dataGridBinds))
                return;

            if (dataGridBinds.CurrentColumn is not DataGridBoundColumn boundColumn) return;
            var binding = boundColumn.Binding as Binding;
            if (binding?.Path?.Path != nameof(BindRow.Key)) return;

            // Якщо хочеш "запис хоткея" тільки з модифікаторами:
            if (Keyboard.Modifiers == ModifierKeys.None)
                return;

            // ігноруємо навігацію
            switch (e.Key)
            {
                case Key.Up:
                case Key.Down:
                case Key.Left:
                case Key.Right:
                case Key.Tab:
                case Key.Escape:
                    return;
            }

            var key = (e.Key == Key.System) ? e.SystemKey : e.Key;
            var keyText = _vm.FormatKeyGesture(key, Keyboard.Modifiers);
            if (string.IsNullOrWhiteSpace(keyText)) return;

            if (dataGridBinds.CurrentItem is BindRow row)
            {
                row.Key = keyText;
                dataGridBinds.CommitEdit(DataGridEditingUnit.Cell, true);
            }

            e.Handled = true;
        }


        private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T typed) return typed;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        private bool IsCurrentCellEditing(DataGrid grid)
        {
            if (grid.CurrentItem == null || grid.CurrentColumn == null) return false;

            var content = grid.CurrentColumn.GetCellContent(grid.CurrentItem);
            if (content == null) return false;

            var cell = FindParent<DataGridCell>(content);
            return cell?.IsEditing == true;
        }

        private void DataGridAvailabilityDays_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_vm is null) return;
            if (dataGridAvailabilityDays.CurrentColumn is not DataGridBoundColumn boundColumn) return;

            var binding = boundColumn.Binding as Binding;
            var columnName = binding?.Path?.Path?.Trim('[', ']') ?? string.Empty;
            if (string.IsNullOrWhiteSpace(columnName)) return;
            if (columnName == "DayOfMonth") return;

            // Навігацію не чіпаємо
            switch (e.Key)
            {
                case Key.Up:
                case Key.Down:
                case Key.Left:
                case Key.Right:
                case Key.Tab:
                case Key.Enter:
                case Key.Escape:
                    return;
            }

            var key = (e.Key == Key.System) ? e.SystemKey : e.Key;
            var keyText = _vm.FormatKeyGesture(key, Keyboard.Modifiers);

            // Якщо FormatKeyGesture повернув null для одиночних клавіш (modifiers==None),
            // беремо "символьний" варіант: 1 / M / A ...
            if (string.IsNullOrWhiteSpace(keyText))
                keyText = key.ToString();

            // Якщо немає бінда — дозволяємо звичайний ввод (не чіпаємо)
            if (!_vm.TryGetBindValue(keyText, out var bindValue))
                return;

            // Якщо клітинка редагується — зупиняємо редагування, щоб символ не вставився
            if (IsCurrentCellEditing(dataGridAvailabilityDays))
                dataGridAvailabilityDays.CommitEdit(DataGridEditingUnit.Cell, true);

            var rowIndex = dataGridAvailabilityDays.Items.IndexOf(dataGridAvailabilityDays.CurrentItem);
            if (!_vm.TryApplyBindToCell(columnName, rowIndex, bindValue, out var nextRowIndex))
                return;

            dataGridAvailabilityDays.CommitEdit(DataGridEditingUnit.Cell, true);
            dataGridAvailabilityDays.CommitEdit(DataGridEditingUnit.Row, true);

            if (nextRowIndex is int next && next >= 0 && next < dataGridAvailabilityDays.Items.Count)
            {
                var nextItem = dataGridAvailabilityDays.Items[next];

                dataGridAvailabilityDays.ScrollIntoView(nextItem, boundColumn);
                dataGridAvailabilityDays.CurrentCell = new DataGridCellInfo(nextItem, boundColumn);

                // Замість SelectedItem (рядок) — вибираємо клітинку
                dataGridAvailabilityDays.SelectedCells.Clear();
                dataGridAvailabilityDays.SelectedCells.Add(new DataGridCellInfo(nextItem, boundColumn));

                // Краще почати редагування через Dispatcher, щоб не ловити "invalid state"
                dataGridAvailabilityDays.Dispatcher.BeginInvoke(new Action(() =>
                {
                    dataGridAvailabilityDays.Focus();
                    dataGridAvailabilityDays.BeginEdit();
                }), System.Windows.Threading.DispatcherPriority.Background);
            }


            e.Handled = true;
        }

        private void DataGridAvailabilityDays_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;
            if (e.Column is not DataGridBoundColumn boundColumn) return;
            if (e.Row.Item is not DataRowView rowView) return;

            var binding = boundColumn.Binding as Binding;
            var columnName = binding?.Path?.Path?.Trim('[', ']') ?? string.Empty;
            if (string.IsNullOrWhiteSpace(columnName) || columnName == "DayOfMonth")
                return;

            var raw = (e.EditingElement as TextBox)?.Text ?? string.Empty;
            if (!TryNormalizeNoSpaceInterval(raw, out var normalized))
                return;

            rowView[columnName] = normalized;
        }

        private static bool TryNormalizeNoSpaceInterval(string raw, out string normalized)
        {
            normalized = raw;

            var match = _intervalNoSpace.Match(raw);
            if (!match.Success)
                return false;

            var h1 = int.Parse(match.Groups["h1"].Value);
            var m1 = int.Parse(match.Groups["m1"].Value);
            var h2 = int.Parse(match.Groups["h2"].Value);
            var m2 = int.Parse(match.Groups["m2"].Value);

            var start = new TimeSpan(h1, m1, 0);
            var end = new TimeSpan(h2, m2, 0);

            if (start >= end)
                return false;

            normalized = $"{start:hh\\:mm} - {end:hh\\:mm}";
            return true;
        }

        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !_digitsOnly.IsMatch(e.Text);
        }

        private void NumberOnly_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.SourceDataObject.GetDataPresent(DataFormats.Text, true))
            {
                e.CancelCommand();
                return;
            }

            var text = e.SourceDataObject.GetData(DataFormats.Text) as string ?? "";
            if (!_digitsOnly.IsMatch(text))
                e.CancelCommand();
        }

        // Month: 1..12
        private void Month_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox tb) return;

            if (!int.TryParse(tb.Text, out int value))
            {
                tb.Text = "1";
                return;
            }

            if (value < 1) tb.Text = "1";
            else if (value > 12) tb.Text = "12";
        }

        // Year: 2000..3000
        private void Year_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox tb) return;

            if (!int.TryParse(tb.Text, out int value))
            {
                tb.Text = "2000";
                return;
            }

            if (value < 2000) tb.Text = "2000";
            else if (value > 3000) tb.Text = "3000";
        }

        private void AvailabilityGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (sender is not DataGrid grid)
                return;

            // DataView -> DataTable -> DataColumn
            if (grid.ItemsSource is not DataView dv || dv.Table == null)
                return;

            if (!dv.Table.Columns.Contains(e.PropertyName))
                return;

            var dc = dv.Table.Columns[e.PropertyName];

            // Header з Caption (у тебе Caption = "Day" і ПІБ працівника)
            e.Column.Header = dc.Caption;

            // 1) Day колонка фіксована і ReadOnly
            if (dc.ColumnName == "DayOfMonth")
            {
                e.Column.Width = new DataGridLength(70);
                e.Column.MinWidth = 70;
                e.Column.IsReadOnly = true;
                return;
            }

            // 2) Employee колонки: ділять простір, але не менше MinWidth (скрол з’явиться автоматично)
            e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            e.Column.MinWidth = 140; // або інше число
        }



    }
}
