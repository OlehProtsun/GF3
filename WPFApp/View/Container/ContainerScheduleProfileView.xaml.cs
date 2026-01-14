using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Shapes;
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
                BuildMatrixColumns(_vm.ScheduleMatrix.Table, dataGridScheduleProfile);
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
            BuildMatrixColumns(_vm.ScheduleMatrix.Table, dataGridScheduleProfile);
        }

        private static void BuildMatrixColumns(DataTable? table, DataGrid grid)
        {
            if (table is null) return;

            grid.AutoGenerateColumns = false;
            grid.Columns.Clear();

            grid.FrozenColumnCount = 1;

            var dayColName = ContainerScheduleEditViewModel.DayColumnName;
            var conflictColName = ContainerScheduleEditViewModel.ConflictColumnName;

            var tbStyle = (Style)Application.Current.FindResource("MatrixCellTextBlockStyle");
            var dangerBrush = (System.Windows.Media.Brush)Application.Current.FindResource("DangerBrush");
            var boolToVis = new BooleanToVisibilityConverter();

            foreach (DataColumn column in table.Columns)
            {
                // 1) ВЗАГАЛІ не показуємо колонку Conflict
                if (column.ColumnName == conflictColName)
                    continue;

                var header = string.IsNullOrWhiteSpace(column.Caption) ? column.ColumnName : column.Caption;

                // 2) Колонка Day: текст + червоний кружок якщо Conflict == true
                if (column.ColumnName == dayColName)
                {
                    var templateCol = new DataGridTemplateColumn
                    {
                        Header = header,
                        Width = 70,
                        IsReadOnly = true
                    };

                    var gridFactory = new FrameworkElementFactory(typeof(Grid));

                    var textFactory = new FrameworkElementFactory(typeof(TextBlock));
                    textFactory.SetValue(FrameworkElement.StyleProperty, tbStyle);
                    textFactory.SetBinding(TextBlock.TextProperty, new Binding($"[{dayColName}]"));
                    gridFactory.AppendChild(textFactory);

                    var dotFactory = new FrameworkElementFactory(typeof(Ellipse));
                    dotFactory.SetValue(FrameworkElement.WidthProperty, 8.0);
                    dotFactory.SetValue(FrameworkElement.HeightProperty, 8.0);
                    dotFactory.SetValue(Shape.FillProperty, dangerBrush);
                    dotFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
                    dotFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
                    dotFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(4, 0, 0, 0));
                    dotFactory.SetBinding(UIElement.VisibilityProperty, new Binding($"[{conflictColName}]")
                    {
                        Converter = boolToVis
                    });
                    gridFactory.AppendChild(dotFactory);

                    templateCol.CellTemplate = new DataTemplate { VisualTree = gridFactory };

                    grid.Columns.Add(templateCol);
                    continue;
                }

                // 3) Інші колонки (працівники) залишаємо як текст
                var binding = new Binding($"[{column.ColumnName}]")
                {
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    ValidatesOnDataErrors = true,
                    NotifyOnValidationError = true
                };

                var col = new DataGridTextColumn
                {
                    Header = header,
                    Binding = binding,
                    IsReadOnly = true,
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                    ElementStyle = tbStyle
                };

                grid.Columns.Add(col);
            }
        }

        private void ScheduleMatrix_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            // 1) Прибираємо колонку Conflict
            if (string.Equals(e.PropertyName, "Conflict", StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true;
                return;
            }

            // 2) Для "днів" робимо шаблонну колонку
            // Тут припущення: день-колонки називаються Day1, Day2... або 1,2,3...
            // Підлаштуй під свої назви
            if (IsDayColumn(e.PropertyName))
            {
                var col = new DataGridTemplateColumn
                {
                    Header = e.Column.Header,
                    SortMemberPath = e.PropertyName,
                    CellTemplate = (DataTemplate)FindResource("DayCellTemplate")
                };

                // ВАЖЛИВО: кажемо, що в клітинці лежить об'єкт DayCell (див. нижче)
                col.CellTemplate.Seal(); // optional

                // Прив’язка для DataGridTemplateColumn робиться через ContentPresenter в шаблоні,
                // тому просто міняємо колонку, а DataContext клітинки буде значенням властивості.
                // Щоб так було, у твоїх рядків Day1/Day2/... мають бути типу DayCell.
                e.Column = col;
            }
        }

        private bool IsDayColumn(string name)
        {
            // Приклади: "Day1".."Day31" або "1".."31"
            if (name.StartsWith("Day", StringComparison.OrdinalIgnoreCase))
                return true;

            return int.TryParse(name, out var d) && d >= 1 && d <= 31;
        }
    }
}
