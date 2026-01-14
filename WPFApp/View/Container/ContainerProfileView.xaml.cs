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
            if (vm.ScheduleListVm.IsMultiOpenEnabled)
            {
                e.Handled = true;
                return;
            }
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
                if (dataGridSchedules?.DataContext is ContainerScheduleListViewModel vm && vm.IsMultiOpenEnabled)
                {
                    if (row.DataContext is ScheduleRowVm scheduleRow)
                        vm.ToggleRowSelection(scheduleRow);

                    row.IsSelected = true;
                    row.Focus();
                    e.Handled = true;
                    return;
                }

                row.IsSelected = true;
                row.Focus();
                e.Handled = true;
            }
        }

        private void DataGridSchedules_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dataGridSchedules?.DataContext is ContainerScheduleListViewModel vm && vm.IsMultiOpenEnabled)
            {
                e.Handled = true; // В MULTIOPEN подвійний клік НІЧОГО не робить
            }
        }

    }
}
