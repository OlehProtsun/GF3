using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using WPFApp.Infrastructure.ScheduleMatrix;
using WPFApp.ViewModel.Home;

namespace WPFApp.View.Home
{
    public partial class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent();
        }

        // Same perf tuning as in ContainerScheduleProfileView
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

        // Rebuild columns (schema-driven) like ContainerScheduleProfileView
        private void ActiveScheduleMatrixGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not DataGrid grid)
                return;

            ConfigureGridPerformance(grid);

            // Rebuild on load + on DC changes (important when Refresh replaces items)
            grid.DataContextChanged -= Grid_DataContextChanged;
            grid.DataContextChanged += Grid_DataContextChanged;

            RefreshGridSmart(grid);
        }

        private void Grid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is DataGrid grid)
                RefreshGridSmart(grid);
        }

        private static void RefreshGridSmart(DataGrid grid)
        {
            if (grid.DataContext is not HomeScheduleCardViewModel card)
                return;

            var table = card.ScheduleMatrix?.Table;
            if (table == null)
                return;

            var sig = string.Join("|", table.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
            if (grid.Tag as string == sig)
                return;

            grid.Columns.Clear();

            // Build columns EXACTLY like profile (same helper)
            ScheduleMatrixColumnBuilder.BuildScheduleMatrixColumns(table, grid, isReadOnly: true);

            // Fix headers: use DataColumn.Caption (employee display name)
            foreach (var col in grid.Columns)
            {
                if (col.SortMemberPath == null) continue;

                var dc = table.Columns.Cast<DataColumn>().FirstOrDefault(x => x.ColumnName == col.SortMemberPath);
                if (dc != null && !string.IsNullOrWhiteSpace(dc.Caption))
                    col.Header = dc.Caption;
            }

            grid.Tag = sig;
        }
    }
}
