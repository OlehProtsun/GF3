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

        private void Row_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not ContainerProfileViewModel rootVm) return;

            // MultiOpen = ON: подвійний клік нічого не робить
            if (rootVm.ScheduleListVm.IsMultiOpenEnabled)
            {
                e.Handled = true;
                return;
            }

            // якщо подвійний клік по чекбоксу — ігноруємо
            if (FindAncestor<CheckBox>((DependencyObject)e.OriginalSource) != null)
                return;

            if (sender is DataGridRow row)
            {
                // гарантуємо, що відкриваємо саме той рядок
                dataGridSchedules.SelectedItem = row.DataContext;

                if (rootVm.ScheduleListVm.OpenProfileCommand.CanExecute(null))
                    rootVm.ScheduleListVm.OpenProfileCommand.Execute(null);

                e.Handled = true;
            }
        }


        private void DataGridSchedules_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not ContainerProfileViewModel vm) return;

            // MultiOpen ON: double click нічого не робить
            if (vm.ScheduleListVm.IsMultiOpenEnabled)
            {
                e.Handled = true;
                return;
            }

            // Відкривати ТІЛЬКИ якщо double-click реально по рядку
            var row = FindAncestor<DataGridRow>((DependencyObject)e.OriginalSource);
            if (row == null) return;

            dataGridSchedules.SelectedItem = row.DataContext;

            if (vm.ScheduleListVm.OpenProfileCommand.CanExecute(null))
                vm.ScheduleListVm.OpenProfileCommand.Execute(null);

            e.Handled = true;
        }



        private void RowHitArea_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (dataGridSchedules?.DataContext is not ContainerScheduleListViewModel vm) return;

            // OFF: нічого не робимо, хай відпрацює MouseLeftButtonDown (швидке виділення)
            if (!vm.IsMultiOpenEnabled)
                return;

            // ON: кліком по CheckBox не керуємо
            if (FindAncestor<CheckBox>((DependencyObject)e.OriginalSource) != null)
                return;

            // Знаходимо рядок як в Employee
            var dep = (DependencyObject)sender;
            while (dep != null && dep is not DataGridRow)
                dep = VisualTreeHelper.GetParent(dep);

            if (dep is DataGridRow row && row.DataContext is ScheduleRowVm item)
                vm.ToggleRowSelection(item);

            dataGridSchedules.UnselectAll();
            dataGridSchedules.Focus();

            e.Handled = true;
        }

        private void RowHitArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (dataGridSchedules?.DataContext is not ContainerScheduleListViewModel vm) return;

            // ON: Preview вже обробив
            if (vm.IsMultiOpenEnabled)
                return;

            // швидко як в Employee: просто selected + focus
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



        private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T t) return t;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
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
