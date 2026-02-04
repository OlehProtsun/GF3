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
using WPFApp.Service;
using WPFApp.ViewModel.Container.Edit;
using WPFApp.ViewModel.Container.Edit.Helpers;
using WPFApp.ViewModel.Container.ScheduleEdit;
using WPFApp.ViewModel.Container.ScheduleEdit.Helpers;
using WPFApp.ViewModel.Dialogs;

namespace WPFApp.View.Container
{
    /// <summary>
    /// ContainerScheduleEditView — code-behind для UserControl редагування schedule.
    ///
    /// Важливо:
    /// - Ми НЕ переносимо бізнес-логіку сюди (вона вже у ViewModel та Infrastructure).
    /// - Тут лишається “UI glue”:
    ///   1) оптимізація DataGrid (virtualization)
    ///   2) керування динамічними колонками (перебудова при зміні схеми DataTable)
    ///   3) UX-деталі: paint режим (Alt+drag), pause refresh при відкритих ComboBox, тощо
    ///   4) коректне “відпускання” важких DataView/DataRowView при Unloaded
    ///
    /// Цей файл спеціально написаний так, щоб:
    /// - мінімізувати фрізи UI
    /// - уникати “старих контейнерів” DataGrid після зміни ItemsSource/columns
    /// - не плодити зайві алокації (особливо в частих подіях)
    /// </summary>
    public partial class ContainerScheduleEditView : UserControl
    {
        // ============================
        // 1) Посилання на ViewModel
        // ============================

        /// <summary>
        /// Поточний ViewModel, з яким пов’язаний цей view.
        /// Ми підписуємось на _vm.MatrixChanged і робимо “smart refresh” DataGrid.
        /// </summary>
        private ContainerScheduleEditViewModel? _vm;

        // ============================
        // 2) Стан редагування клітинки
        // ============================

        /// <summary>
        /// Попереднє значення клітинки до редагування (щоб повернути його, якщо введення невалідне).
        /// Заповнюється у PreparingCellForEdit.
        /// </summary>
        private string? _previousCellValue;

        // ============================
        // 3) Paint режим (Alt+drag)
        // ============================

        /// <summary>
        /// Чи зараз йде процес “малювання” (Alt+LMB + move).
        /// </summary>
        private bool _isPainting;

        /// <summary>
        /// Остання клітинка, яку ми пофарбували під час drag,
        /// щоб не застосовувати paint повторно на тій самій клітинці.
        /// </summary>
        private ScheduleMatrixCellRef? _lastPaintedCell;

        // ============================
        // 4) Smart refresh матриць + схема колонок
        // ============================

        /// <summary>
        /// Хеш схеми колонок schedule-матриці.
        /// Якщо хеш змінився — треба перебудувати колонки.
        /// Якщо не змінився — НЕ чіпаємо колонки (це швидше і без миготіння).
        /// </summary>
        private int? _scheduleSchemaHash;

        /// <summary>
        /// Хеш схеми колонок preview-матриці (availability preview).
        /// </summary>
        private int? _previewSchemaHash;

        // ============================
        // 5) Logging/Perf (обмежено)
        // ============================

        private readonly ILoggerService _logger = LoggerService.Instance;
        private DateTime _lastScrollLogUtc = DateTime.MinValue;

        // ============================
        // 6) UX: пауза refresh при відкритих ComboBox
        // ============================

        /// <summary>
        /// True, коли Shop ComboBox відкритий (потрібно для scroll-log/UX паузи).
        /// </summary>
        private bool _shopComboOpen;

        /// <summary>
        /// True, коли Availability ComboBox відкритий.
        /// </summary>
        private bool _availabilityComboOpen;

        /// <summary>
        /// Якщо true — ми не робимо RefreshMatricesSmart одразу при MatrixChanged,
        /// а лише ставимо прапорець _pendingMatrixRefresh.
        ///
        /// Це потрібно, щоб DataGrid не “фрізив” UI, коли користувач взаємодіє з ComboBox.
        /// </summary>
        private bool _suspendMatrixRefresh;

        /// <summary>
        /// Якщо під час паузи приходили MatrixChanged — запам’ятовуємо, що refresh потрібен.
        /// Коли ResumeMatrixRefresh() викликається — виконаємо refresh 1 раз.
        /// </summary>
        private bool _pendingMatrixRefresh;

