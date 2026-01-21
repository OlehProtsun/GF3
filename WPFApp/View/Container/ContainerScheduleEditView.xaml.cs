using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WPFApp.Infrastructure;
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

        private static int _nextViewId;
        private readonly int _viewId = Interlocked.Increment(ref _nextViewId);

        private long _attachCalls;
        private long _detachCalls;

        private long _matrixChangedReceived;
        private long _matrixChangedQueued;
        private long _matrixChangedExecuted;

        private long _refreshSmartRuns;
        private long _scheduleRebuildCols;
        private long _scheduleSwapSrc;
        private long _previewRebuildCols;
        private long _previewSwapSrc;

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
            MatrixRefreshDiagnostics.RecordUiRefresh(
    "EditView.DataContextChanged",
    $"view={_viewId} newVm={VmId(DataContext as ContainerScheduleEditViewModel)}");

            AttachViewModel(DataContext as ContainerScheduleEditViewModel);

        }

        private void ContainerScheduleEditView_Loaded(object sender, RoutedEventArgs e)
        {
            MatrixRefreshDiagnostics.RecordUiRefresh(
            "EditView.Loaded",
            $"view={_viewId} vm={VmId(DataContext as ContainerScheduleEditViewModel)}");

            AttachViewModel(DataContext as ContainerScheduleEditViewModel);
        }

        private void ContainerScheduleEditView_Unloaded(object sender, RoutedEventArgs e)
        {
            MatrixRefreshDiagnostics.RecordUiRefresh(
    "EditView.Unloaded",
    $"view={_viewId} vm={VmId(_vm)}");

            DetachViewModel();
        }

        private void AttachViewModel(ContainerScheduleEditViewModel? viewModel)
        {
            if (ReferenceEquals(_vm, viewModel))
            {
                MatrixRefreshDiagnostics.RecordUiRefresh(
                    "EditView.AttachViewModel: SKIP (same vm)",
                    $"view={_viewId} vm={VmId(viewModel)} colsS={dataGridScheduleMatrix.Columns.Count} colsP={dataGridAvailabilityPreview.Columns.Count}");
                return;
            }

            var snap = MatrixRefreshDiagnostics.AllocSnapshot();
            var callNo = Interlocked.Increment(ref _attachCalls);

            int oldVmId = VmId(_vm);
            int newVmId = VmId(viewModel);

            MatrixRefreshDiagnostics.RecordUiRefresh(
                "EditView.AttachViewModel: ENTER",
                $"view={_viewId} call={callNo} oldVm={oldVmId} newVm={newVmId} " +
                $"{GridPerfSnapshot(dataGridScheduleMatrix)} | {GridPerfSnapshot(dataGridAvailabilityPreview)}");

            // ---- unsubscribe old vm ----
            if (_vm != null)
            {
                int subsBeforeUnsub = -1;
                try { subsBeforeUnsub = _vm.GetMatrixChangedSubscriberCount(); } catch { /* ignore */ }

                MatrixRefreshDiagnostics.RecordUiRefresh(
                    "EditView.AttachViewModel: before-unsubscribe",
                    $"view={_viewId} oldVm={oldVmId} subs={subsBeforeUnsub}");

                _vm.MatrixChanged -= VmOnMatrixChanged;
                MatrixRefreshDiagnostics.Count(
                    "EditView.MatrixChanged-=",
                    log: true,
                    extra: $"view={_viewId} vm={oldVmId}");

                int subsAfterUnsub = -1;
                try { subsAfterUnsub = _vm.GetMatrixChangedSubscriberCount(); } catch { /* ignore */ }

                MatrixRefreshDiagnostics.RecordUiRefresh(
                    "EditView.AttachViewModel: after-unsubscribe",
                    $"view={_viewId} oldVm={oldVmId} subs={subsAfterUnsub}");
            }

            // ---- assign new vm ----
            _vm = viewModel;

            MatrixRefreshDiagnostics.RecordUiRefresh(
                "EditView.AttachViewModel: assigned vm",
                $"view={_viewId} vm={(viewModel == null ? "null" : "!=null")} vmId={VmId(_vm)}");

            // ---- subscribe new vm + init grid ----
            if (_vm != null)
            {
                int subsBeforeSub = -1;
                try { subsBeforeSub = _vm.GetMatrixChangedSubscriberCount(); } catch { /* ignore */ }

                MatrixRefreshDiagnostics.RecordUiRefresh(
                    "EditView.AttachViewModel: before-subscribe",
                    $"view={_viewId} vm={newVmId} subs={subsBeforeSub}");

                _vm.MatrixChanged += VmOnMatrixChanged;
                MatrixRefreshDiagnostics.Count(
                    "EditView.MatrixChanged+=",
                    log: true,
                    extra: $"view={_viewId} vm={newVmId}");

                int subsAfterSub = -1;
                try { subsAfterSub = _vm.GetMatrixChangedSubscriberCount(); } catch { /* ignore */ }

                MatrixRefreshDiagnostics.RecordUiRefresh(
                    "EditView.AttachViewModel: after-subscribe",
                    $"view={_viewId} vm={newVmId} subs={subsAfterSub}");

                // Build columns only if we already have a schema.
                int scheduleCols = 0, previewCols = 0;

                if (_vm.ScheduleMatrix?.Table != null)
                {
                    scheduleCols = _vm.ScheduleMatrix.Table.Columns.Count;
                    ScheduleMatrixColumnBuilder.BuildScheduleMatrixColumns(
                        _vm.ScheduleMatrix.Table,
                        dataGridScheduleMatrix,
                        isReadOnly: false);
                }

                if (_vm.AvailabilityPreviewMatrix?.Table != null)
                {
                    previewCols = _vm.AvailabilityPreviewMatrix.Table.Columns.Count;
                    ScheduleMatrixColumnBuilder.BuildScheduleMatrixColumns(
                        _vm.AvailabilityPreviewMatrix.Table,
                        dataGridAvailabilityPreview,
                        isReadOnly: true);
                }

                // Assign ItemsSource
                dataGridScheduleMatrix.ItemsSource = _vm.ScheduleMatrix;
                dataGridAvailabilityPreview.ItemsSource = _vm.AvailabilityPreviewMatrix;

                MatrixRefreshDiagnostics.RecordUiRefresh(
                    "EditView.AttachViewModel: ItemsSource set",
                    $"view={_viewId} vm={newVmId} " +
                    $"scheduleCols={scheduleCols} previewCols={previewCols} " +
                    $"scheduleSrc={MatrixRefreshDiagnostics.IdOf(dataGridScheduleMatrix.ItemsSource)} " +
                    $"previewSrc={MatrixRefreshDiagnostics.IdOf(dataGridAvailabilityPreview.ItemsSource)}");
            }
            else
            {
                // Optional: keep grid stable if vm becomes null (you can comment this out if not desired)
                // dataGridScheduleMatrix.ItemsSource = null;
                // dataGridAvailabilityPreview.ItemsSource = null;
                MatrixRefreshDiagnostics.RecordUiRefresh(
                    "EditView.AttachViewModel: vm is null",
                    $"view={_viewId} call={callNo}");
            }

            MatrixRefreshDiagnostics.RecordAllocDelta(
                "EditView.AttachViewModel: EXIT",
                snap,
                $"view={_viewId} call={callNo} vm={VmId(_vm)}");
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

            MatrixRefreshDiagnostics.Count(
                "EditView.SwapItemsSource",
                log: true,
                extra: $"grid={grid.Name} src={MatrixRefreshDiagnostics.IdOf(source)} cols={grid.Columns.Count}");
        }


        private void DetachViewModel()
        {
            var snap = MatrixRefreshDiagnostics.AllocSnapshot();
            var callNo = Interlocked.Increment(ref _detachCalls);

            MatrixRefreshDiagnostics.RecordUiRefresh(
                "EditView.DetachViewModel: ENTER",
                $"view={_viewId} call={callNo} vm={VmId(_vm)}");

            MatrixRefreshDiagnostics.RecordUiRefresh("EditView.DetachViewModel: start");

            if (_vm == null) return;

            MatrixRefreshDiagnostics.RecordUiRefresh(
    "EditView.DetachViewModel: before-unsubscribe",
    $"view={_viewId} vm={VmId(_vm)} subs={_vm.GetMatrixChangedSubscriberCount()}");

            _vm.MatrixChanged -= VmOnMatrixChanged;
            MatrixRefreshDiagnostics.Count(
                "EditView.MatrixChanged-=",
                log: true,
                extra: $"view={_viewId} vm={VmId(_vm)}");
            _vm.CancelBackgroundWork();

            // !!! ключове: відпускаємо важкі DataView/DataRowView зі сторони DataGrid
            SwapItemsSource(dataGridScheduleMatrix, null);
            SwapItemsSource(dataGridAvailabilityPreview, null);
            dataGridScheduleMatrix.Columns.Clear();
            dataGridAvailabilityPreview.Columns.Clear();
            MatrixRefreshDiagnostics.RecordUiRefresh(
    "EditView.DetachViewModel: after-clear",
    $"view={_viewId} {GridPerfSnapshot(dataGridScheduleMatrix)} | {GridPerfSnapshot(dataGridAvailabilityPreview)}");
            MatrixRefreshDiagnostics.RecordAllocDelta("EditView.DetachViewModel: EXIT", snap, $"view={_viewId} call={callNo}");

            MatrixRefreshDiagnostics.RecordUiRefresh("EditView.DetachViewModel: cleared grids");


            _scheduleSchemaSig = null;
            _previewSchemaSig = null;
            _vm = null;
        }


        private void VmOnMatrixChanged(object? sender, EventArgs e)
        {
            var recv = Interlocked.Increment(ref _matrixChangedReceived);
            int vmId = VmId(_vm);
            var stack = MatrixRefreshDiagnostics.ShortStack(skipFrames: 2);

            MatrixRefreshDiagnostics.Count(
                "EditView.VmOnMatrixChanged",
                log: true,
                extra: stack == null ? $"view={_viewId} vm={vmId}" : $"view={_viewId} vm={vmId} stack={stack}");

            MatrixRefreshDiagnostics.RecordUiRefresh(
                "EditView.VmOnMatrixChanged: RECEIVED",
                $"view={_viewId} vm={vmId} recv={recv} refreshQueued={_refreshQueued}");

            if (_vm is null) return;

            if (_refreshQueued) return;
            _refreshQueued = true;

            var queued = Interlocked.Increment(ref _matrixChangedQueued);

            MatrixRefreshDiagnostics.RecordUiRefresh(
                "EditView.VmOnMatrixChanged: QUEUED",
                $"view={_viewId} vm={vmId} queued={queued}");

            Dispatcher.BeginInvoke(new Action(() =>
            {
                var snap = MatrixRefreshDiagnostics.AllocSnapshot();
                var exec = Interlocked.Increment(ref _matrixChangedExecuted);

                _refreshQueued = false;

                MatrixRefreshDiagnostics.RecordUiRefresh(
                    "EditView.VmOnMatrixChanged: EXECUTE",
                    $"view={_viewId} vm={vmId} exec={exec} " +
                    $"{GridPerfSnapshot(dataGridScheduleMatrix)}");

                RefreshMatricesSmart();

                MatrixRefreshDiagnostics.RecordAllocDelta(
                    "EditView.VmOnMatrixChanged: EXECUTE_DONE",
                    snap,
                    $"view={_viewId} vm={vmId} exec={exec}");

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
            var snap = MatrixRefreshDiagnostics.AllocSnapshot();
            var sw = Stopwatch.StartNew();
            var run = Interlocked.Increment(ref _refreshSmartRuns);

            int vmId = VmId(_vm);

            bool sRebuild = false, sSwap = false, pRebuild = false, pSwap = false;

            string oldScheduleSig = _scheduleSchemaSig ?? "<null>";
            string oldPreviewSig = _previewSchemaSig ?? "<null>";

            int scheduleSrcBefore = MatrixRefreshDiagnostics.IdOf(dataGridScheduleMatrix.ItemsSource);
            int previewSrcBefore = MatrixRefreshDiagnostics.IdOf(dataGridAvailabilityPreview.ItemsSource);

            MatrixRefreshDiagnostics.RecordUiRefresh(
                "EditView.RefreshMatricesSmart: ENTER",
                $"view={_viewId} vm={vmId} run={run} " +
                $"{GridPerfSnapshot(dataGridScheduleMatrix)} | {GridPerfSnapshot(dataGridAvailabilityPreview)} " +
                $"oldSig[s]={oldScheduleSig} oldSig[p]={oldPreviewSig} " +
                $"srcBefore[s]={scheduleSrcBefore} srcBefore[p]={previewSrcBefore}");

            try
            {
                if (_vm is null)
                {
                    MatrixRefreshDiagnostics.RecordUiRefresh(
                        "EditView.RefreshMatricesSmart: VM_NULL",
                        $"view={_viewId} run={run}");
                    return;
                }

                // -----------------------------
                // SCHEDULE GRID
                // -----------------------------
                var scheduleTable = _vm.ScheduleMatrix?.Table;

                if (scheduleTable == null || scheduleTable.Columns.Count == 0)
                {
                    if (dataGridScheduleMatrix.ItemsSource != null || dataGridScheduleMatrix.Columns.Count > 0)
                    {
                        ResetGridColumns(dataGridScheduleMatrix);
                        _scheduleSchemaSig = null;
                        sSwap = true;
                        Interlocked.Increment(ref _scheduleSwapSrc);

                        MatrixRefreshDiagnostics.RecordUiRefresh(
                            "EditView.ScheduleGrid: reset columns (empty table)",
                            $"view={_viewId} run={run}");
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

                        sRebuild = true;
                        Interlocked.Increment(ref _scheduleRebuildCols);

                        MatrixRefreshDiagnostics.RecordUiRefresh(
                            "EditView.ScheduleGrid: rebuild columns",
                            $"view={_viewId} run={run} newSig={scheduleSig} oldSig={oldScheduleSig} cols={scheduleTable.Columns.Count}");
                    }

                    if (!ReferenceEquals(dataGridScheduleMatrix.ItemsSource, _vm.ScheduleMatrix))
                    {
                        SwapItemsSource(dataGridScheduleMatrix, _vm.ScheduleMatrix);
                        sSwap = true;
                        Interlocked.Increment(ref _scheduleSwapSrc);

                        MatrixRefreshDiagnostics.RecordUiRefresh(
                            "EditView.ScheduleGrid: swap ItemsSource",
                            $"view={_viewId} run={run} newSrc={MatrixRefreshDiagnostics.IdOf(_vm.ScheduleMatrix)}");
                    }
                }

                // -----------------------------
                // PREVIEW GRID
                // -----------------------------
                var previewTable = _vm.AvailabilityPreviewMatrix?.Table;

                if (previewTable == null || previewTable.Columns.Count == 0)
                {
                    if (dataGridAvailabilityPreview.ItemsSource != null || dataGridAvailabilityPreview.Columns.Count > 0)
                    {
                        ResetGridColumns(dataGridAvailabilityPreview);
                        _previewSchemaSig = null;
                        pSwap = true;
                        Interlocked.Increment(ref _previewSwapSrc);

                        MatrixRefreshDiagnostics.RecordUiRefresh(
                            "EditView.PreviewGrid: reset columns (empty table)",
                            $"view={_viewId} run={run}");
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

                        pRebuild = true;
                        Interlocked.Increment(ref _previewRebuildCols);

                        MatrixRefreshDiagnostics.RecordUiRefresh(
                            "EditView.PreviewGrid: rebuild columns",
                            $"view={_viewId} run={run} newSig={previewSig} oldSig={oldPreviewSig} cols={previewTable.Columns.Count}");
                    }

                    if (!ReferenceEquals(dataGridAvailabilityPreview.ItemsSource, _vm.AvailabilityPreviewMatrix))
                    {
                        SwapItemsSource(dataGridAvailabilityPreview, _vm.AvailabilityPreviewMatrix);
                        pSwap = true;
                        Interlocked.Increment(ref _previewSwapSrc);

                        MatrixRefreshDiagnostics.RecordUiRefresh(
                            "EditView.PreviewGrid: swap ItemsSource",
                            $"view={_viewId} run={run} newSrc={MatrixRefreshDiagnostics.IdOf(_vm.AvailabilityPreviewMatrix)}");
                    }
                }

                MatrixRefreshDiagnostics.RecordUiRefresh(
                    "EditView.RefreshMatricesSmart: MID",
                    $"view={_viewId} run={run} " +
                    $"did[sRebuild]={sRebuild} did[sSwap]={sSwap} did[pRebuild]={pRebuild} did[pSwap]={pSwap} " +
                    $"cnt[sRebuild]={_scheduleRebuildCols} cnt[sSwap]={_scheduleSwapSrc} " +
                    $"cnt[pRebuild]={_previewRebuildCols} cnt[pSwap]={_previewSwapSrc}");
            }
            finally
            {
                sw.Stop();

                int scheduleSrcAfter = MatrixRefreshDiagnostics.IdOf(dataGridScheduleMatrix.ItemsSource);
                int previewSrcAfter = MatrixRefreshDiagnostics.IdOf(dataGridAvailabilityPreview.ItemsSource);

                MatrixRefreshDiagnostics.RecordUiRefresh(
                    "EditView.RefreshMatricesSmart: EXIT",
                    $"view={_viewId} vm={VmId(_vm)} run={run} durMs={sw.Elapsed.TotalMilliseconds:0} " +
                    $"did[sRebuild]={sRebuild} did[sSwap]={sSwap} did[pRebuild]={pRebuild} did[pSwap]={pSwap} " +
                    $"sigNow[s]={_scheduleSchemaSig ?? "<null>"} sigNow[p]={_previewSchemaSig ?? "<null>"} " +
                    $"srcAfter[s]={scheduleSrcAfter} srcAfter[p]={previewSrcAfter} " +
                    $"{GridPerfSnapshot(dataGridScheduleMatrix)} | {GridPerfSnapshot(dataGridAvailabilityPreview)}");

                MatrixRefreshDiagnostics.RecordAllocDelta(
                    "EditView.RefreshMatricesSmart: ALLOC",
                    snap,
                    $"view={_viewId} run={run} durMs={sw.Elapsed.TotalMilliseconds:0} " +
                    $"srcBefore[s]={scheduleSrcBefore} srcAfter[s]={scheduleSrcAfter} " +
                    $"srcBefore[p]={previewSrcBefore} srcAfter[p]={previewSrcAfter}");
            }
        }


        private static int VmId(ContainerScheduleEditViewModel? vm) => MatrixRefreshDiagnostics.IdOf(vm);

        private string GridPerfSnapshot(DataGrid g)
        {
            try
            {
                bool isVirt = VirtualizingPanel.GetIsVirtualizing(g);
                var mode = VirtualizingPanel.GetVirtualizationMode(g);
                bool canScroll = (bool)g.GetValue(ScrollViewer.CanContentScrollProperty);

                int cols = g.Columns?.Count ?? 0;
                int items = g.Items?.Count ?? 0;

                return $"grid='{g.Name}' virt={isVirt} mode={mode} canContentScroll={canScroll} " +
                       $"rowVirt={g.EnableRowVirtualization} colVirt={g.EnableColumnVirtualization} " +
                       $"cols={cols} items={items}";
            }
            catch (Exception ex)
            {
                return $"gridPerfSnap failed: {ex.GetType().Name}: {ex.Message}";
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
