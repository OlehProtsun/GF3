using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WPFApp.ViewModel.Container.List;

namespace WPFApp.View.Container
{
    /// <summary>
    /// List screen for containers.
    /// Code-behind keeps only UI interactions that are hard to express in pure XAML:
    /// row hit-area selection and double-click open behavior.
    /// </summary>
    public partial class ContainerListView : UserControl
    {
        /// <summary>
        /// Initializes the view and wires DataGrid double-click to the VM command.
        /// </summary>
        public ContainerListView()
        {
            InitializeComponent();
            dataGridContainerList.MouseDoubleClick += DataGridContainerList_MouseDoubleClick;
        }

        /// <summary>
        /// Opens the selected container profile on row double-click.
        /// </summary>
        private void DataGridContainerList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ContainerListViewModel vm && vm.OpenProfileCommand.CanExecute(null))
                vm.OpenProfileCommand.Execute(null);
        }

        /// <summary>
        /// Makes the whole custom row layout clickable (not only the text content).
        /// This keeps DataGrid selection behavior predictable when using card-style rows.
        /// </summary>
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
