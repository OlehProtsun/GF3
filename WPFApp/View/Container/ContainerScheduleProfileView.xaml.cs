/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerScheduleProfileView у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using WPFApp.ViewModel.Container.ScheduleProfile;
using WPFApp.UI.Matrix.Schedule;

namespace WPFApp.View.Container
{
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public partial class ContainerScheduleProfileView : UserControl` та контракт його використання у шарі WPFApp.
    /// </summary>
    public partial class ContainerScheduleProfileView : UserControl
    {
        private ContainerScheduleProfileViewModel? _vm;

        
        private string? _schemaSig;
        private readonly DispatcherTimer _refreshThrottleTimer;
        private bool _refreshRequested;

        
        private bool _summarySync;   
        private bool _summarySyncH;  

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ContainerScheduleProfileView()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ContainerScheduleProfileView()
        {
            InitializeComponent();

            _refreshThrottleTimer = new DispatcherTimer(
                TimeSpan.FromMilliseconds(16),
                DispatcherPriority.Background,
                RefreshThrottleTimer_Tick,
                Dispatcher);

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

            grid.SetValue(VirtualizingPanel.ScrollUnitProperty, ScrollUnit.Pixel);
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
            if (ReferenceEquals(_vm, viewModel))
            {
                
                Dispatcher.BeginInvoke(new Action(UpdateSummaryHorizontalBar), DispatcherPriority.Loaded);
                return;
            }

            if (_vm != null)
                _vm.MatrixChanged -= VmOnMatrixChanged;

            _vm = viewModel;

            if (_vm != null)
            {
                _vm.MatrixChanged += VmOnMatrixChanged;

                RefreshGridSmart();
                Dispatcher.BeginInvoke(new Action(UpdateSummaryHorizontalBar), DispatcherPriority.Loaded);
            }
        }

        private void DetachViewModel()
        {
            if (_vm == null) return;

            _vm.MatrixChanged -= VmOnMatrixChanged;
            _vm.CancelBackgroundWork();
            _vm = null;
        }

        private void VmOnMatrixChanged(object? sender, EventArgs e)
        {
            if (_vm is null) return;

            _refreshRequested = true;
            if (!_refreshThrottleTimer.IsEnabled)
                _refreshThrottleTimer.Start();
        }

        private void RefreshThrottleTimer_Tick(object? sender, EventArgs e)
        {
            if (!_refreshRequested)
            {
                _refreshThrottleTimer.Stop();
                return;
            }

            _refreshRequested = false;
            RefreshGridSmart();
            UpdateSummaryHorizontalBar();

            if (!_refreshRequested)
                _refreshThrottleTimer.Stop();
        }

        
        
        
        private static void ResetGridColumns(DataGrid grid)
        {
            
            
            grid.Columns.Clear();
        }

        private void RefreshGridSmart()
        {
            if (_vm is null) return;

            var table = _vm.ScheduleMatrix?.Table;
            if (table == null) return;

            var sig = string.Join("|", table.Columns.Cast<DataColumn>()
                .Select(c => $"{c.ColumnName}:{c.DataType.FullName}")); if (sig == _schemaSig)
                return;

            ResetGridColumns(dataGridScheduleProfile);

            
            ScheduleMatrixColumnBuilder.BuildScheduleMatrixColumns(table, dataGridScheduleProfile, isReadOnly: true);

            _schemaSig = sig;
        }

        
        
        

        
        
        
        
        private void SummaryBodyScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_summarySync) return;

            try
            {
                _summarySync = true;

                
                if (e.HorizontalChange != 0)
                    SummaryHeaderScroll?.ScrollToHorizontalOffset(e.HorizontalOffset);

                
                if (e.VerticalChange != 0)
                    SummaryLeftScroll?.ScrollToVerticalOffset(e.VerticalOffset);

                
                if (e.HorizontalChange != 0 || e.ExtentWidthChange != 0 || e.ViewportWidthChange != 0)
                    UpdateSummaryHorizontalBar();
            }
            finally
            {
                _summarySync = false;
            }
        }

        
        
        
        private void SummaryHeaderScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
            => ForwardWheelToSummaryBody(e);

        
        
        

        private void SummaryLeftScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
            => ForwardWheelToSummaryBody(e);

        
        
        

        
        
        
        private void SummaryHScroll_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_summarySyncH) return;

            try
            {
                _summarySyncH = true;

                SummaryBodyScroll?.ScrollToHorizontalOffset(e.NewValue);
                SummaryHeaderScroll?.ScrollToHorizontalOffset(e.NewValue);
            }
            finally
            {
                _summarySyncH = false;
            }
        }

        
        
        
        
        
        
        
        
        private void UpdateSummaryHorizontalBar()
        {
            if (SummaryBodyScroll == null || SummaryHScroll == null)
                return;

            
            
            var max = Math.Max(0, SummaryBodyScroll.ExtentWidth - SummaryBodyScroll.ViewportWidth);

            SummaryHScroll.Minimum = 0;
            SummaryHScroll.Maximum = max;

            
            SummaryHScroll.ViewportSize = SummaryBodyScroll.ViewportWidth;

            SummaryHScroll.LargeChange = SummaryBodyScroll.ViewportWidth;
            SummaryHScroll.SmallChange = 30;

            if (!_summarySyncH)
                SummaryHScroll.Value = SummaryBodyScroll.HorizontalOffset;

            
            SummaryHScroll.Visibility = max > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SummaryBodyScroll_Loaded(object sender, RoutedEventArgs e)
            => UpdateSummaryHorizontalBar();

        private void SummaryBodyScroll_SizeChanged(object sender, SizeChangedEventArgs e)
            => UpdateSummaryHorizontalBar();

        private void ForwardWheelToSummaryBody(MouseWheelEventArgs e)
        {
            if (SummaryBodyScroll == null) return;

            SummaryBodyScroll.ScrollToVerticalOffset(SummaryBodyScroll.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }
}
