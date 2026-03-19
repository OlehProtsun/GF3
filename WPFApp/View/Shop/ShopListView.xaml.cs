/*
  Опис файлу: цей модуль містить реалізацію компонента ShopListView у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WPFApp.ViewModel.Shop;

namespace WPFApp.View.Shop
{
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public partial class ShopListView : UserControl` та контракт його використання у шарі WPFApp.
    /// </summary>
    public partial class ShopListView : UserControl
    {
        /// <summary>
        /// Визначає публічний елемент `public ShopListView()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ShopListView()
        {
            InitializeComponent();
            dataGridShopList.MouseDoubleClick += DataGridShopList_MouseDoubleClick;
        }

        private void DataGridShopList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ShopListViewModel vm && vm.OpenProfileCommand.CanExecute(null))
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