        /// <summary>
        /// Coalescing: якщо MatrixChanged приходить “бурстом” (генерація/refresh/lookup),
        /// ми не робимо 10 refresh-ів, а робимо лише 1, запланований через Dispatcher.
        /// </summary>
        private bool _refreshQueued;

        public ContainerScheduleEditView()
        {
            InitializeComponent();

            // 1) Примусово вмикаємо virtualization (навіть якщо стилі її вимикають)
            ConfigureGridPerformance(dataGridScheduleMatrix);
            ConfigureGridPerformance(dataGridAvailabilityPreview);

            // 2) Загальні lifecycle події
            Loaded += ContainerScheduleEditView_Loaded;
            Unloaded += ContainerScheduleEditView_Unloaded;
            DataContextChanged += ContainerScheduleEditView_DataContextChanged;

            // 3) Події DataGrid для UX/логіки (виділення/paint/scroll)
            dataGridScheduleMatrix.PreparingCellForEdit += ScheduleMatrix_PreparingCellForEdit;
            dataGridScheduleMatrix.CurrentCellChanged += ScheduleMatrix_CurrentCellChanged;
            dataGridScheduleMatrix.SelectedCellsChanged += ScheduleMatrix_SelectedCellsChanged;

            dataGridScheduleMatrix.PreviewMouseLeftButtonDown += ScheduleMatrix_PreviewMouseLeftButtonDown;
            dataGridScheduleMatrix.MouseMove += ScheduleMatrix_MouseMove;
            dataGridScheduleMatrix.PreviewMouseLeftButtonUp += ScheduleMatrix_PreviewMouseLeftButtonUp;

            // ScrollChanged підключаємо як routed event handler
            dataGridScheduleMatrix.AddHandler(
                ScrollViewer.ScrollChangedEvent,
                new ScrollChangedEventHandler(ScheduleMatrix_ScrollChanged));
        }

        // =========================================================
        // Lifecycle: attach/detach VM
        // =========================================================

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

        /// <summary>
        /// AttachViewModel:
        /// - відписатися від старого _vm.MatrixChanged
        /// - підписатися на новий
        /// - синхронізувати ItemsSource і колонки, якщо схема вже є
        /// </summary>
        private void AttachViewModel(ContainerScheduleEditViewModel? viewModel)
        {
            if (ReferenceEquals(_vm, viewModel))
                return;

            // при переході на інший VM скидаємо локальні прапорці
            ResetInteractionState(commitPendingSelections: false);

            // ---- unsubscribe old vm ----
            if (_vm != null)
                _vm.MatrixChanged -= VmOnMatrixChanged;

            _vm = viewModel;

            // ---- subscribe new vm + init grid ----
            if (_vm != null)
            {
                _vm.MatrixChanged += VmOnMatrixChanged;

                // Будуємо колонки тільки якщо таблиця вже існує (не чіпаємо порожній стан)
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

                // Assign ItemsSource (DataView)
                dataGridScheduleMatrix.ItemsSource = _vm.ScheduleMatrix;
                dataGridAvailabilityPreview.ItemsSource = _vm.AvailabilityPreviewMatrix;
            }
            else
            {
                // якщо VM став null (наприклад навігація) — очищаємо гріди
                SwapItemsSource(dataGridScheduleMatrix, null);
                SwapItemsSource(dataGridAvailabilityPreview, null);
                dataGridScheduleMatrix.Columns.Clear();
                dataGridAvailabilityPreview.Columns.Clear();
                _scheduleSchemaHash = null;
                _previewSchemaHash = null;
            }
        }

        /// <summary>
        /// DetachViewModel:
        /// - відписка від подій VM
        /// - скасування фонового білду у VM
        /// - повне “відпускання” ItemsSource/Columns у DataGrid
        ///
        /// Це важливо для пам’яті, бо DataView/DataRowView можуть бути великими.
        /// </summary>
        private void DetachViewModel()
        {
            if (_vm == null) return;

            // перед закриттям view комітимо pending selection, щоб VM не лишився в "pending" стані
            ResetInteractionState(commitPendingSelections: true);

            _vm.MatrixChanged -= VmOnMatrixChanged;

            // якщо VM має CTS/Task.Run — зупиняємо
            _vm.CancelBackgroundWork();

            // ключове: відпускаємо важкі DataView/DataRowView зі сторони DataGrid
            SwapItemsSource(dataGridScheduleMatrix, null);
            SwapItemsSource(dataGridAvailabilityPreview, null);
            dataGridScheduleMatrix.Columns.Clear();
            dataGridAvailabilityPreview.Columns.Clear();

            _scheduleSchemaHash = null;
            _previewSchemaHash = null;

            _vm = null;
        }

