using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using WPFApp.Applications.Matrix.Availability;
using WPFApp.View.Availability.Helpers;
using WPFApp.ViewModel.Availability.Edit;
using WPFApp.ViewModel.Availability.Helpers;

namespace WPFApp.View.Availability
{
    /// <summary>
    /// AvailabilityEditView.xaml.cs
    ///
    /// Принцип:
    /// - Мінімум логіки в code-behind:
    ///   * побудова колонок DataGrid з DataTable (бо колонки динамічні по працівниках)
    ///   * обробка hotkeys/binds при натисканні клавіш у матриці
    ///   * обмеження вводу (Month/Year: цифри, clamp)
    ///   * захоплення KeyGesture у таблиці binds
    ///
    /// Валідація/нормалізація availability-кодів:
    /// - НЕ робимо тут regex-валідацію інтервалів
    /// - використовуємо AvailabilityMatrixEngine.TryNormalizeCell, щоб не мати “дві правди”
    /// </summary>
    public partial class AvailabilityEditView : UserControl
    {
        // Поточний VM, на який підписані.
        private AvailabilityEditViewModel? _vm;

        public AvailabilityEditView()
        {
            InitializeComponent();

            // Підписки на lifecycle, щоб коректно attach/detach VM.
            DataContextChanged += OnDataContextChanged;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            // Матриця availability:
            // - PreviewKeyDown перехоплює натиснення клавіш для bind apply (до того як TextBox вставить символ)
            dataGridAvailabilityDays.PreviewKeyDown += DataGridAvailabilityDays_PreviewKeyDown;

            // Binds grid:
            dataGridBinds.AutoGeneratingColumn += DataGridBinds_AutoGeneratingColumn;
            dataGridBinds.PreviewKeyDown += DataGridBinds_PreviewKeyDown;
            dataGridBinds.RowEditEnding += DataGridBinds_RowEditEnding;

            // Search textbox: Enter -> SearchEmployeeCommand
            textBoxSearchValueFromAvailabilityEdit.KeyDown += TextBoxSearchValueFromAvailabilityEdit_KeyDown;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AttachViewModel(DataContext as AvailabilityEditViewModel);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AttachViewModel(DataContext as AvailabilityEditViewModel);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DetachViewModel();
        }

        private void AttachViewModel(AvailabilityEditViewModel? viewModel)
        {
            // 1) Якщо той самий інстанс — нічого не робимо.
            if (ReferenceEquals(_vm, viewModel))
                return;

            // 2) Від’єднуємо попередній.
            DetachViewModel();

            // 3) Запам’ятовуємо новий.
            _vm = viewModel;

            // 4) Якщо VM нема — виходимо.
            if (_vm is null)
                return;

            // 5) Підписка на MatrixChanged:
            //    коли VM перебудував DataTable (додав/прибрав employee колонки або rows),
            //    view має перебудувати DataGridColumns.
            _vm.MatrixChanged += VmOnMatrixChanged;

            // 6) Перший build колонок одразу.
            AvailabilityMatrixGridBuilder.BuildEditable(_vm.AvailabilityDays.Table, dataGridAvailabilityDays);
        }

        private void DetachViewModel()
        {
            // 1) Якщо VM нема — нічого.
            if (_vm is null)
                return;

            // 2) Відписка.
            _vm.MatrixChanged -= VmOnMatrixChanged;

            // 3) Обнуляємо.
            _vm = null;
        }

        private void VmOnMatrixChanged(object? sender, EventArgs e)
        {
            // 1) Якщо VM вже нема — виходимо.
            if (_vm is null)
                return;

            // 2) Маршалимо у UI thread (стійкість).
            if (!Dispatcher.CheckAccess())
            {
                _ = Dispatcher.BeginInvoke(new Action(() => VmOnMatrixChanged(sender, e)));
                return;
            }

            // 3) Перебудова колонок.
            AvailabilityMatrixGridBuilder.BuildEditable(_vm.AvailabilityDays.Table, dataGridAvailabilityDays);
        }

