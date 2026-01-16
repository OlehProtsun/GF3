using System;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WPFApp.Service;
using WPFApp.ViewModel.Container;
using WPFApp.ViewModel.Dialogs;

namespace WPFApp.View.Container
{
    /// <summary>
    /// Interaction logic for ContainerScheduleEditView.xaml
    /// </summary>
    public partial class ContainerScheduleEditView : UserControl
    {
        private ContainerScheduleEditViewModel? _vm;
        private string? _previousCellValue;
        private bool _isPainting;
        private ScheduleMatrixCellRef? _lastPaintedCell;

        public ContainerScheduleEditView()
        {
            InitializeComponent();
            Loaded += ContainerScheduleEditView_Loaded;
            Unloaded += ContainerScheduleEditView_Unloaded;
            DataContextChanged += ContainerScheduleEditView_DataContextChanged;
            dataGridScheduleMatrix.PreparingCellForEdit += ScheduleMatrix_PreparingCellForEdit;
            dataGridScheduleMatrix.CurrentCellChanged += ScheduleMatrix_CurrentCellChanged;
            dataGridScheduleMatrix.PreviewMouseLeftButtonDown += ScheduleMatrix_PreviewMouseLeftButtonDown;
            dataGridScheduleMatrix.MouseMove += ScheduleMatrix_MouseMove;
            dataGridScheduleMatrix.PreviewMouseLeftButtonUp += ScheduleMatrix_PreviewMouseLeftButtonUp;
        }

        private void ContainerScheduleEditView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AttachViewModel(DataContext as ContainerScheduleEditViewModel);
        }

        private void ContainerScheduleEditView_Loaded(object sender, RoutedEventArgs e)
        {
            AttachViewModel(DataContext as ContainerScheduleEditViewModel);
        }

        private void ContainerScheduleEditView_Unloaded(object sender, RoutedEventArgs e)
        {
            DetachViewModel();
        }

        private void AttachViewModel(ContainerScheduleEditViewModel? viewModel)
        {
            if (_vm != null)
                _vm.MatrixChanged -= VmOnMatrixChanged;

            _vm = viewModel;

            if (_vm != null)
            {
                _vm.MatrixChanged += VmOnMatrixChanged;
                BuildMatrixColumns(_vm.ScheduleMatrix.Table, dataGridScheduleMatrix, isReadOnly: false);
                BuildMatrixColumns(_vm.AvailabilityPreviewMatrix.Table, dataGridAvailabilityPreview, isReadOnly: true);
            }
        }

        private void DetachViewModel()
        {
            if (_vm == null) return;
            _vm.MatrixChanged -= VmOnMatrixChanged;
        }

        private void VmOnMatrixChanged(object? sender, System.EventArgs e)
        {
            if (_vm is null) return;
            BuildMatrixColumns(_vm.ScheduleMatrix.Table, dataGridScheduleMatrix, isReadOnly: false);
            BuildMatrixColumns(_vm.AvailabilityPreviewMatrix.Table, dataGridAvailabilityPreview, isReadOnly: true);
        }

