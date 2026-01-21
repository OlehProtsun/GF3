using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
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
        private readonly ILoggerService _logger = LoggerService.Instance;
        private DateTime _lastScrollLogUtc = DateTime.MinValue;
        private bool _shopComboOpen;
        private bool _availabilityComboOpen;
        private bool _suspendMatrixRefresh;
        private bool _pendingMatrixRefresh;

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
            dataGridScheduleMatrix.AddHandler(
                ScrollViewer.ScrollChangedEvent,
                new ScrollChangedEventHandler(ScheduleMatrix_ScrollChanged));
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
            if (ReferenceEquals(_vm, viewModel))
                return;

            // ---- unsubscribe old vm ----
            if (_vm != null)
            {
                _vm.MatrixChanged -= VmOnMatrixChanged;
            }

            // ---- assign new vm ----
            _vm = viewModel;

            // ---- subscribe new vm + init grid ----
            if (_vm != null)
            {
                _vm.MatrixChanged += VmOnMatrixChanged;

                // Build columns only if we already have a schema.
                if (_vm.ScheduleMatrix?.Table != null)
                {
                    ScheduleMatrixColumnBuilder.BuildScheduleMatrixColumns(
                        _vm.ScheduleMatrix.Table,
                        dataGridScheduleMatrix,
                        isReadOnly: false);
                }

                if (_vm.AvailabilityPreviewMatrix?.Table != null)
                {
                    ScheduleMatrixColumnBuilder.BuildScheduleMatrixColumns(
                        _vm.AvailabilityPreviewMatrix.Table,
                        dataGridAvailabilityPreview,
                        isReadOnly: true);
                }

                // Assign ItemsSource
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

        private static void HardResetGrid(DataGrid grid)
        {
            if (grid == null) return;

            // зупинити редагування, щоб DataGrid не тримав старі row/cell containers
            grid.CancelEdit(DataGridEditingUnit.Cell);
            grid.CancelEdit(DataGridEditingUnit.Row);

            // прибрати selection/current cell (це часто тримає DataRowView)
            grid.UnselectAllCells();
            grid.UnselectAll();
            grid.SelectedItem = null;
            grid.CurrentCell = new DataGridCellInfo();
        }

        private static void SwapItemsSource(DataGrid grid, IEnumerable? source)
        {
            HardResetGrid(grid);

            // важливо: спочатку null -> тоді нове
            grid.ItemsSource = null;
            grid.ItemsSource = source; // тепер тип правильний
        }


        private void DetachViewModel()
        {
            if (_vm == null) return;

            _vm.MatrixChanged -= VmOnMatrixChanged;
            _vm.CancelBackgroundWork();

            // !!! ключове: відпускаємо важкі DataView/DataRowView зі сторони DataGrid
            SwapItemsSource(dataGridScheduleMatrix, null);
            SwapItemsSource(dataGridAvailabilityPreview, null);
            dataGridScheduleMatrix.Columns.Clear();
            dataGridAvailabilityPreview.Columns.Clear();
            _scheduleSchemaSig = null;
            _previewSchemaSig = null;
            _vm = null;
        }


        private void VmOnMatrixChanged(object? sender, EventArgs e)
        {
            if (_vm is null) return;

            if (_suspendMatrixRefresh)
            {
                _pendingMatrixRefresh = true;
                return;
            }

            if (_refreshQueued) return;
            _refreshQueued = true;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                _refreshQueued = false;

                RefreshMatricesSmart();
            }), DispatcherPriority.Background);
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
            if (_vm is null)
                return;

            var scheduleTable = _vm.ScheduleMatrix?.Table;

            if (scheduleTable == null || scheduleTable.Columns.Count == 0)
            {
                if (dataGridScheduleMatrix.ItemsSource != null || dataGridScheduleMatrix.Columns.Count > 0)
                {
                    ResetGridColumns(dataGridScheduleMatrix);
                    _scheduleSchemaSig = null;
                }
            }
            else
            {
                var scheduleSig = BuildSig(scheduleTable);

                if (scheduleSig != _scheduleSchemaSig)
                {
                    ResetGridColumns(dataGridScheduleMatrix);
                    ScheduleMatrixColumnBuilder.BuildScheduleMatrixColumns(
                        scheduleTable,
                        dataGridScheduleMatrix,
                        isReadOnly: false);

                    _scheduleSchemaSig = scheduleSig;
                }

                if (!ReferenceEquals(dataGridScheduleMatrix.ItemsSource, _vm.ScheduleMatrix))
                {
                    SwapItemsSource(dataGridScheduleMatrix, _vm.ScheduleMatrix);
                }
            }

            var previewTable = _vm.AvailabilityPreviewMatrix?.Table;

            if (previewTable == null || previewTable.Columns.Count == 0)
            {
                if (dataGridAvailabilityPreview.ItemsSource != null || dataGridAvailabilityPreview.Columns.Count > 0)
                {
                    ResetGridColumns(dataGridAvailabilityPreview);
                    _previewSchemaSig = null;
                }
            }
            else
            {
                var previewSig = BuildSig(previewTable);

                if (previewSig != _previewSchemaSig)
                {
                    ResetGridColumns(dataGridAvailabilityPreview);
                    ScheduleMatrixColumnBuilder.BuildScheduleMatrixColumns(
                        previewTable,
                        dataGridAvailabilityPreview,
                        isReadOnly: true);

                    _previewSchemaSig = previewSig;
                }

                if (!ReferenceEquals(dataGridAvailabilityPreview.ItemsSource, _vm.AvailabilityPreviewMatrix))
                {
                    SwapItemsSource(dataGridAvailabilityPreview, _vm.AvailabilityPreviewMatrix);
                }
            }
        }


        private static string BuildSig(DataTable? table)
        {
            if (table == null || table.Columns.Count == 0)
                return string.Empty;

            return string.Join("|", table.Columns.Cast<DataColumn>()
                .Select(c => $"{c.ColumnName}:{c.DataType.Name}:{c.Caption}"));
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

        private void ShopComboBox_DropDownClosed(object sender, EventArgs e)
        {
            _shopComboOpen = false;
            _vm?.CommitPendingShopSelection();
            ResumeMatrixRefresh();
        }

        private void AvailabilityComboBox_DropDownClosed(object sender, EventArgs e)
        {
            _availabilityComboOpen = false;
            _vm?.CommitPendingAvailabilitySelection();
            ResumeMatrixRefresh();
        }

        private void ShopComboBox_DropDownOpened(object sender, EventArgs e)
        {
            _shopComboOpen = true;
            PauseMatrixRefresh();
        }

        private void AvailabilityComboBox_DropDownOpened(object sender, EventArgs e)
        {
            _availabilityComboOpen = true;
            PauseMatrixRefresh();
        }

        private void PauseMatrixRefresh()
        {
            _suspendMatrixRefresh = true;
        }

        private void ResumeMatrixRefresh()
        {
            _suspendMatrixRefresh = false;

            if (_pendingMatrixRefresh)
            {
                _pendingMatrixRefresh = false;
                RefreshMatricesSmart();
            }
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

        private void ScheduleMatrix_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!_shopComboOpen && !_availabilityComboOpen)
                return;

            var now = DateTime.UtcNow;
            if ((now - _lastScrollLogUtc).TotalMilliseconds < 500)
                return;

            _lastScrollLogUtc = now;
            _logger.LogPerf("ScheduleGrid", $"ScrollChanged: V={e.VerticalOffset:F2} H={e.HorizontalOffset:F2} ΔV={e.VerticalChange:F2} ΔH={e.HorizontalChange:F2}");
        }

        private void LogGridSnapshot(string context)
        {
            var grid = dataGridScheduleMatrix;
            var scrollViewer = FindChild<ScrollViewer>(grid);
            var itemsCount = grid.Items.Count;
            var columnsCount = grid.Columns.Count;
            var sourceType = grid.ItemsSource?.GetType().Name ?? "null";
            var scrollInfo = scrollViewer == null
                ? "ScrollViewer=null"
                : $"V={scrollViewer.VerticalOffset:F2}/{scrollViewer.ScrollableHeight:F2} H={scrollViewer.HorizontalOffset:F2}/{scrollViewer.ScrollableWidth:F2}";

            _logger.LogPerf(
                "ScheduleGrid",
                $"{context} Items={itemsCount} Columns={columnsCount} Source={sourceType} {scrollInfo}");
        }

        private static T? FindChild<T>(DependencyObject? parent) where T : DependencyObject
        {
            if (parent == null) return null;

            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed)
                    return typed;

                var nested = FindChild<T>(child);
                if (nested != null)
                    return nested;
            }

            return null;
        }

    }
}