        /// <summary>
        /// Скидає “локальний UI стан” (комбо, флаги refresh, лог).
        /// Якщо commitPendingSelections=true — просимо VM підтвердити pending вибори.
        /// </summary>
        private void ResetInteractionState(bool commitPendingSelections)
        {
            _shopComboOpen = false;
            _availabilityComboOpen = false;

            _suspendMatrixRefresh = false;
            _pendingMatrixRefresh = false;

            _refreshQueued = false;
            _lastScrollLogUtc = DateTime.MinValue;

            if (commitPendingSelections && _vm != null)
            {
                _vm.CommitPendingShopSelection();
                _vm.CommitPendingAvailabilitySelection();
            }
        }

        // =========================================================
        // DataGrid performance/refresh helpers
        // =========================================================

        /// <summary>
        /// Важливо:
        /// - WPF DataGrid інколи вимикає virtualization стилями
        /// - ми тут примусово її вмикаємо для великих таблиць (31 день × N працівників)
        /// </summary>
        private static void ConfigureGridPerformance(DataGrid grid)
        {
            if (grid == null) return;

            grid.EnableRowVirtualization = true;
            grid.EnableColumnVirtualization = true;

            VirtualizingPanel.SetIsVirtualizing(grid, true);
            VirtualizingPanel.SetVirtualizationMode(grid, VirtualizationMode.Recycling);

            // тримаємо CanContentScroll=true, інакше virtualization перестає працювати
            grid.SetValue(ScrollViewer.CanContentScrollProperty, true);

            // deferred scrolling інколи робить UX гіршим (підвисання під час thumb drag)
            grid.SetValue(ScrollViewer.IsDeferredScrollingEnabledProperty, false);

            // оптимізація scroll unit
            grid.SetValue(VirtualizingPanel.ScrollUnitProperty, ScrollUnit.Item);

            // невеликий кеш “на сторінку” для зменшення churn при прокрутці
            VirtualizingPanel.SetCacheLengthUnit(grid, VirtualizationCacheLengthUnit.Page);
            VirtualizingPanel.SetCacheLength(grid, new VirtualizationCacheLength(1));
        }

        /// <summary>
        /// ResetGridColumns:
        /// - спочатку ItemsSource=null (важливо!)
        /// - потім Columns.Clear()
        ///
        /// Так WPF відпускає старі binding-и та контейнери.
        /// </summary>
        private static void ResetGridColumns(DataGrid grid)
        {
            grid.ItemsSource = null;
            grid.Columns.Clear();
        }

        /// <summary>
        /// HardResetGrid:
        /// - зупиняє редагування
        /// - прибирає selection/current cell
        ///
        /// Це допомагає уникати ситуацій, коли DataGrid тримає посилання на DataRowView,
        /// і GC не може прибрати стару таблицю.
        /// </summary>
        private static void HardResetGrid(DataGrid grid)
        {
            grid.CancelEdit(DataGridEditingUnit.Cell);
            grid.CancelEdit(DataGridEditingUnit.Row);

            grid.UnselectAllCells();
            grid.UnselectAll();
            grid.SelectedItem = null;
            grid.CurrentCell = new DataGridCellInfo();
        }

        /// <summary>
        /// SwapItemsSource:
        /// - робить HardResetGrid
        /// - потім ItemsSource=null
        /// - потім ItemsSource=source
        ///
        /// Саме “null -> new” часто критично для коректного перевідображення.
        /// </summary>
        private static void SwapItemsSource(DataGrid grid, IEnumerable? source)
        {
            HardResetGrid(grid);
            grid.ItemsSource = null;
            grid.ItemsSource = source;
        }

        // =========================================================
        // VM -> View refresh (MatrixChanged)
        // =========================================================

        /// <summary>
        /// MatrixChanged може приходити часто (серіями).
        /// Ми:
        /// - якщо refresh “на паузі” -> ставимо _pendingMatrixRefresh
        /// - інакше -> плануємо рівно 1 refresh через Dispatcher (coalesce)
        /// </summary>
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

