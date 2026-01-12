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
        }

        private void AvailabilityProfileView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_vm != null)
                _vm.MatrixChanged -= VmOnMatrixChanged;

            _vm = DataContext as AvailabilityProfileViewModel;
            if (_vm != null)
            {
                _vm.MatrixChanged += VmOnMatrixChanged;
                BuildMatrixColumns(_vm.ProfileAvailabilityMonths.Table, dataGridAvailabilityMonthProfile);
            }
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

            // стиль відображення (центр) — як в Edit
            var tbStyle = new Style(typeof(TextBlock));
            tbStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
            tbStyle.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center));
            tbStyle.Setters.Add(new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center));

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
