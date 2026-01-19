using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using WPFApp.ViewModel.Container;

namespace WPFApp.View.Container
{
    /// <summary>
    /// Interaction logic for ContainerScheduleProfileView.xaml
    /// </summary>
    public partial class ContainerScheduleProfileView : UserControl
    {
        private ContainerScheduleProfileViewModel? _vm;
        private string? _schemaSig;
        private bool _refreshQueued;

        public ContainerScheduleProfileView()
        {
            InitializeComponent();

            ConfigureGridPerformance(dataGridScheduleProfile);

            DataContextChanged += ContainerScheduleProfileView_DataContextChanged;
            Loaded += ContainerScheduleProfileView_Loaded;
            Unloaded += ContainerScheduleProfileView_Unloaded;
        }

        private static void ConfigureGridPerformance(DataGrid grid)
        {
            grid.EnableRowVirtualization = true;
            grid.EnableColumnVirtualization = true;
            VirtualizingPanel.SetIsVirtualizing(grid, true);
            VirtualizingPanel.SetVirtualizationMode(grid, VirtualizationMode.Recycling);

            grid.SetValue(ScrollViewer.CanContentScrollProperty, true);
            grid.SetValue(ScrollViewer.IsDeferredScrollingEnabledProperty, false);

            grid.SetValue(VirtualizingPanel.ScrollUnitProperty, ScrollUnit.Item);
            VirtualizingPanel.SetCacheLengthUnit(grid, VirtualizationCacheLengthUnit.Page);
            VirtualizingPanel.SetCacheLength(grid, new VirtualizationCacheLength(1));
        }

        private void ContainerScheduleProfileView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
            => AttachViewModel(DataContext as ContainerScheduleProfileViewModel);

        private void ContainerScheduleProfileView_Loaded(object sender, RoutedEventArgs e)
            => AttachViewModel(DataContext as ContainerScheduleProfileViewModel);

        private void ContainerScheduleProfileView_Unloaded(object sender, RoutedEventArgs e)
            => DetachViewModel();

        private void AttachViewModel(ContainerScheduleProfileViewModel? viewModel)
        {
            if (_vm != null)
                _vm.MatrixChanged -= VmOnMatrixChanged;

            _vm = viewModel;

            if (_vm != null)
            {
                _vm.MatrixChanged += VmOnMatrixChanged;
                RefreshGridSmart();
            }
        }

        private void DetachViewModel()
        {
            if (_vm == null) return;
            _vm.MatrixChanged -= VmOnMatrixChanged;
        }

        private void VmOnMatrixChanged(object? sender, EventArgs e)
        {
            if (_vm is null) return;
            if (_refreshQueued) return;
            _refreshQueued = true;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                _refreshQueued = false;
                RefreshGridSmart();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private static void ResetGridColumns(DataGrid grid)
        {
            grid.ItemsSource = null;
            grid.Columns.Clear();
        }

        private void RefreshGridSmart()
        {
            if (_vm is null) return;
            var table = _vm.ScheduleMatrix?.Table;
            if (table == null) return;

            var sig = string.Join("|", table.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
            if (sig != _schemaSig)
            {
                ResetGridColumns(dataGridScheduleProfile);
                ScheduleMatrixColumnBuilder.BuildScheduleMatrixColumns(table, dataGridScheduleProfile, isReadOnly: true);
                _schemaSig = sig;
            }
        }
    }
}