        /// <summary>
        /// Smart refresh:
        /// - перебудовує колонки тільки якщо схема змінилась
        /// - ItemsSource міняє тільки якщо DataView instance змінився
        /// - інакше не робить зайвих дій (менше фрізів/миготіння)
        /// </summary>
        private void RefreshMatricesSmart()
        {
            if (_vm is null)
                return;

            // -------------------------
            // Schedule matrix
            // -------------------------
            var scheduleTable = _vm.ScheduleMatrix?.Table;

            if (scheduleTable == null || scheduleTable.Columns.Count == 0)
            {
                // Якщо VM каже “матриці нема”, а грід ще щось тримає — очищаємо
                if (dataGridScheduleMatrix.ItemsSource != null || dataGridScheduleMatrix.Columns.Count > 0)
                {
                    ResetGridColumns(dataGridScheduleMatrix);
                    _scheduleSchemaHash = null;
                }
            }
            else
            {
                var scheduleHash = BuildSchemaHash(scheduleTable);

                // Якщо схема змінилася — повністю перебудовуємо колонки
                if (scheduleHash != _scheduleSchemaHash)
                {
                    ResetGridColumns(dataGridScheduleMatrix);

                    ScheduleMatrixColumnBuilder.BuildScheduleMatrixColumns(
                        scheduleTable,
                        dataGridScheduleMatrix,
                        isReadOnly: false);

                    _scheduleSchemaHash = scheduleHash;
                }

                // Якщо DataView instance змінився — перепризначаємо ItemsSource
                if (!ReferenceEquals(dataGridScheduleMatrix.ItemsSource, _vm.ScheduleMatrix))
                    SwapItemsSource(dataGridScheduleMatrix, _vm.ScheduleMatrix);
            }

            // -------------------------
            // Preview matrix
            // -------------------------
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

        /// <summary>
        /// Швидкий хеш схеми DataTable без побудови довгих string (менше алокацій).
        /// Враховує:
        /// - ColumnName
        /// - DataType
        ///
        /// Якщо у вас Caption використовується як “частина схеми”, можна додати і її,
        /// але це зайве для більшості випадків.
        /// </summary>
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

        // =========================================================
        // DataGrid selection -> VM selection
        // =========================================================

        private void ScheduleMatrix_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            // Запам’ятовуємо попереднє значення, щоб мати можливість “відкотитись”
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

            // Важливо: беремо SortMemberPath як “істинне” ім’я колонки.
            // Header може бути людино-читабельним і НЕ збігатися з columnName DataTable.
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

            // Попередньо виділяємо capacity, щоб менше алокацій
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

        // =========================================================
        // Paint mode (Alt + drag)
        // =========================================================

        private void ScheduleMatrix_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_vm is null) return;

            // Якщо Alt НЕ натиснутий — нічого не робимо (звичайне виділення)
            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                return;

            // Paint дозволений тільки якщо VM увімкнув режим
            if (_vm.ActivePaintMode == ContainerScheduleEditViewModel.SchedulePaintMode.None)
                return;

            // Визначаємо клітинку, по якій натиснули
            var cell = FindParent<DataGridCell>(e.OriginalSource as DependencyObject);
            if (cell == null) return;

            if (TryGetCellReference(cell, out var cellRef))
            {
                _vm.SelectedCellRef = cellRef;

                // Реальна логіка “як фарбувати” всередині VM (ApplyPaintToCell)
                _vm.ApplyPaintToCell(cellRef);

                _isPainting = true;
                _lastPaintedCell = cellRef;

                // Важливо: щоб DataGrid не “перетягував” selection під час paint
                e.Handled = true;
            }
        }

        private void ScheduleMatrix_MouseMove(object sender, MouseEventArgs e)
        {
            // paint працює тільки коли LMB затиснута і ми почали paint у MouseDown
            if (!_isPainting || _vm is null || e.LeftButton != MouseButtonState.Pressed)
                return;

            var cell = FindParent<DataGridCell>(e.OriginalSource as DependencyObject);
            if (cell == null) return;

            if (TryGetCellReference(cell, out var cellRef))
            {
                if (_lastPaintedCell.HasValue && _lastPaintedCell.Value.Equals(cellRef))
                    return;

                _vm.ApplyPaintToCell(cellRef);
                _lastPaintedCell = cellRef;
            }
        }

        private void ScheduleMatrix_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isPainting = false;
            _lastPaintedCell = null;
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

        // =========================================================
        // Edit cell commit (викликається з XAML: CellEditEnding="...")
        // =========================================================

