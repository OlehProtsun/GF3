using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Shapes;
using WPFApp.Infrastructure;
using WPFApp.ViewModel.Container;

namespace WPFApp.View.Container
{
    public static class ScheduleMatrixColumnBuilder
    {
        public static void BuildScheduleMatrixColumns(DataTable? table, DataGrid grid, bool isReadOnly)
        {
            if (table is null)
            {
                grid.ItemsSource = null;
                grid.Columns.Clear();
                return;
            }

            grid.ItemsSource = table.DefaultView;
            grid.AutoGenerateColumns = false;
            grid.Columns.Clear();
            grid.FrozenColumnCount = 1;

            var dayColName = ContainerScheduleEditViewModel.DayColumnName;
            var conflictColName = ContainerScheduleEditViewModel.ConflictColumnName;

            // ✅ додай
            var weekendColName = ContainerScheduleEditViewModel.WeekendColumnName;

            var tbStyle = (Style)Application.Current.FindResource("MatrixCellTextBlockStyle");
            var editStyle = isReadOnly
                ? null
                : (Style)Application.Current.FindResource("MatrixCellTextBoxStyle");
            var dangerBrush = (System.Windows.Media.Brush)Application.Current.FindResource("DangerBrush");
            var boolToVis = new BooleanToVisibilityConverter();

            foreach (DataColumn column in table.Columns)
            {
                // ✅ було: if (column.ColumnName == conflictColName) continue;
                if (column.ColumnName == conflictColName || column.ColumnName == weekendColName)
                    continue;

                var header = string.IsNullOrWhiteSpace(column.Caption) ? column.ColumnName : column.Caption;

                if (column.ColumnName == dayColName)
                {
                    var templateCol = new DataGridTemplateColumn
                    {
                        Header = header,
                        Width = 70,
                        IsReadOnly = true,
                        SortMemberPath = dayColName
                    };

                    var root = new FrameworkElementFactory(typeof(Grid));

                    var txt = new FrameworkElementFactory(typeof(TextBlock));
                    txt.SetValue(FrameworkElement.StyleProperty, tbStyle);
                    txt.SetValue(FrameworkElement.MarginProperty, new Thickness(14, 0, 0, 0));
                    txt.SetBinding(TextBlock.TextProperty, new Binding($"[{dayColName}]"));
                    root.AppendChild(txt);

                    var dot = new FrameworkElementFactory(typeof(Ellipse));
                    dot.SetValue(FrameworkElement.WidthProperty, 8.0);
                    dot.SetValue(FrameworkElement.HeightProperty, 8.0);
                    dot.SetValue(Shape.FillProperty, dangerBrush);
                    dot.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
                    dot.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
                    dot.SetValue(FrameworkElement.MarginProperty, new Thickness(4, 0, 0, 0));
                    dot.SetBinding(UIElement.VisibilityProperty, new Binding($"[{conflictColName}]")
                    {
                        Converter = boolToVis
                    });
                    root.AppendChild(dot);

                    templateCol.CellTemplate = new DataTemplate { VisualTree = root };

                    grid.Columns.Add(templateCol);
                    continue;
                }

                var binding = new Binding($"[{column.ColumnName}]")
                {
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    ValidatesOnDataErrors = true,
                    NotifyOnValidationError = true
                };

                var col = new DataGridTextColumn
                {
                    Header = header,
                    SortMemberPath = column.ColumnName,
                    Binding = binding,
                    IsReadOnly = isReadOnly || column.ReadOnly,
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                    ElementStyle = tbStyle
                };

                if (!isReadOnly && editStyle != null)
                    col.EditingElementStyle = editStyle;

                grid.Columns.Add(col);
            }

            MatrixRefreshDiagnostics.RecordColumnBuild(
                grid.Name,
                MatrixRefreshDiagnostics.BuildColumnSignature(table),
                grid.Columns.Count,
                grid.FrozenColumnCount);
        }
    }
}
