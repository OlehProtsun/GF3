using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WPFApp.ViewModel.Container.Profile;
using WPFApp.ViewModel.Container.ScheduleList;

namespace WPFApp.View.Container
{
    /// <summary>
    /// Profile screen for a single container.
    /// Code-behind handles only UI-specific behavior:
    /// - dynamic grid column rebuild for statistics
    /// - row click / double-click interactions for the schedules list
    /// - safe attach/detach of VM events
    /// </summary>
    public partial class ContainerProfileView : UserControl
    {
        private ContainerProfileViewModel? _vm;
        private bool _rebuildQueued;

        /// <summary>
        /// Wires lifecycle events so the view can subscribe/unsubscribe from VM notifications safely.
        /// </summary>
        public ContainerProfileView()
        {
            InitializeComponent();

            DataContextChanged += (_, __) => AttachVm(DataContext as ContainerProfileViewModel);
            Loaded += (_, __) => AttachVm(DataContext as ContainerProfileViewModel);
            Unloaded += (_, __) => DetachVm();
        }

        private void AttachVm(ContainerProfileViewModel? vm)
        {
            if (_vm != null)
                _vm.StatisticsChanged -= VmOnStatisticsChanged;

            _vm = vm;

            if (_vm != null)
            {
                _vm.StatisticsChanged += VmOnStatisticsChanged;

                // одразу побудувати колонки (якщо дані вже є)
                QueueRebuildStatsColumns();
            }
        }

        private void DetachVm()
        {
            if (_vm == null) return;

            _vm.StatisticsChanged -= VmOnStatisticsChanged;
            _vm.CancelBackgroundWork();
            _vm = null;
        }

        private void VmOnStatisticsChanged(object? sender, EventArgs e) => QueueRebuildStatsColumns();

        private void QueueRebuildStatsColumns()
        {
            if (_rebuildQueued) return;
            _rebuildQueued = true;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                _rebuildQueued = false;
                RebuildEmployeeShopHoursColumns();
            }), DispatcherPriority.Background);
        }



        void RebuildEmployeeShopHoursColumns()
        {
            if (_vm == null || dataGridContainerEmployeeShopHours == null)
                return;

            dataGridContainerEmployeeShopHours.Columns.Clear();

            // Employee
            dataGridContainerEmployeeShopHours.Columns.Add(new DataGridTextColumn
            {
                Header = "Employee",
                Binding = new Binding(nameof(ContainerProfileViewModel.EmployeeShopHoursRow.Employee))
                {
                    Mode = BindingMode.OneWay
                },
                Width = new DataGridLength(240)
            });

            // Work Days
            dataGridContainerEmployeeShopHours.Columns.Add(new DataGridTextColumn
            {
                Header = "Work Days",
                Binding = new Binding(nameof(ContainerProfileViewModel.EmployeeShopHoursRow.WorkDays))
                {
                    Mode = BindingMode.OneWay
                },
                Width = new DataGridLength(90)
            });

            // Free Days
            dataGridContainerEmployeeShopHours.Columns.Add(new DataGridTextColumn
            {
                Header = "Free Days",
                Binding = new Binding(nameof(ContainerProfileViewModel.EmployeeShopHoursRow.FreeDays))
                {
                    Mode = BindingMode.OneWay
                },
                Width = new DataGridLength(90)
            });

            // Sum
            dataGridContainerEmployeeShopHours.Columns.Add(new DataGridTextColumn
            {
                Header = "Sum",
                Binding = new Binding(nameof(ContainerProfileViewModel.EmployeeShopHoursRow.HoursSum))
                {
                    Mode = BindingMode.OneWay
                },
                Width = new DataGridLength(90)
            });

            // Dynamic shop columns
            foreach (var shop in _vm.ShopHeaders)
            {
                var b = new Binding($"[{shop.Key}]")
                {
                    Mode = BindingMode.OneWay,
                    FallbackValue = "",
                    TargetNullValue = ""
                };

                dataGridContainerEmployeeShopHours.Columns.Add(new DataGridTextColumn
                {
                    Header = shop.Name,
                    Binding = b,
                    Width = new DataGridLength(90)
                });
            }

            dataGridContainerEmployeeShopHours.FrozenColumnCount = 4;
        }

        // ====== твої існуючі handlers (залишаю як були) ======
        private void DataGridSchedules_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not ContainerProfileViewModel vm) return;

            if (vm.ScheduleListVm.IsMultiOpenEnabled)
            {
                e.Handled = true;
                return;
            }

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

            if (!vm.IsMultiOpenEnabled)
                return;

            if (FindAncestor<CheckBox>((DependencyObject)e.OriginalSource) != null)
                return;

            var dep = (DependencyObject)sender;
            while (dep != null && dep is not DataGridRow)
                dep = VisualTreeHelper.GetParent(dep);

            if (dep is DataGridRow row && row.DataContext is ScheduleRowVm item)
                vm.ToggleRowSelection(item);

            dataGridSchedules.UnselectAll();
            dataGridSchedules.Focus();

            e.Handled = true;
        }

        private static Style BlankTextWhenTotalsRow()
        {
            var style = new Style(typeof(TextBlock));

            var trigger = new DataTrigger
            {
                Binding = new Binding(nameof(ContainerProfileViewModel.EmployeeShopHoursRow.Employee)),
                Value = "TOTAL"
            };

            trigger.Setters.Add(new Setter(TextBlock.TextProperty, ""));
            style.Triggers.Add(trigger);

            return style;
        }

        private void RowHitArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (dataGridSchedules?.DataContext is not ContainerScheduleListViewModel vm) return;

            if (vm.IsMultiOpenEnabled)
                return;

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
        private void DataGridSchedules_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (dataGridSchedules?.DataContext is not ContainerScheduleListViewModel vm)
                return;

            // Працює ТІЛЬКИ в MultiOpen режимі
            if (!vm.IsMultiOpenEnabled)
                return;

            var original = e.OriginalSource as DependencyObject;
            if (original == null)
                return;

            // Якщо клік прямо по чекбоксу — не чіпаємо (даємо йому самому переключитись)
            if (FindAncestor<CheckBox>(original) != null)
                return;

            // Знаходимо рядок під кліком
            var row = FindAncestor<DataGridRow>(original);
            if (row?.DataContext is ScheduleRowVm item)
                vm.ToggleRowSelection(item);

            // щоб DataGrid не робив стандартний selection (ти керуєш вибором через IsChecked)
            dataGridSchedules.UnselectAll();
            dataGridSchedules.Focus();

            e.Handled = true;
        }


        private void DataGridSchedules_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dataGridSchedules?.DataContext is ContainerScheduleListViewModel vm && vm.IsMultiOpenEnabled)
            {
                e.Handled = true;
            }
        }
    }
}
