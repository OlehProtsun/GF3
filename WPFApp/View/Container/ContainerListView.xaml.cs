/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerListView у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WPFApp.ViewModel.Container.List;

namespace WPFApp.View.Container
{
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public partial class ContainerListView : UserControl` та контракт його використання у шарі WPFApp.
    /// </summary>
    public partial class ContainerListView : UserControl
    {
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ContainerListView()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ContainerListView()
        {
            InitializeComponent();
            dataGridContainerList.MouseDoubleClick += DataGridContainerList_MouseDoubleClick;
        }

        
        
        
        private void DataGridContainerList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ContainerListViewModel vm && vm.OpenProfileCommand.CanExecute(null))
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
