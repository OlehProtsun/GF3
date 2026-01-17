using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using WPFApp.Service;
using WPFApp.ViewModel.Container;
using WPFApp.ViewModel.Dialogs;
using static WPFApp.ViewModel.Container.ContainerScheduleEditViewModel;

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
            dataGridScheduleMatrix.SelectedCellsChanged += ScheduleMatrix_SelectedCellsChanged;
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
                ScheduleMatrixColumnBuilder.BuildScheduleMatrixColumns(_vm.ScheduleMatrix.Table, dataGridScheduleMatrix, isReadOnly: false);
                ScheduleMatrixColumnBuilder.BuildScheduleMatrixColumns(_vm.AvailabilityPreviewMatrix.Table, dataGridAvailabilityPreview, isReadOnly: true);
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

            void RefreshMatrices()
            {
                ScheduleMatrixColumnBuilder.BuildScheduleMatrixColumns(_vm.ScheduleMatrix.Table, dataGridScheduleMatrix, isReadOnly: false);
                ScheduleMatrixColumnBuilder.BuildScheduleMatrixColumns(_vm.AvailabilityPreviewMatrix.Table, dataGridAvailabilityPreview, isReadOnly: true);
            }

            if (Dispatcher.CheckAccess())
            {
                RefreshMatrices();
            }
            else
            {
                Dispatcher.Invoke(RefreshMatrices);
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

        private void ScheduleMatrix_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (_vm is null) return;

            var selectedCells = dataGridScheduleMatrix.SelectedCells;
            if (selectedCells.Count == 0)
            {
                _vm.UpdateSelectedCellRefs(Array.Empty<ScheduleMatrixCellRef>());
                return;
            }

            var refs = new List<ScheduleMatrixCellRef>(selectedCells.Count);
            foreach (var cellInfo in selectedCells)
            {
                var columnName = cellInfo.Column?.SortMemberPath ?? cellInfo.Column?.Header?.ToString();
                if (columnName is null)
                    continue;

                if (_vm.TryBuildCellReference(cellInfo.Item, columnName, out var cellRef))
                    refs.Add(cellRef);
            }

            _vm.UpdateSelectedCellRefs(refs);
        }

        private void ScheduleMatrix_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_vm is null) return;

            // ✅ стандартне виділення — без втручання
            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                return;

            // ✅ paint тільки коли реально увімкнено режим фарбування
            if (_vm.ActivePaintMode == SchedulePaintMode.None)
                return;

            var cell = FindParent<DataGridCell>(e.OriginalSource as DependencyObject);
            if (cell == null) return;

            if (TryGetCellReference(cell, out var cellRef))
            {
                _vm.SelectedCellRef = cellRef;
                ApplyPaint(cellRef);
                _isPainting = true;
                _lastPaintedCell = cellRef;

                e.Handled = true; // ✅ щоб DataGrid не “смикав” selection під час paint
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
