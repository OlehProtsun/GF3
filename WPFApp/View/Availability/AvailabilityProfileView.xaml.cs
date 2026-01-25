using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WPFApp.ViewModel.Availability;

namespace WPFApp.View.Availability
{
    /// <summary>
    /// Interaction logic for AvailabilityProfileView.xaml
    /// </summary>
    public partial class AvailabilityProfileView : UserControl
    {
        private AvailabilityProfileViewModel? _vm;

        public AvailabilityProfileView()
        {
            InitializeComponent();
            DataContextChanged += AvailabilityProfileView_DataContextChanged;
            Loaded += AvailabilityProfileView_Loaded;
            Unloaded += AvailabilityProfileView_Unloaded;
        }

        private void AvailabilityProfileView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AttachViewModel(DataContext as AvailabilityProfileViewModel);
        }

        private void AvailabilityProfileView_Loaded(object sender, RoutedEventArgs e)
        {
            AttachViewModel(DataContext as AvailabilityProfileViewModel);
        }

        private void AvailabilityProfileView_Unloaded(object sender, RoutedEventArgs e)
        {
            DetachViewModel();
        }

        private void AttachViewModel(AvailabilityProfileViewModel? viewModel)
        {
            if (_vm != null)
                _vm.MatrixChanged -= VmOnMatrixChanged;

            _vm = viewModel;
            if (_vm != null)
            {
                _vm.MatrixChanged += VmOnMatrixChanged;
                BuildMatrixColumns(_vm.ProfileAvailabilityMonths.Table, dataGridAvailabilityMonthProfile);
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
            BuildMatrixColumns(_vm.ProfileAvailabilityMonths.Table, dataGridAvailabilityMonthProfile);
        }

        private static void BuildMatrixColumns(DataTable? table, DataGrid grid)
        {
            if (table is null) return;

            grid.AutoGenerateColumns = false;
            grid.Columns.Clear();

            // як в Edit: перша колонка "заморожена"
            grid.FrozenColumnCount = 1;

            var tbStyle = (Style)Application.Current.FindResource("MatrixCellTextBlockStyle");

            foreach (DataColumn column in table.Columns)
            {
                var header = string.IsNullOrWhiteSpace(column.Caption)
                    ? column.ColumnName
                    : column.Caption;

                // ReadOnly: робимо OneWay, бо редагування не потрібне
                var b = new Binding($"[{column.ColumnName}]")
                {
                    Mode = BindingMode.OneWay
                };

                var col = new DataGridTextColumn
                {
                    Header = header,
                    Binding = b,
                    IsReadOnly = true,
                    ElementStyle = tbStyle
                };

                if (column.ColumnName == "DayOfMonth")
                {
                    col.Width = 60;
                }
                else
                {
                    col.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                    col.MinWidth = 140; // щоб нормально працював горизонтальний скрол
                }

                grid.Columns.Add(col);
            }
        }

    }
}
