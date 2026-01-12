using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WPFApp.ViewModel.Container;

namespace WPFApp.View.Container
{
    /// <summary>
    /// Interaction logic for ContainerListView.xaml
    /// </summary>
    public partial class ContainerListView : UserControl
    {
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
