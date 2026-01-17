using System.Windows;
using System.Windows.Controls;
using WPFApp.ViewModel.Container;

namespace WPFApp.View.Container
{
    /// <summary>
    /// Interaction logic for ContainerScheduleProfileView.xaml
    /// </summary>
    public partial class ContainerScheduleProfileView : UserControl
    {
        private ContainerScheduleProfileViewModel? _vm;

        public ContainerScheduleProfileView()
        {
            InitializeComponent();
            DataContextChanged += ContainerScheduleProfileView_DataContextChanged;
            Loaded += ContainerScheduleProfileView_Loaded;
            Unloaded += ContainerScheduleProfileView_Unloaded;
        }

        private void ContainerScheduleProfileView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AttachViewModel(DataContext as ContainerScheduleProfileViewModel);
        }

        private void ContainerScheduleProfileView_Loaded(object sender, RoutedEventArgs e)
        {
            AttachViewModel(DataContext as ContainerScheduleProfileViewModel);
        }

        private void ContainerScheduleProfileView_Unloaded(object sender, RoutedEventArgs e)
        {
            DetachViewModel();
        }

        private void AttachViewModel(ContainerScheduleProfileViewModel? viewModel)
        {
            if (_vm != null)
                _vm.MatrixChanged -= VmOnMatrixChanged;

            _vm = viewModel;
            if (_vm != null)
            {
                _vm.MatrixChanged += VmOnMatrixChanged;
                ScheduleMatrixColumnBuilder.BuildScheduleMatrixColumns(_vm.ScheduleMatrix.Table, dataGridScheduleProfile, isReadOnly: true);
            }
        }

        private void DetachViewModel()
        {
            if (_vm == null) return;
            _vm.MatrixChanged -= VmOnMatrixChanged;
        }

        private void VmOnMatrixChanged(object? sender, System.EventArgs e)
        {
            if (_vm is null) return;
            ScheduleMatrixColumnBuilder.BuildScheduleMatrixColumns(_vm.ScheduleMatrix.Table, dataGridScheduleProfile, isReadOnly: true);
        }
    }
}
