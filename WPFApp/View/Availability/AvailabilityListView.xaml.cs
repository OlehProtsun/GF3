using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using WPFApp.ViewModel.Availability.List;

namespace WPFApp.View.Availability
{
    /// <summary>
    /// AvailabilityListView.xaml.cs
    ///
    /// Принцип:
    /// - code-behind тут робить лише “UI glue”:
    ///   * Enter у пошуку -> SearchCommand
    ///   * DoubleClick по рядку/гриді -> OpenProfileCommand
    ///   * клік по “hit area” -> коректне виділення рядка без переходу в edit cell
    ///
    /// Вся бізнес-логіка живе в ViewModel.
    /// </summary>
    public partial class AvailabilityListView : UserControl
    {
        public AvailabilityListView()
        {
            InitializeComponent();

            // Підписка на double-click на DataGrid.
            // Важливо:
            // - підписуємося 1 раз на інстанс View
            // - цього достатньо, бо DataGrid — дочірній елемент цього ж View
            dataGridAvailabilityList.MouseDoubleClick += DataGridAvailabilityList_MouseDoubleClick;
        }

        /// <summary>
        /// Enter у полі пошуку => запускаємо SearchCommand.
        /// </summary>
        private void InputSearch_KeyDown(object sender, KeyEventArgs e)
        {
            // 1) Цікавить лише Enter.
            if (e.Key != Key.Enter)
                return;

            // 2) Дістаємо VM.
            if (DataContext is not AvailabilityListViewModel vm)
                return;

            // 3) Виконуємо команду, якщо можна.
            if (vm.SearchCommand.CanExecute(null))
                vm.SearchCommand.Execute(null);

            // 4) Handled, щоб Enter не дзвякнув системним “beep” у деяких шаблонах.
            e.Handled = true;
        }

        /// <summary>
        /// Double-click по DataGrid => відкриваємо профіль.
        /// </summary>
        private void DataGridAvailabilityList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Делегуємо в єдину точку.
            TryOpenProfileFromUi();

            // Ставимо Handled, щоб подія не “піднялась” вище по дереву (інколи викликає зайві реакції).
            e.Handled = true;
        }

        /// <summary>
        /// Double-click по DataGridRow (якщо у XAML підписано на RowStyle).
        /// Залишаємо, щоб не ламати XAML, але реалізація — однакова.
        /// </summary>
        private void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TryOpenProfileFromUi();
            e.Handled = true;
        }

        /// <summary>
        /// Єдина точка “відкрити профіль”, щоб не дублювати code paths.
        /// </summary>
        private void TryOpenProfileFromUi()
        {
            // 1) Дістаємо VM.
            if (DataContext is not AvailabilityListViewModel vm)
                return;

            // 2) Якщо команда доступна — виконуємо.
            if (vm.OpenProfileCommand.CanExecute(null))
                vm.OpenProfileCommand.Execute(null);
        }

        /// <summary>
        /// RowHitArea_MouseLeftButtonDown:
        /// - це обробник для “широкої області кліку” (наприклад Grid/Border всередині Row template).
        ///
        /// Завдання:
        /// - по кліку виділити рядок повністю
        /// - і НЕ дати DataGrid перейти у режим “редагування клітинки”
        /// </summary>
        private void RowHitArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 1) Нам потрібен DependencyObject, щоб знайти контейнер-рядок.
            if (sender is not DependencyObject dep)
                return;

            // 2) Найнадійніший спосіб отримати DataGridRow:
            //    ItemsControl.ContainerFromElement(ItemsControl, elementInsideRow)
            //    - працює стабільно для будь-яких шаблонів
            //    - не треба вручну ходити по VisualTree
            var row = ItemsControl.ContainerFromElement(dataGridAvailabilityList, dep) as DataGridRow;

            // 3) Якщо рядок знайдено — вибираємо і фокусимо його.
            if (row != null)
            {
                row.IsSelected = true;
                row.Focus();

                // 4) Handled=true — щоб DataGrid не пробував ставити курсор у клітинку/починати edit.
                e.Handled = true;
            }
        }
    }
}
