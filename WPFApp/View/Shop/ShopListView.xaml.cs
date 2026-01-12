using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WPFApp.ViewModel.Shop;

namespace WPFApp.View.Shop
{
    /// <summary>
    /// Interaction logic for ShopListView.xaml
    /// </summary>
    public partial class ShopListView : UserControl
    {
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
