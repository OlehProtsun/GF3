/*
  Опис файлу: цей модуль містить реалізацію компонента EmployeeListView у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WPFApp.ViewModel.Employee;

namespace WPFApp.View.Employee
{
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public partial class EmployeeListView : UserControl` та контракт його використання у шарі WPFApp.
    /// </summary>
    public partial class EmployeeListView : UserControl
    {
        /// <summary>
        /// Визначає публічний елемент `public EmployeeListView()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public EmployeeListView()
        {
            InitializeComponent();
            dataGridEmployeeList.MouseDoubleClick += DataGridEmployeeList_MouseDoubleClick;
        }

        private void DataGridEmployeeList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is EmployeeListViewModel vm && vm.OpenProfileCommand.CanExecute(null))
                vm.OpenProfileCommand.Execute(null);
        }

        private void RowHitArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dep = (DependencyObject)sender;
            while (dep != null && dep is not DataGridRow)
                dep = VisualTreeHelper.GetParent(dep);

            if (dep is DataGridRow row)
            {
                row.IsSelected = true;
                row.Focus();
                e.Handled = true;
            }
        }
    }
}