        /// <summary>
        /// Обробник DataGrid.CellEditEnding:
        /// - ловить commit
        /// - бере введений raw текст
        /// - просить VM нормалізувати/валідувати (TryApplyMatrixEdit)
        /// - якщо помилка — відкатити старе значення і показати повідомлення
        ///
        /// Увага:
        /// - цей метод має бути підключений у XAML (CellEditEnding="ScheduleMatrix_CellEditEnding")
        ///   або в коді. Підключай лише в одному місці, щоб не було подвійного виклику.
        /// </summary>
        private void ScheduleMatrix_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (_vm is null) return;
            if (e.EditAction != DataGridEditAction.Commit) return;
            if (e.Row.Item is not DataRowView rowView) return;
            if (e.Column is not DataGridBoundColumn boundColumn) return;

            // Отримуємо “технічне” ім’я колонки з binding (наприклад [emp_12])
            var binding = boundColumn.Binding as Binding;
            var columnName = binding?.Path?.Path?.Trim('[', ']') ?? string.Empty;

            // Службові колонки не редагуємо
            if (columnName == ContainerScheduleEditViewModel.DayColumnName
                || columnName == ContainerScheduleEditViewModel.ConflictColumnName)
                return;

            // Беремо день
            if (!int.TryParse(rowView[ContainerScheduleEditViewModel.DayColumnName]?.ToString(), out var day))
                return;

            // raw введення: або з TextBox, або fallback з rowView
            var raw = (e.EditingElement as TextBox)?.Text ?? rowView[columnName]?.ToString() ?? string.Empty;

            // VM повертає normalized і error (якщо невалідно)
            if (!_vm.TryApplyMatrixEdit(columnName, day, raw, out var normalized, out var error))
            {
                // відкат
                rowView[columnName] = _previousCellValue ?? ContainerScheduleEditViewModel.EmptyMark;

                // показ помилки
                if (!string.IsNullOrWhiteSpace(error))
                    CustomMessageBox.Show("Error", error, CustomMessageBoxIcon.Error, okText: "OK");

                return;
            }

            // commit нормалізованого значення
            rowView[columnName] = normalized;
        }

        // =========================================================
        // Opened schedules list / buttons
        // =========================================================

        private async void OpenedSchedulesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_vm is null) return;

            if (sender is ListBox lb && lb.SelectedItem is ScheduleBlockViewModel block)
            {
                // якщо той самий блок — нічого не робимо
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

        // =========================================================
        // ComboBox UX: pause/resume refresh
        // =========================================================

        /// <summary>
        /// Пауза ДО того, як DataGrid закомітить поточну клітинку (CellEditEnding),
        /// щоб не було фрізів при різкій зміні selection + перебудові матриці.
        /// </summary>
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
            // UX: коли дропдаун відкритий — не скролимо список під ним (запобігає “стрибанню”)
            if (sender is ComboBox comboBox && comboBox.IsDropDownOpen)
                e.Handled = true;
        }

        private void PauseMatrixRefresh()
        {
            _suspendMatrixRefresh = true;
        }

        private void ResumeMatrixRefresh()
        {
            _suspendMatrixRefresh = false;

            // Якщо під час паузи були MatrixChanged — виконуємо 1 refresh після закриття ComboBox
            if (_pendingMatrixRefresh)
            {
                _pendingMatrixRefresh = false;
                Dispatcher.BeginInvoke(new Action(RefreshMatricesSmart), DispatcherPriority.Background);
            }
        }

        // =========================================================
        // Perf logging (обмежено)
        // =========================================================

        private void ScheduleMatrix_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Лог тільки коли відкриті комбобокси (вузький сценарій діагностики)
            if (!_shopComboOpen && !_availabilityComboOpen)
                return;

            var now = DateTime.UtcNow;
            if ((now - _lastScrollLogUtc).TotalMilliseconds < 500)
                return;

            _lastScrollLogUtc = now;
            _logger.LogPerf(
                "ScheduleGrid",
                $"ScrollChanged: V={e.VerticalOffset:F2} H={e.HorizontalOffset:F2} ΔV={e.VerticalChange:F2} ΔH={e.HorizontalChange:F2}");
        }

        private void MinHoursCell_TargetUpdated(object sender, DataTransferEventArgs e)
        {
            // Важливо: ValidationRule не спрацьовує на старті,
            // тому примусово “проганяємо” source update — отримаємо Validation.HasError одразу.
            if (sender is DependencyObject d)
            {
                var be = BindingOperations.GetBindingExpression(d, NumericUpDown.ValueProperty);
                be?.UpdateSource();
            }
        }
    }
}
