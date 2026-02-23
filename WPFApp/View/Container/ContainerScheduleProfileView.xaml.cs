using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using WPFApp.ViewModel.Container.ScheduleProfile;
using WPFApp.Infrastructure.ScheduleMatrix;

namespace WPFApp.View.Container
{
    /// <summary>
    /// Read-only schedule profile screen.
    /// Code-behind is responsible for DataGrid performance settings, schema-aware column rebuild
    /// and synchronized scrolling of the custom summary table (header/left/body/bottom scrollbar).
    /// </summary>
    public partial class ContainerScheduleProfileView : UserControl
    {
        private ContainerScheduleProfileViewModel? _vm;

        // ---- Matrix(DataGrid) rebuild guards ----
        private string? _schemaSig;
        private bool _refreshQueued;

        // ---- Summary scroll sync guards ----
        private bool _summarySync;   // захист від рекурсії під час ScrollChanged
        private bool _summarySyncH;  // захист від рекурсії під час ValueChanged нижнього ScrollBar

        /// <summary>
        /// Initializes UI helpers (virtualization + lifecycle handlers).
        /// </summary>
        public ContainerScheduleProfileView()
        {
            InitializeComponent();

            ConfigureGridPerformance(dataGridScheduleProfile);

            DataContextChanged += ContainerScheduleProfileView_DataContextChanged;
            Loaded += ContainerScheduleProfileView_Loaded;
            Unloaded += ContainerScheduleProfileView_Unloaded;
        }

        // =========================================================
        // 1) DataGrid performance
        // =========================================================
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

        // =========================================================
        // 2) VM attach/detach
        // =========================================================
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
                // тільки оновити UI-залежні речі після layout
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
            if (_refreshQueued) return;

            _refreshQueued = true;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                _refreshQueued = false;
                RefreshGridSmart();

                // після оновлення SummaryRows / headers — оновити max нижнього H-scroll
                UpdateSummaryHorizontalBar();

            }), DispatcherPriority.Background);
        }

        // =========================================================
        // 3) Schedule Matrix DataGrid columns builder (schema-driven)
        // =========================================================
        private static void ResetGridColumns(DataGrid grid)
        {
            // ВАЖЛИВО: НЕ робимо grid.ItemsSource = null;
            // бо ItemsSource у тебе біндиться в XAML, і це зносить binding.
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

            // Будуємо колонки під поточну DataTable схему
            ScheduleMatrixColumnBuilder.BuildScheduleMatrixColumns(table, dataGridScheduleProfile, isReadOnly: true);

            _schemaSig = sig;
        }

        // =========================================================
        // 4) SUMMARY TABLE SCROLL SYNC (Frozen left + Frozen headers)
        // =========================================================

        /// <summary>
        /// Main body ScrollViewer -> синхронізує Header (X) та Left (Y),
        /// і тримає нижній ScrollBar у відповідності.
        /// </summary>
        private void SummaryBodyScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_summarySync) return;

            try
            {
                _summarySync = true;

                // Sync X: body -> header
                if (e.HorizontalChange != 0)
                    SummaryHeaderScroll?.ScrollToHorizontalOffset(e.HorizontalOffset);

                // Sync Y: body -> left
                if (e.VerticalChange != 0)
                    SummaryLeftScroll?.ScrollToVerticalOffset(e.VerticalOffset);

                // Оновлюємо нижній scrollbar тільки коли змінилась горизонтальна частина / viewport / extent
                if (e.HorizontalChange != 0 || e.ExtentWidthChange != 0 || e.ViewportWidthChange != 0)
                    UpdateSummaryHorizontalBar();
            }
            finally
            {
                _summarySync = false;
            }
        }

        /// <summary>
        /// Колесо миші над шапкою: скролимо вертикально Body (шапка лишається sticky).
        /// </summary>
        private void SummaryHeaderScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
            => ForwardWheelToSummaryBody(e);

        /// <summary>
        /// Колесо миші над лівою колонкою: скролимо вертикально Body.
        /// </summary>

        private void SummaryLeftScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
            => ForwardWheelToSummaryBody(e);

        // =========================================================
        // 5) Bottom horizontal scrollbar (external)
        // =========================================================

        /// <summary>
        /// Нижній горизонтальний ScrollBar керує горизонтальним offset для Body + Header.
        /// </summary>
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

        /// <summary>
        /// Оновити межі/viewport для нижнього H-scrollbar.
        /// Викликаємо при:
        /// - body scroll changed,
        /// - body loaded,
        /// - body size changed,
        /// - matrix changed (бо змінився контент).
        /// </summary>
        private void UpdateSummaryHorizontalBar()
        {
            if (SummaryBodyScroll == null || SummaryHScroll == null)
                return;

            // ExtentWidth = повна ширина контенту
            // ViewportWidth = видима ширина
            var max = Math.Max(0, SummaryBodyScroll.ExtentWidth - SummaryBodyScroll.ViewportWidth);

            SummaryHScroll.Minimum = 0;
            SummaryHScroll.Maximum = max;

            // “довжина повзунка” — відносно viewport
            SummaryHScroll.ViewportSize = SummaryBodyScroll.ViewportWidth;

            SummaryHScroll.LargeChange = SummaryBodyScroll.ViewportWidth;
            SummaryHScroll.SmallChange = 30;

            if (!_summarySyncH)
                SummaryHScroll.Value = SummaryBodyScroll.HorizontalOffset;

            // (необов’язково) ховати якщо нема що скролити
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
