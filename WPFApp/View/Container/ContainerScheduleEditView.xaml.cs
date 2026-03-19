/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerScheduleEditView у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WPFApp.Controls;
using WPFApp.UI.Dialogs;
using WPFApp.UI.Matrix.Schedule;
using WPFApp.ViewModel.Container.Edit;
using WPFApp.ViewModel.Container.Edit.Helpers;
using WPFApp.ViewModel.Container.ScheduleEdit;
using WPFApp.ViewModel.Container.ScheduleEdit.Helpers;
using WPFApp.ViewModel.Dialogs;

namespace WPFApp.View.Container
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public partial class ContainerScheduleEditView : UserControl` та контракт його використання у шарі WPFApp.
    /// </summary>
    public partial class ContainerScheduleEditView : UserControl
    {
        
        
        

        
        
        
        
        private ContainerScheduleEditViewModel? _vm;

        
        
        

        
        
        
        
        private string? _previousCellValue;

        
        
        

        
        
        
        private bool _isPainting;

        private bool _isDayRowDragSelecting;
        private int _dayRowDragAnchorIndex = -1;
        private int _dayRowDragLastIndex = -1;
        private bool _dayRowDragKeepExisting;

        
        
        
        
        private ScheduleMatrixCellRef? _lastPaintedCell;

        
        
        

        
        
        
        
        
        private int? _scheduleSchemaHash;

        
        
        
        private int? _previewSchemaHash;

        
        
        

        
        
        
        

        
        
        
        private bool _shopComboOpen;

        
        
        
        private bool _availabilityComboOpen;

        
        
        
        
        
        
        private bool _suspendMatrixRefresh;

        
        
        
        
        private bool _pendingMatrixRefresh;

        
        
        
        
        private bool _refreshQueued;
        private readonly DispatcherTimer _matrixRefreshThrottleTimer;
        private bool _matrixRefreshRequested;

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ContainerScheduleEditView()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ContainerScheduleEditView()
        {
            InitializeComponent();

            _matrixRefreshThrottleTimer = new DispatcherTimer(
                TimeSpan.FromMilliseconds(16),
                DispatcherPriority.Background,
                MatrixRefreshThrottleTimer_Tick,
                Dispatcher);

            
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

            
            ResetInteractionState(commitPendingSelections: false);

            
            if (_vm != null)
                _vm.MatrixChanged -= VmOnMatrixChanged;

            _vm = viewModel;

            
            if (_vm != null)
            {
                _vm.MatrixChanged += VmOnMatrixChanged;

                
                if (_vm.ScheduleMatrix?.Table != null)
                {
                    ScheduleMatrixColumnBuilder.BuildScheduleMatrixColumns(
                        _vm.ScheduleMatrix.Table,
                        dataGridScheduleMatrix,
                        isReadOnly: false);

                    _scheduleSchemaHash = BuildSchemaHash(_vm.ScheduleMatrix.Table);
                }
                else
                {
                    _scheduleSchemaHash = null;
                }

                if (_vm.AvailabilityPreviewMatrix?.Table != null)
                {
                    ScheduleMatrixColumnBuilder.BuildScheduleMatrixColumns(
                        _vm.AvailabilityPreviewMatrix.Table,
                        dataGridAvailabilityPreview,
                        isReadOnly: true);

                    _previewSchemaHash = BuildSchemaHash(_vm.AvailabilityPreviewMatrix.Table);
                }
                else
                {
                    _previewSchemaHash = null;
                }

                
                dataGridScheduleMatrix.ItemsSource = _vm.ScheduleMatrix;
                dataGridAvailabilityPreview.ItemsSource = _vm.AvailabilityPreviewMatrix;
            }
            else
            {
                
                SwapItemsSource(dataGridScheduleMatrix, null);
                SwapItemsSource(dataGridAvailabilityPreview, null);
                dataGridScheduleMatrix.Columns.Clear();
                dataGridAvailabilityPreview.Columns.Clear();
                _scheduleSchemaHash = null;
                _previewSchemaHash = null;
            }
        }

        
        
        
        
        
        
        
        
        private void DetachViewModel()
        {
            if (_vm == null) return;

            
            ResetInteractionState(commitPendingSelections: true);

            _vm.MatrixChanged -= VmOnMatrixChanged;

            
            _vm.CancelBackgroundWork();

            
            SwapItemsSource(dataGridScheduleMatrix, null);
            SwapItemsSource(dataGridAvailabilityPreview, null);
            dataGridScheduleMatrix.Columns.Clear();
            dataGridAvailabilityPreview.Columns.Clear();

            _scheduleSchemaHash = null;
            _previewSchemaHash = null;

            _vm = null;
        }

        
        
        
        
        private void ResetInteractionState(bool commitPendingSelections)
        {
            _shopComboOpen = false;
            _availabilityComboOpen = false;

            _suspendMatrixRefresh = false;
            _pendingMatrixRefresh = false;


            if (commitPendingSelections && _vm != null)
            {
                _vm.CommitPendingShopSelection();
                _vm.CommitPendingAvailabilitySelection();
            }
        }

        
        
        

        
        
        
        
        
        private static void ConfigureGridPerformance(DataGrid grid)
        {
            if (grid == null) return;

            grid.EnableRowVirtualization = true;
            grid.EnableColumnVirtualization = true;

            VirtualizingPanel.SetIsVirtualizing(grid, true);
            VirtualizingPanel.SetVirtualizationMode(grid, VirtualizationMode.Recycling);

            
            grid.SetValue(ScrollViewer.CanContentScrollProperty, true);

            
            grid.SetValue(ScrollViewer.IsDeferredScrollingEnabledProperty, false);

            
            grid.SetValue(VirtualizingPanel.ScrollUnitProperty, ScrollUnit.Pixel);

            
            VirtualizingPanel.SetCacheLengthUnit(grid, VirtualizationCacheLengthUnit.Page);
            VirtualizingPanel.SetCacheLength(grid, new VirtualizationCacheLength(1));
        }

        
        
        
        
        
        
        
        private static void ResetGridColumns(DataGrid grid)
        {
            grid.ItemsSource = null;
            grid.Columns.Clear();
        }

        
        
        
        
        
        
        
        
        private static void HardResetGrid(DataGrid grid)
        {
            grid.CancelEdit(DataGridEditingUnit.Cell);
            grid.CancelEdit(DataGridEditingUnit.Row);

            grid.UnselectAllCells();
            grid.UnselectAll();
            grid.SelectedItem = null;
            grid.CurrentCell = new DataGridCellInfo();
        }

        
        
        
        
        
        
        
        
        private static void SwapItemsSource(DataGrid grid, IEnumerable? source)
        {
            HardResetGrid(grid);
            grid.ItemsSource = null;
            grid.ItemsSource = source;
        }

        
        
        

        
        
        
        
        
        
        private void VmOnMatrixChanged(object? sender, EventArgs e)
        {
            if (_vm is null) return;

            if (_suspendMatrixRefresh)
            {
                _pendingMatrixRefresh = true;
                return;
            }

            _matrixRefreshRequested = true;
            if (!_matrixRefreshThrottleTimer.IsEnabled)
                _matrixRefreshThrottleTimer.Start();
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
                    _scheduleSchemaHash = null;
                }
            }
            else
            {
                var scheduleHash = BuildSchemaHash(scheduleTable);

                
                if (scheduleHash != _scheduleSchemaHash)
                {
                    ResetGridColumns(dataGridScheduleMatrix);

                    ScheduleMatrixColumnBuilder.BuildScheduleMatrixColumns(
                        scheduleTable,
                        dataGridScheduleMatrix,
                        isReadOnly: false);

                    _scheduleSchemaHash = scheduleHash;
                }

                
                if (!ReferenceEquals(dataGridScheduleMatrix.ItemsSource, _vm.ScheduleMatrix))
                    SwapItemsSource(dataGridScheduleMatrix, _vm.ScheduleMatrix);
            }

            
            
            
            var previewTable = _vm.AvailabilityPreviewMatrix?.Table;

            if (previewTable == null || previewTable.Columns.Count == 0)
            {
                if (dataGridAvailabilityPreview.ItemsSource != null || dataGridAvailabilityPreview.Columns.Count > 0)
                {
                    ResetGridColumns(dataGridAvailabilityPreview);
                    _previewSchemaHash = null;
                }
            }
            else
            {
                var previewHash = BuildSchemaHash(previewTable);

                if (previewHash != _previewSchemaHash)
                {
                    ResetGridColumns(dataGridAvailabilityPreview);

                    ScheduleMatrixColumnBuilder.BuildScheduleMatrixColumns(
                        previewTable,
                        dataGridAvailabilityPreview,
                        isReadOnly: true);

                    _previewSchemaHash = previewHash;
                }

                if (!ReferenceEquals(dataGridAvailabilityPreview.ItemsSource, _vm.AvailabilityPreviewMatrix))
                    SwapItemsSource(dataGridAvailabilityPreview, _vm.AvailabilityPreviewMatrix);
            }
        }

        
        
        
        
        
        
        
        
        
        private static int BuildSchemaHash(DataTable table)
        {
            unchecked
            {
                int hash = 17;
                foreach (DataColumn c in table.Columns)
                {
                    hash = (hash * 31) + (c.ColumnName?.GetHashCode() ?? 0);
                    hash = (hash * 31) + (c.DataType?.GetHashCode() ?? 0);
                }

                return hash;
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

            var col = dataGridScheduleMatrix.CurrentCell.Column;
            var item = dataGridScheduleMatrix.CurrentCell.Item;

            if (col is null || item is null)
            {
                _vm.SelectedCellRef = null;
                return;
            }

            
            
            var columnName = col.SortMemberPath ?? col.Header?.ToString();

            if (_vm.TryBuildCellReference(item, columnName, out var cellRef))
                _vm.SelectedCellRef = cellRef;
            else
                _vm.SelectedCellRef = null;
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
                var col = cellInfo.Column;
                if (col is null) continue;

                var columnName = col.SortMemberPath ?? col.Header?.ToString();
                if (string.IsNullOrWhiteSpace(columnName))
                    continue;

                if (_vm.TryBuildCellReference(cellInfo.Item, columnName, out var cellRef))
                    refs.Add(cellRef);
            }

            _vm.UpdateSelectedCellRefs(refs);
        }

        
        
        

        private void ScheduleMatrix_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_vm is null) return;

            
            
            
            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                var cell = FindParent<DataGridCell>(e.OriginalSource as DependencyObject);
                if (cell == null) return;

                var colName = cell.Column?.SortMemberPath ?? cell.Column?.Header?.ToString();
                if (string.IsNullOrWhiteSpace(colName)) return;

                if (string.Equals(colName, ContainerScheduleEditViewModel.DayColumnName, StringComparison.Ordinal))
                {
                    _dayRowDragKeepExisting = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

                    
                    var row = FindParent<DataGridRow>(cell)
                              ?? (dataGridScheduleMatrix.ItemContainerGenerator.ContainerFromItem(cell.DataContext) as DataGridRow);

                    if (row == null) return;

                    _isDayRowDragSelecting = true;
                    _dayRowDragAnchorIndex = row.GetIndex();
                    _dayRowDragLastIndex = _dayRowDragAnchorIndex;

                    dataGridScheduleMatrix.Focus();
                    dataGridScheduleMatrix.CaptureMouse();

                    
                    SelectWholeRowsRange(_dayRowDragAnchorIndex, _dayRowDragAnchorIndex, _dayRowDragKeepExisting);

                    e.Handled = true;
                    return;
                }

                
                return;
            }

            
            
            
            if (_vm.ActivePaintMode == ContainerScheduleEditViewModel.SchedulePaintMode.None)
                return;

            var paintCell = FindParent<DataGridCell>(e.OriginalSource as DependencyObject);
            if (paintCell == null) return;

            if (TryGetCellReference(paintCell, out var cellRef))
            {
                _vm.SelectedCellRef = cellRef;
                _vm.ApplyPaintToCell(cellRef);

                _isPainting = true;
                _lastPaintedCell = cellRef;

                e.Handled = true;
            }
        }

        private void ScheduleMatrix_MouseMove(object sender, MouseEventArgs e)
        {
            if (_vm is null) return;

            
            
            
            if (_isDayRowDragSelecting &&
                e.LeftButton == MouseButtonState.Pressed &&
                !Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                
                
                if (TryGetRowIndexUnderMouse(e, out int idx))
                {
                    if (idx != _dayRowDragLastIndex)
                    {
                        SelectWholeRowsRange(_dayRowDragAnchorIndex, idx, _dayRowDragKeepExisting);
                        _dayRowDragLastIndex = idx;
                    }
                }

                e.Handled = true;
                return;
            }

            
            
            
            if (!_isPainting) return;

            var paintCell = FindParent<DataGridCell>(e.OriginalSource as DependencyObject);
            if (paintCell == null) return;

            if (TryGetCellReference(paintCell, out var cellRef))
            {
                if (_lastPaintedCell == null || !_lastPaintedCell.Equals(cellRef))
                {
                    _vm.ApplyPaintToCell(cellRef);
                    _lastPaintedCell = cellRef;
                }
            }
        }

        private bool TryGetRowIndexUnderMouse(MouseEventArgs e, out int rowIndex)
        {
            rowIndex = -1;

            
            var pos = e.GetPosition(dataGridScheduleMatrix);

            
            var hit = dataGridScheduleMatrix.InputHitTest(pos) as DependencyObject;
            if (hit == null) return false;

            var row = FindParent<DataGridRow>(hit);
            if (row == null) return false;

            rowIndex = row.GetIndex();
            return rowIndex >= 0;
        }


        private void SelectWholeRowCells(object rowItem, bool keepExistingSelection)
        {
            if (rowItem == null) return;

            dataGridScheduleMatrix.Focus();

            if (!keepExistingSelection)
                dataGridScheduleMatrix.SelectedCells.Clear();

            foreach (var col in dataGridScheduleMatrix.Columns)
            {
                if (col.Visibility != Visibility.Visible)
                    continue;

                
                
                

                dataGridScheduleMatrix.SelectedCells.Add(new DataGridCellInfo(rowItem, col));
            }

            
            var dayCol = dataGridScheduleMatrix.Columns.FirstOrDefault(c =>
                string.Equals(c.SortMemberPath ?? c.Header?.ToString(),
                              ContainerScheduleEditViewModel.DayColumnName,
                              StringComparison.Ordinal));

            if (dayCol != null)
            {
                dataGridScheduleMatrix.CurrentCell = new DataGridCellInfo(rowItem, dayCol);
                dataGridScheduleMatrix.ScrollIntoView(rowItem, dayCol);
            }
        }

        private void SelectWholeRowsRange(int a, int b, bool keepExistingSelection)
        {
            if (a < 0 || b < 0) return;
            if (dataGridScheduleMatrix.Items.Count == 0) return;

            int start = Math.Min(a, b);
            int end = Math.Max(a, b);

            start = Math.Max(0, start);
            end = Math.Min(dataGridScheduleMatrix.Items.Count - 1, end);

            dataGridScheduleMatrix.Focus();

            if (!keepExistingSelection)
                dataGridScheduleMatrix.SelectedCells.Clear();

            var cols = dataGridScheduleMatrix.Columns
                .Where(c => c.Visibility == Visibility.Visible)
                .ToList();

            for (int i = start; i <= end; i++)
            {
                var item = dataGridScheduleMatrix.Items[i];
                foreach (var col in cols)
                    dataGridScheduleMatrix.SelectedCells.Add(new DataGridCellInfo(item, col));
            }

            
            dataGridScheduleMatrix.ScrollIntoView(dataGridScheduleMatrix.Items[end]);
        }



        private void ScheduleMatrix_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isPainting = false;
            _lastPaintedCell = null;

            if (_isDayRowDragSelecting)
            {
                _isDayRowDragSelecting = false;
                _dayRowDragAnchorIndex = -1;
                _dayRowDragLastIndex = -1;

                if (dataGridScheduleMatrix.IsMouseCaptured)
                    dataGridScheduleMatrix.ReleaseMouseCapture();

                e.Handled = true;
            }
        }


        private bool TryGetCellReference(DataGridCell cell, out ScheduleMatrixCellRef cellRef)
        {
            cellRef = default;
            if (_vm is null) return false;

            var col = cell.Column;
            if (col is null) return false;

            var columnName = col.SortMemberPath ?? col.Header?.ToString();
            if (string.IsNullOrWhiteSpace(columnName))
                return false;

            return _vm.TryBuildCellReference(cell.DataContext, columnName, out cellRef);
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

        
        
        

        
        
        
        
        
        
        
        
        
        
        
        private void ScheduleMatrix_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (_vm is null) return;
            if (e.EditAction != DataGridEditAction.Commit) return;
            if (e.Row.Item is not DataRowView rowView) return;

            
            var columnName = e.Column?.SortMemberPath;
            if (string.IsNullOrWhiteSpace(columnName)) return;

            if (columnName == ContainerScheduleEditViewModel.DayColumnName ||
                columnName == ContainerScheduleEditViewModel.ConflictColumnName)
                return;

            if (!int.TryParse(rowView[ContainerScheduleEditViewModel.DayColumnName]?.ToString(), out var day))
                return;

            var raw = (e.EditingElement as TextBox)?.Text ?? string.Empty;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                
                dataGridScheduleMatrix.CommitEdit(DataGridEditingUnit.Cell, true);
                dataGridScheduleMatrix.CommitEdit(DataGridEditingUnit.Row, true);

                
                if (!_vm.TryApplyMatrixEdit(columnName, day, raw, out var normalized, out var error))
                {
                    rowView[columnName] = _previousCellValue ?? ContainerScheduleEditViewModel.EmptyMark;

                    if (!string.IsNullOrWhiteSpace(error))
                        CustomMessageBox.Show("Error", error, CustomMessageBoxIcon.Error, okText: "OK");

                    return;
                }

                
                rowView[columnName] = normalized;

                
                if (dataGridScheduleMatrix.ItemContainerGenerator.ContainerFromItem(rowView) is DataGridRow dgRow)
                    dgRow.InvalidateVisual();

            }), DispatcherPriority.Background);
        }


        
        
        

        private async void OpenedSchedulesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_vm is null) return;

            if (sender is ListBox lb && lb.SelectedItem is ScheduleBlockViewModel block)
            {
                
                if (ReferenceEquals(_vm.ActiveSchedule, block))
                    return;

                await _vm.SelectBlockAsync(block);
            }
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

        
        
        

        
        
        
        
        private void LookupComboBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            PauseMatrixRefresh();
        }

        private void EmployeeComboBox_DropDownOpened(object sender, EventArgs e)
        {
            PauseMatrixRefresh();
        }

        private void EmployeeComboBox_DropDownClosed(object sender, EventArgs e)
        {
            ResumeMatrixRefresh();
        }

        private void ShopComboBox_DropDownOpened(object sender, EventArgs e)
        {
            _shopComboOpen = true;
            PauseMatrixRefresh();
        }

        private void ShopComboBox_DropDownClosed(object sender, EventArgs e)
        {
            _shopComboOpen = false;
            _vm?.CommitPendingShopSelection();
            ResumeMatrixRefresh();
        }

        private void AvailabilityComboBox_DropDownOpened(object sender, EventArgs e)
        {
            _availabilityComboOpen = true;
            PauseMatrixRefresh();
        }

        private void AvailabilityComboBox_DropDownClosed(object sender, EventArgs e)
        {
            _availabilityComboOpen = false;
            _vm?.CommitPendingAvailabilitySelection();
            ResumeMatrixRefresh();
        }

        private void ComboBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not ComboBox comboBox)
                return;

            
            if (comboBox.IsDropDownOpen)
            {
                e.Handled = false;
                return;
            }

            
            e.Handled = true;
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
                _matrixRefreshRequested = true;
                if (!_matrixRefreshThrottleTimer.IsEnabled)
                    _matrixRefreshThrottleTimer.Start();
            }
        }

        private void MatrixRefreshThrottleTimer_Tick(object? sender, EventArgs e)
        {
            if (!_matrixRefreshRequested || _suspendMatrixRefresh)
                return;

            _matrixRefreshRequested = false;
            RefreshMatricesSmart();

            if (!_matrixRefreshRequested)
                _matrixRefreshThrottleTimer.Stop();
        }

        private void MinHoursCell_TargetUpdated(object sender, DataTransferEventArgs e)
        {
            
            
            if (sender is DependencyObject d)
            {
                var be = BindingOperations.GetBindingExpression(d, NumericUpDown.ValueProperty);
                be?.UpdateSource();
            }
        }
    }
}
