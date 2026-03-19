/*
  Опис файлу: цей модуль містить реалізацію компонента AvailabilityEditView у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
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
    /// Визначає публічний елемент `public partial class AvailabilityEditView : UserControl` та контракт його використання у шарі WPFApp.
    /// </summary>
    public partial class AvailabilityEditView : UserControl
    {
        
        private AvailabilityEditViewModel? _vm;

        /// <summary>
        /// Визначає публічний елемент `public AvailabilityEditView()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AvailabilityEditView()
        {
            InitializeComponent();

            
            DataContextChanged += OnDataContextChanged;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            
            
            dataGridAvailabilityDays.PreviewKeyDown += DataGridAvailabilityDays_PreviewKeyDown;

            
            dataGridBinds.AutoGeneratingColumn += DataGridBinds_AutoGeneratingColumn;
            dataGridBinds.PreviewKeyDown += DataGridBinds_PreviewKeyDown;
            dataGridBinds.RowEditEnding += DataGridBinds_RowEditEnding;

            
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
            
            if (ReferenceEquals(_vm, viewModel))
                return;

            
            DetachViewModel();

            
            _vm = viewModel;

            
            if (_vm is null)
                return;

            
            
            
            _vm.MatrixChanged += VmOnMatrixChanged;

            
            AvailabilityMatrixGridBuilder.BuildEditable(_vm.AvailabilityDays.Table, dataGridAvailabilityDays);
        }

        private void DetachViewModel()
        {
            
            if (_vm is null)
                return;

            
            _vm.MatrixChanged -= VmOnMatrixChanged;

            
            _vm = null;
        }

        private void VmOnMatrixChanged(object? sender, EventArgs e)
        {
            
            if (_vm is null)
                return;

            
            if (!Dispatcher.CheckAccess())
            {
                _ = Dispatcher.BeginInvoke(new Action(() => VmOnMatrixChanged(sender, e)));
                return;
            }

            
            AvailabilityMatrixGridBuilder.BuildEditable(_vm.AvailabilityDays.Table, dataGridAvailabilityDays);
        }

        
        
        

        private void TextBoxSearchValueFromAvailabilityEdit_KeyDown(object sender, KeyEventArgs e)
        {
            
            if (e.Key != Key.Enter)
                return;

            
            if (DataContext is not AvailabilityEditViewModel vm)
                return;

            
            if (vm.SearchEmployeeCommand.CanExecute(null))
                vm.SearchEmployeeCommand.Execute(null);

            e.Handled = true;
        }

        
        
        

        private async void DataGridBinds_RowEditEnding(object? sender, DataGridRowEditEndingEventArgs e)
        {
            
            if (e.EditAction != DataGridEditAction.Commit)
                return;

            
            if (_vm is null)
                return;

            
            if (e.Row.Item is not BindRow row)
                return;

            
            
            await _vm.UpsertBindAsync(row);
        }

        private void DataGridBinds_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            
            
            
            
            if (e.PropertyName == nameof(BindRow.Key))
                e.Column.IsReadOnly = true;
        }

        private void DataGridBinds_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            
            if (_vm is null)
                return;

            
            if (e.Key == Key.Enter)
            {
                dataGridBinds.CommitEdit(DataGridEditingUnit.Cell, true);
                dataGridBinds.CommitEdit(DataGridEditingUnit.Row, true);
                e.Handled = true;
                return;
            }

            
            if (IsCurrentCellEditing(dataGridBinds))
                return;

            
            if (dataGridBinds.CurrentColumn is not DataGridBoundColumn boundColumn)
                return;

            if (boundColumn.Binding is not Binding b)
                return;

            if (b.Path?.Path != nameof(BindRow.Key))
                return;

            
            
            if (Keyboard.Modifiers == ModifierKeys.None)
                return;

            
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

            
            if (AvailabilityViewInputHelper.IsCommonEditorShortcut(e.Key, Keyboard.Modifiers))
                return;

            
            var key = (e.Key == Key.System) ? e.SystemKey : e.Key;

            
            var keyText = _vm.FormatKeyGesture(key, Keyboard.Modifiers);

            
            if (string.IsNullOrWhiteSpace(keyText))
                return;

            
            if (dataGridBinds.CurrentItem is BindRow row)
            {
                row.Key = keyText;

                
                dataGridBinds.CommitEdit(DataGridEditingUnit.Cell, true);
            }

            
            e.Handled = true;
        }

        
        
        

        private void DataGridAvailabilityDays_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            
            if (_vm is null)
                return;

            
            if (dataGridAvailabilityDays.CurrentColumn is not DataGridBoundColumn boundColumn)
                return;

            if (boundColumn.Binding is not Binding binding)
                return;

            
            
            var columnName = (binding.Path?.Path ?? string.Empty).Trim('[', ']');

            
            if (string.IsNullOrWhiteSpace(columnName))
                return;

            
            if (columnName == AvailabilityMatrixEngine.DayColumnName)
                return;

            
            
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

            
            if (AvailabilityViewInputHelper.IsCommonEditorShortcut(e.Key, Keyboard.Modifiers))
                return;

            
            
            
            var key = (e.Key == Key.System) ? e.SystemKey : e.Key;

            string rawKeyToken;

            if (Keyboard.Modifiers != ModifierKeys.None)
            {
                
                rawKeyToken = _vm.FormatKeyGesture(key, Keyboard.Modifiers) ?? string.Empty;

                
                if (string.IsNullOrWhiteSpace(rawKeyToken))
                    return;
            }
            else
            {
                
                rawKeyToken = AvailabilityViewInputHelper.KeyToBindToken(key);
            }

            
            
            
            if (!_vm.TryGetBindValue(rawKeyToken, out var bindValue))
                return;

            
            
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

                
                dataGridAvailabilityDays.SelectedCells.Clear();
                dataGridAvailabilityDays.SelectedCells.Add(new DataGridCellInfo(nextItem, boundColumn));

                
                dataGridAvailabilityDays.Dispatcher.BeginInvoke(new Action(() =>
                {
                    dataGridAvailabilityDays.Focus();
                    dataGridAvailabilityDays.BeginEdit();
                }), System.Windows.Threading.DispatcherPriority.Background);
            }

            
            e.Handled = true;
        }

        
        
        

        private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
        {
            
            
            
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
            
            if (grid.CurrentItem == null || grid.CurrentColumn == null)
                return false;

            
            var content = grid.CurrentColumn.GetCellContent(grid.CurrentItem);

            
            if (content == null)
                return false;

            
            var cell = FindParent<DataGridCell>(content);

            
            return cell?.IsEditing == true;
        }


        
        
        

        private void AvailabilityGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            
            
            
            
            
            

            if (sender is not DataGrid grid)
                return;

            if (grid.ItemsSource is not DataView dv || dv.Table is null)
                return;

            if (!dv.Table.Columns.Contains(e.PropertyName))
                return;

            var dc = dv.Table.Columns[e.PropertyName];

            
            e.Column.Header = dc.Caption;

            
            if (dc.ColumnName == AvailabilityMatrixEngine.DayColumnName)
            {
                e.Column.Width = new DataGridLength(70);
                e.Column.MinWidth = 70;
                e.Column.IsReadOnly = true;
                return;
            }

            
            e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            e.Column.MinWidth = 140;
        }
    }
}