        // =========================================================
        // Employees search
        // =========================================================

        private void TextBoxSearchValueFromAvailabilityEdit_KeyDown(object sender, KeyEventArgs e)
        {
            // 1) Лише Enter.
            if (e.Key != Key.Enter)
                return;

            // 2) VM.
            if (DataContext is not AvailabilityEditViewModel vm)
                return;

            // 3) Execute SearchEmployeeCommand.
            if (vm.SearchEmployeeCommand.CanExecute(null))
                vm.SearchEmployeeCommand.Execute(null);

            e.Handled = true;
        }

        // =========================================================
        // Binds DataGrid
        // =========================================================

        private async void DataGridBinds_RowEditEnding(object? sender, DataGridRowEditEndingEventArgs e)
        {
            // 1) Цікавить лише commit.
            if (e.EditAction != DataGridEditAction.Commit)
                return;

            // 2) VM має бути підключений.
            if (_vm is null)
                return;

            // 3) Рядок має бути BindRow.
            if (e.Row.Item is not BindRow row)
                return;

            // 4) Upsert (Create/Update) bind-а.
            //    Це async void handler (бо event), але тут це нормальна практика для WPF events.
            await _vm.UpsertBindAsync(row);
        }

        private void DataGridBinds_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            // Якщо у XAML стоїть AutoGenerateColumns=true — ми можемо “підкрутити” поведінку колонок.
            // Для Key: робимо ReadOnly, бо:
            // - Key заповнюємо через “захоплення” hotkey у PreviewKeyDown
            // - вручну вводити Key не хочемо (щоб уникнути різних форматів)
            if (e.PropertyName == nameof(BindRow.Key))
                e.Column.IsReadOnly = true;
        }

