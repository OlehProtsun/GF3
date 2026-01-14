using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WPFApp.ViewModel.Container;

namespace WPFApp.View.Container
{
    /// <summary>
    /// Interaction logic for ContainerProfileView.xaml
    /// </summary>
    public partial class ContainerProfileView : UserControl
    {
        public ContainerProfileView()
        {
            InitializeComponent();
            dataGridSchedules.MouseDoubleClick += DataGridSchedules_MouseDoubleClick;
        }

        private void DataGridSchedules_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not ContainerProfileViewModel vm) return;
            if (vm.ScheduleListVm.OpenProfileCommand.CanExecute(null))
                vm.ScheduleListVm.OpenProfileCommand.Execute(null);
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
