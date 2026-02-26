/*
  Опис файлу: цей модуль містить реалізацію компонента HomeView у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using WPFApp.UI.Matrix.Schedule;
using WPFApp.ViewModel.Home;

namespace WPFApp.View.Home
{
    /// <summary>
    /// Визначає публічний елемент `public partial class HomeView : UserControl` та контракт його використання у шарі WPFApp.
    /// </summary>
    public partial class HomeView : UserControl
    {
        /// <summary>
        /// Визначає публічний елемент `public HomeView()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public HomeView()
        {
            InitializeComponent();
        }

        
        private static void ConfigureGridPerformance(DataGrid grid)
        {
            grid.EnableRowVirtualization = true;
            grid.EnableColumnVirtualization = true;

            VirtualizingPanel.SetIsVirtualizing(grid, true);
            VirtualizingPanel.SetVirtualizationMode(grid, VirtualizationMode.Recycling);

            grid.SetValue(ScrollViewer.CanContentScrollProperty, true);
            grid.SetValue(ScrollViewer.IsDeferredScrollingEnabledProperty, false);

            grid.SetValue(VirtualizingPanel.ScrollUnitProperty, ScrollUnit.Pixel);
            VirtualizingPanel.SetCacheLengthUnit(grid, VirtualizationCacheLengthUnit.Page);
            VirtualizingPanel.SetCacheLength(grid, new VirtualizationCacheLength(1));
        }

        
        private void ActiveScheduleMatrixGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not DataGrid grid)
                return;

            ConfigureGridPerformance(grid);

            
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

            
            ScheduleMatrixColumnBuilder.BuildScheduleMatrixColumns(table, grid, isReadOnly: true);

            
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
