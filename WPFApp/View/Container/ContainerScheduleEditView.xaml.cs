using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
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
        private string? _scheduleSchemaSig;
        private string? _previewSchemaSig;

        // coalesce MatrixChanged bursts (generation/lookup refreshes) into a single UI refresh
        private bool _refreshQueued;


        public ContainerScheduleEditView()
        {
            InitializeComponent();

            // Performance: ensure virtualization is ON even if the Style turns it off.
            ConfigureGridPerformance(dataGridScheduleMatrix);
            ConfigureGridPerformance(dataGridAvailabilityPreview);

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

        private static void ResetGridColumns(DataGrid grid)
        {
            // важливо: скинути ItemsSource, щоб WPF відпустив старі binding-и/containers
            grid.ItemsSource = null;
            grid.Columns.Clear();
        }


        private static void ConfigureGridPerformance(DataGrid grid)
        {
            if (grid == null) return;

            // Row/column virtualization
            grid.EnableRowVirtualization = true;
            grid.EnableColumnVirtualization = true;
            VirtualizingPanel.SetIsVirtualizing(grid, true);
            VirtualizingPanel.SetVirtualizationMode(grid, VirtualizationMode.Recycling);

            // Keep virtualization working (some styles disable it via CanContentScroll=false)
            grid.SetValue(ScrollViewer.CanContentScrollProperty, true);
            grid.SetValue(ScrollViewer.IsDeferredScrollingEnabledProperty, false);

            // Avoid layout churn on fast scroll
            grid.SetValue(VirtualizingPanel.ScrollUnitProperty, ScrollUnit.Item);
            VirtualizingPanel.SetCacheLengthUnit(grid, VirtualizationCacheLengthUnit.Page);
            VirtualizingPanel.SetCacheLength(grid, new VirtualizationCacheLength(1));

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

                // Build columns only if we already have a schema.
                if (_vm.ScheduleMatrix?.Table != null)
                    ScheduleMatrixColumnBuilder.BuildScheduleMatrixColumns(_vm.ScheduleMatrix.Table, dataGridScheduleMatrix, isReadOnly: false);
                if (_vm.AvailabilityPreviewMatrix?.Table != null)
                    ScheduleMatrixColumnBuilder.BuildScheduleMatrixColumns(_vm.AvailabilityPreviewMatrix.Table, dataGridAvailabilityPreview, isReadOnly: true);

                dataGridScheduleMatrix.ItemsSource = _vm.ScheduleMatrix;
                dataGridAvailabilityPreview.ItemsSource = _vm.AvailabilityPreviewMatrix;
            }
        }

        private async void OpenedSchedulesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_vm is null) return;

            if (sender is ListBox lb && lb.SelectedItem is ScheduleBlockViewModel block)
            {
                // щоб не викликати повторно при тих самих значеннях
                if (ReferenceEquals(_vm.ActiveSchedule, block))
                    return;

                await _vm.SelectBlockAsync(block); // це веде в owner.SelectScheduleBlockAsync(...) :contentReference[oaicite:1]{index=1}
            }
        }


        private void DetachViewModel()
        {
            if (_vm == null) return;

            _vm.MatrixChanged -= VmOnMatrixChanged;

            // ✅ зупиняємо фоновий preview, щоб він не спамив UI після виходу з Edit
            _vm.CancelBackgroundWork();
        }

        private void VmOnMatrixChanged(object? sender, System.EventArgs e)
        {
            if (_vm is null) return;

            // Coalesce multiple MatrixChanged into one refresh so we don't queue dozens of UI updates
            // during Generate / availability preview rebuild.
            if (_refreshQueued) return;
            _refreshQueued = true;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                _refreshQueued = false;
                RefreshMatricesSmart();
            }), System.Windows.Threading.DispatcherPriority.Background);
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

        private void RefreshMatricesSmart()
        {
            if (_vm is null) return;

            var scheduleSig = BuildSig(_vm.ScheduleMatrix?.Table);
            if (scheduleSig != _scheduleSchemaSig)
            {
                ResetGridColumns(dataGridScheduleMatrix);
                ScheduleMatrixColumnBuilder.BuildScheduleMatrixColumns(_vm.ScheduleMatrix.Table, dataGridScheduleMatrix, isReadOnly: false);
                _scheduleSchemaSig = scheduleSig;
            }

            if (!ReferenceEquals(dataGridScheduleMatrix.ItemsSource, _vm.ScheduleMatrix))
                dataGridScheduleMatrix.ItemsSource = _vm.ScheduleMatrix;


            var previewSig = BuildSig(_vm.AvailabilityPreviewMatrix?.Table);
            if (previewSig != _previewSchemaSig)
            {
                ResetGridColumns(dataGridAvailabilityPreview);
                ScheduleMatrixColumnBuilder.BuildScheduleMatrixColumns(_vm.AvailabilityPreviewMatrix.Table, dataGridAvailabilityPreview, isReadOnly: true);
                _previewSchemaSig = previewSig;
            }

            if (!ReferenceEquals(dataGridAvailabilityPreview.ItemsSource, _vm.AvailabilityPreviewMatrix))
                dataGridAvailabilityPreview.ItemsSource = _vm.AvailabilityPreviewMatrix;

        }

        private static string BuildSig(DataTable? t)
        {
            if (t == null) return "";
            // тільки імена колонок (можна ще типи додати)
            return string.Join("|", t.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
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