        private void DataGridBinds_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 1) Без VM — нема логіки.
            if (_vm is null)
                return;

            // 2) ENTER: комітимо клітинку+рядок (RowEditEnding викличе Upsert).
            if (e.Key == Key.Enter)
            {
                dataGridBinds.CommitEdit(DataGridEditingUnit.Cell, true);
                dataGridBinds.CommitEdit(DataGridEditingUnit.Row, true);
                e.Handled = true;
                return;
            }

            // 3) Якщо зараз редагуємо TextBox — не перехоплюємо (щоб користувач міг вводити значення).
            if (IsCurrentCellEditing(dataGridBinds))
                return;

            // 4) Переконуємось, що поточна колонка — саме Key (бо ми “захоплюємо” hotkey тільки туди).
            if (dataGridBinds.CurrentColumn is not DataGridBoundColumn boundColumn)
                return;

            if (boundColumn.Binding is not Binding b)
                return;

            if (b.Path?.Path != nameof(BindRow.Key))
                return;

            // 5) Якщо модифікаторів нема — не захоплюємо.
            //    Це твоя початкова логіка: хоткей = комбінація з Ctrl/Alt/Shift/Win.
            if (Keyboard.Modifiers == ModifierKeys.None)
                return;

            // 6) Не перехоплюємо навігацію.
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

            // 7) Не ламаємо стандартні Ctrl shortcuts (Ctrl+C/V/X/Z/Y/A).
            if (AvailabilityViewInputHelper.IsCommonEditorShortcut(e.Key, Keyboard.Modifiers))
                return;

            // 8) У WPF, коли натиснуто Alt+key, інколи приходить Key.System + SystemKey.
            var key = (e.Key == Key.System) ? e.SystemKey : e.Key;

            // 9) Форматуємо у “людський” вигляд (Ctrl+M).
            var keyText = _vm.FormatKeyGesture(key, Keyboard.Modifiers);

            // 10) Якщо форматування не вдалося — виходимо.
            if (string.IsNullOrWhiteSpace(keyText))
                return;

            // 11) Записуємо в поточний BindRow.
            if (dataGridBinds.CurrentItem is BindRow row)
            {
                row.Key = keyText;

                // 12) Комітимо клітинку, щоб RowEditEnding міг спрацювати.
                dataGridBinds.CommitEdit(DataGridEditingUnit.Cell, true);
            }

            // 13) Handled, щоб key не пішов далі.
            e.Handled = true;
        }

        // =========================================================
        // Availability matrix: binds apply on key press
        // =========================================================

        private void DataGridAvailabilityDays_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 1) Без VM — нема логіки.
            if (_vm is null)
                return;

            // 2) Має бути column з Binding, щоб дістати columnName.
            if (dataGridAvailabilityDays.CurrentColumn is not DataGridBoundColumn boundColumn)
                return;

            if (boundColumn.Binding is not Binding binding)
                return;

            // 3) Binding.Path.Path має вигляд "[emp_12]" або "[DayOfMonth]".
            //    Забираємо квадратні дужки.
            var columnName = (binding.Path?.Path ?? string.Empty).Trim('[', ']');

            // 4) Порожній columnName — вихід.
            if (string.IsNullOrWhiteSpace(columnName))
                return;

            // 5) Day column не редагуємо і не застосовуємо binds.
            if (columnName == AvailabilityMatrixEngine.DayColumnName)
                return;

            // 6) Не перехоплюємо навігаційні клавіші та службові клавіші.
            //    Enter — окремо обробляє DataGrid (commit/move), тому не чіпаємо.
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

            // 7) Не ламаємо стандартні Ctrl shortcuts.
            if (AvailabilityViewInputHelper.IsCommonEditorShortcut(e.Key, Keyboard.Modifiers))
                return;

            // 8) Визначаємо “токен” клавіші для bind lookup:
            //    - якщо є modifiers — беремо Ctrl+M (через FormatKeyGesture)
            //    - якщо modifiers немає — беремо "A" або "1" (через helper, щоб D1 став "1")
            var key = (e.Key == Key.System) ? e.SystemKey : e.Key;

            string rawKeyToken;

            if (Keyboard.Modifiers != ModifierKeys.None)
            {
                // 8.1) Комбінація — форматована строка.
                rawKeyToken = _vm.FormatKeyGesture(key, Keyboard.Modifiers) ?? string.Empty;

                // Якщо не вдалося — вихід.
                if (string.IsNullOrWhiteSpace(rawKeyToken))
                    return;
            }
            else
            {
                // 8.2) Одиночна клавіша — токен (A / 1 / NumPad3 / ...).
                rawKeyToken = AvailabilityViewInputHelper.KeyToBindToken(key);
            }

            // 9) Питаємо VM: чи існує bind для цього key.
            //    VM всередині нормалізує ключ через owner.TryNormalizeKey,
            //    тому ми можемо передати “людський” токен.
            if (!_vm.TryGetBindValue(rawKeyToken, out var bindValue))
                return;

            // 10) Якщо клітинка зараз редагується (TextBox активний),
            //     комітимо edit, щоб символ не вставився у TextBox перед застосуванням bind-а.
            if (IsCurrentCellEditing(dataGridAvailabilityDays))
                dataGridAvailabilityDays.CommitEdit(DataGridEditingUnit.Cell, true);

            // 11) Визначаємо rowIndex поточного item.
            var rowIndex = dataGridAvailabilityDays.Items.IndexOf(dataGridAvailabilityDays.CurrentItem);

            // 12) Просимо VM застосувати bind до клітинки (columnName,rowIndex).
            //     VM може повернути nextRowIndex для “автопереходу вниз”.
            if (!_vm.TryApplyBindToCell(columnName, rowIndex, bindValue, out var nextRowIndex))
                return;

            // 13) Комітимо cell і row, щоб DataTable отримав значення і валідація відпрацювала.
            dataGridAvailabilityDays.CommitEdit(DataGridEditingUnit.Cell, true);
            dataGridAvailabilityDays.CommitEdit(DataGridEditingUnit.Row, true);

            // 14) Якщо VM запропонував nextRowIndex — переміщаємо курсор вниз.
            if (nextRowIndex is int next && next >= 0 && next < dataGridAvailabilityDays.Items.Count)
            {
                var nextItem = dataGridAvailabilityDays.Items[next];

                // 14.1) Скролимо до елемента.
                dataGridAvailabilityDays.ScrollIntoView(nextItem, boundColumn);

                // 14.2) Ставимо поточну клітинку.
                dataGridAvailabilityDays.CurrentCell = new DataGridCellInfo(nextItem, boundColumn);

                // 14.3) Очищаємо SelectedCells і ставимо лише потрібну клітинку (UX: видно, де курсор).
                dataGridAvailabilityDays.SelectedCells.Clear();
                dataGridAvailabilityDays.SelectedCells.Add(new DataGridCellInfo(nextItem, boundColumn));

                // 14.4) Починаємо редагування через Dispatcher, щоб уникнути “invalid state” у деяких шаблонах.
                dataGridAvailabilityDays.Dispatcher.BeginInvoke(new Action(() =>
                {
                    dataGridAvailabilityDays.Focus();
                    dataGridAvailabilityDays.BeginEdit();
                }), System.Windows.Threading.DispatcherPriority.Background);
            }

            // 15) Перехопили клавішу — далі не передаємо.
            e.Handled = true;
        }

        // =========================================================
        // Helpers: determine current cell edit mode
        // =========================================================

        private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
        {
            // Піднімаємось по VisualTree вгору, поки:
            // - не знайдемо T
            // - або не дійдемо до кореня (null)
            while (child != null)
            {
                if (child is T typed)
                    return typed;

                child = System.Windows.Media.VisualTreeHelper.GetParent(child);
            }

            return null;
        }

        private bool IsCurrentCellEditing(DataGrid grid)
        {
            // 1) Якщо нема поточного item/column — нема що перевіряти.
            if (grid.CurrentItem == null || grid.CurrentColumn == null)
                return false;

            // 2) Беремо UI-елемент клітинки.
            var content = grid.CurrentColumn.GetCellContent(grid.CurrentItem);

            // 3) Якщо контенту нема — значить клітинки нема (або ще не згенерована).
            if (content == null)
                return false;

            // 4) З контенту знаходимо DataGridCell.
            var cell = FindParent<DataGridCell>(content);

            // 5) IsEditing==true означає, що зараз активний EditingElement (TextBox).
            return cell?.IsEditing == true;
        }


        // =========================================================
        // OPTIONAL: AutoGeneratingColumn fallback (якщо у XAML AutoGenerateColumns=true)
        // =========================================================

        private void AvailabilityGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            // Цей метод має сенс лише якщо у XAML:
            // - AutoGenerateColumns=true
            // - і ти хочеш підкрутити ширини/headers.
            //
            // Якщо ти використовуєш AvailabilityMatrixGridBuilder (AutoGenerateColumns=false),
            // цей handler можна не підключати взагалі.

            if (sender is not DataGrid grid)
                return;

            if (grid.ItemsSource is not DataView dv || dv.Table is null)
                return;

            if (!dv.Table.Columns.Contains(e.PropertyName))
                return;

            var dc = dv.Table.Columns[e.PropertyName];

            // Header беремо з Caption (у тебе Caption = "Day" і ПІБ працівника).
            e.Column.Header = dc.Caption;

            // Day column: fixed width + readonly.
            if (dc.ColumnName == AvailabilityMatrixEngine.DayColumnName)
            {
                e.Column.Width = new DataGridLength(70);
                e.Column.MinWidth = 70;
                e.Column.IsReadOnly = true;
                return;
            }

            // Employee columns: star + min width, щоб горизонтальний скрол був при потребі.
            e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            e.Column.MinWidth = 140;
        }
    }
}