        private static void BuildMatrixColumns(DataTable? table, DataGrid grid, bool isReadOnly)
        {
            if (table is null)
            {
                grid.ItemsSource = null;
                grid.Columns.Clear();
                return;
            }

            grid.ItemsSource = table.DefaultView;

            grid.AutoGenerateColumns = false;
            grid.Columns.Clear();
            grid.FrozenColumnCount = 1;

            var dayColName = ContainerScheduleEditViewModel.DayColumnName;
            var conflictColName = ContainerScheduleEditViewModel.ConflictColumnName;

            var tbStyle = (Style)Application.Current.FindResource("MatrixCellTextBlockStyle");
            var editStyle = (Style)Application.Current.FindResource("MatrixCellTextBoxStyle");
            var dangerBrush = (System.Windows.Media.Brush)Application.Current.FindResource("DangerBrush");
            var boolToVis = new BooleanToVisibilityConverter();

            foreach (DataColumn column in table.Columns)
            {
                // ❌ не показуємо Conflict колонку
                if (column.ColumnName == conflictColName)
                    continue;

                var header = string.IsNullOrWhiteSpace(column.Caption) ? column.ColumnName : column.Caption;

                // ✅ Day колонка: текст + червона крапка якщо Conflict==true
                if (column.ColumnName == dayColName)
                {
                    var templateCol = new DataGridTemplateColumn
                    {
                        Header = header,
                        Width = 70,
                        IsReadOnly = true
                    };

                    var root = new FrameworkElementFactory(typeof(Grid));

                    var txt = new FrameworkElementFactory(typeof(TextBlock));
                    txt.SetValue(FrameworkElement.StyleProperty, tbStyle);
                    txt.SetValue(FrameworkElement.MarginProperty, new Thickness(14, 0, 0, 0)); // щоб був відступ під крапку
                    txt.SetBinding(TextBlock.TextProperty, new Binding($"[{dayColName}]"));
                    root.AppendChild(txt);

                    var dot = new FrameworkElementFactory(typeof(Ellipse));
                    dot.SetValue(FrameworkElement.WidthProperty, 8.0);
                    dot.SetValue(FrameworkElement.HeightProperty, 8.0);
                    dot.SetValue(Shape.FillProperty, dangerBrush);
                    dot.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
                    dot.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
                    dot.SetValue(FrameworkElement.MarginProperty, new Thickness(4, 0, 0, 0));
                    dot.SetBinding(UIElement.VisibilityProperty, new Binding($"[{conflictColName}]")
                    {
                        Converter = boolToVis
                    });
                    root.AppendChild(dot);

                    templateCol.CellTemplate = new DataTemplate { VisualTree = root };

                    grid.Columns.Add(templateCol);
                    continue;
                }

                // ✅ інші колонки (редаговані або readonly)
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
                    IsReadOnly = isReadOnly || column.ReadOnly,
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                    ElementStyle = tbStyle,
                    EditingElementStyle = editStyle
                };

                grid.Columns.Add(col);
            }
        }


        private void ScheduleMatrix_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            if (e.EditingElement is TextBox tb)
                _previousCellValue = tb.Text;
        }

        private void ScheduleMatrix_CurrentCellChanged(object? sender, EventArgs e)
        {
            if (_vm is null) return;
            if (dataGridScheduleMatrix.CurrentCell.Column is null) return;
            if (dataGridScheduleMatrix.CurrentCell.Item is null) return;

            if (_vm.TryBuildCellReference(dataGridScheduleMatrix.CurrentCell.Item,
                    dataGridScheduleMatrix.CurrentCell.Column.Header?.ToString(),
                    out var cellRef))
            {
                _vm.SelectedCellRef = cellRef;
            }
            else
            {
                _vm.SelectedCellRef = null;
            }
        }

        private void ScheduleMatrix_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_vm is null) return;

            var cell = FindParent<DataGridCell>(e.OriginalSource as DependencyObject);
            if (cell == null) return;

            if (TryGetCellReference(cell, out var cellRef))
            {
                _vm.SelectedCellRef = cellRef;
                ApplyPaint(cellRef);
                _isPainting = true;
                _lastPaintedCell = cellRef;
            }
        }

        private void ScheduleMatrix_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPainting || _vm is null || e.LeftButton != MouseButtonState.Pressed)
                return;

            var cell = FindParent<DataGridCell>(e.OriginalSource as DependencyObject);
            if (cell == null) return;

            if (TryGetCellReference(cell, out var cellRef))
            {
                if (_lastPaintedCell.HasValue && _lastPaintedCell.Value.Equals(cellRef))
                    return;

                ApplyPaint(cellRef);
                _lastPaintedCell = cellRef;
            }
        }

        private void ScheduleMatrix_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isPainting = false;
            _lastPaintedCell = null;
        }

        private void ScheduleMatrix_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (_vm is null) return;
            if (e.EditAction != DataGridEditAction.Commit) return;
            if (e.Row.Item is not DataRowView rowView) return;
            if (e.Column is not DataGridBoundColumn boundColumn) return;

            var binding = boundColumn.Binding as Binding;
            var columnName = binding?.Path?.Path?.Trim('[', ']') ?? string.Empty;

            if (columnName == ContainerScheduleEditViewModel.DayColumnName
                || columnName == ContainerScheduleEditViewModel.ConflictColumnName)
                return;

            if (!int.TryParse(rowView[ContainerScheduleEditViewModel.DayColumnName]?.ToString(), out var day))
                return;

            var raw = (e.EditingElement as TextBox)?.Text ?? rowView[columnName]?.ToString() ?? string.Empty;

            if (!_vm.TryApplyMatrixEdit(columnName, day, raw, out var normalized, out var error))
            {
                rowView[columnName] = _previousCellValue ?? ContainerScheduleEditViewModel.EmptyMark;
                if (!string.IsNullOrWhiteSpace(error))
                    CustomMessageBox.Show("Error", error, CustomMessageBoxIcon.Error, okText: "OK");
                return;
            }

            rowView[columnName] = normalized;
        }

        private async void ScheduleBlockSelect_Click(object sender, RoutedEventArgs e)
        {
            if (_vm is null) return;
            if (sender is not Button button) return;
            if (button.DataContext is not ScheduleBlockViewModel block) return;

            await _vm.SelectBlockAsync(block);
        }

        private async void ScheduleBlockClose_Click(object sender, RoutedEventArgs e)
        {
            if (_vm is null) return;
            if (sender is not Button button) return;
            if (button.DataContext is not ScheduleBlockViewModel block) return;

            await _vm.CloseBlockAsync(block);
        }

        private void ApplyPaint(ScheduleMatrixCellRef cellRef)
        {
            _vm?.ApplyPaintToCell(cellRef);
        }

        private bool TryGetCellReference(DataGridCell cell, out ScheduleMatrixCellRef cellRef)
        {
            cellRef = default;
            if (_vm is null) return false;
            if (cell.Column?.Header is null) return false;
            return _vm.TryBuildCellReference(cell.DataContext, cell.Column.Header.ToString(), out cellRef);
        }

        private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T typed)
                    return typed;

                child = VisualTreeHelper.GetParent(child);
            }

            return null;
        }

    }
}
