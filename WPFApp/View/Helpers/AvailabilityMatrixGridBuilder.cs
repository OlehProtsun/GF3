using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WPFApp.Infrastructure.AvailabilityMatrix;

namespace WPFApp.View.Availability.Helpers
{
    /// <summary>
    /// AvailabilityMatrixGridBuilder — централізована побудова колонок DataGrid з DataTable.
    ///
    /// Дві модифікації:
    /// - BuildEditable: для Edit (TwoWay, validation, EditingElementStyle)
    /// - BuildReadOnly: для Profile (OneWay, IsReadOnly)
    ///
    /// Чому в helper:
    /// - одна логіка в 2-х view (Edit/Profile)
    /// - менше шансів розсинхронити ширини/стилі/Day column
    /// </summary>
    public static class AvailabilityMatrixGridBuilder
    {
        // Єдина “правда” про назву колонки дня.
        private const string DayColumnName = AvailabilityMatrixEngine.DayColumnName;

        /// <summary>
        /// Побудувати колонки для editable матриці (EditView):
        /// - TwoWay binding
        /// - UpdateSourceTrigger=PropertyChanged
        /// - ValidatesOnDataErrors (щоб DataTable ColumnError показувався як validation)
        /// - стилі: MatrixCellTextBlockStyle / MatrixCellTextBoxStyle
        /// </summary>
        public static void BuildEditable(DataTable? table, DataGrid grid)
        {
            // 1) Якщо немає таблиці — немає що будувати.
            if (table is null)
                return;

            // 2) Забороняємо auto-generation, бо ми будуємо колонки самі.
            grid.AutoGenerateColumns = false;

            // 3) Очищаємо попередні колонки (важливо при rebuild після MatrixChanged).
            grid.Columns.Clear();

            // 4) “Заморожуємо” першу колонку (Day) як у твоєму початковому коді.
            grid.FrozenColumnCount = 1;

            // 5) Дістаємо стилі ОДИН раз, а не на кожну колонку.
            //    Це дешевше і чистіше.
            var tbStyle = (Style)Application.Current.FindResource("MatrixCellTextBlockStyle");
            var editStyle = (Style)Application.Current.FindResource("MatrixCellTextBoxStyle");

            // 6) Перебираємо DataColumn-и таблиці і будуємо DataGridTextColumn.
            foreach (DataColumn column in table.Columns)
            {
                // 6.1) Header: якщо Caption порожній — використовуємо ColumnName.
                var header = string.IsNullOrWhiteSpace(column.Caption)
                    ? column.ColumnName
                    : column.Caption;

                // 6.2) Binding у DataRowView по індексатору: ["colName"].
                //      Для DataView це найпростіший і правильний шлях.
                var binding = new Binding($"[{column.ColumnName}]")
                {
                    // 6.3) В Edit хочемо оновлювати DataTable під час вводу,
                    //      щоб ColumnChanged міг ставити ColumnError.
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,

                    // 6.4) Включаємо “валидацію” через IDataErrorInfo/DataTable errors.
                    ValidatesOnDataErrors = true,

                    // 6.5) Нехай WPF піднімає Validation.Error events (корисно для стилів).
                    NotifyOnValidationError = true,

                    // 6.6) Для Day column нижче ми встановимо OneWay,
                    //      для employee — залишимо TwoWay.
                    Mode = BindingMode.TwoWay
                };

                // 6.7) Створюємо колонку.
                var col = new DataGridTextColumn
                {
                    Header = header,
                    Binding = binding,

                    // 6.8) ReadOnly беремо з DataColumn.ReadOnly.
                    //      Day column зазвичай ReadOnly=true.
                    IsReadOnly = column.ReadOnly,

                    // 6.9) Стилі відображення/редагування.
                    ElementStyle = tbStyle,
                    EditingElementStyle = editStyle
                };

                // 6.10) Day column: фіксована ширина, OneWay, ReadOnly.
                if (column.ColumnName == DayColumnName)
                {
                    // Day — не редагується.
                    col.IsReadOnly = true;

                    // Day — логічно OneWay, щоб WPF не намагався записувати назад.
                    binding.Mode = BindingMode.OneWay;

                    // Фіксована ширина.
                    col.Width = 60;
                }
                else
                {
                    // 6.11) Employee колонки:
                    // - займають весь доступний простір (Star)
                    // - але щоб не “злипались”, можна ставити MinWidth.
                    col.Width = new DataGridLength(1, DataGridLengthUnitType.Star);

                    // Якщо хочеш такий самий UX як у Profile — можеш розкоментувати:
                    // col.MinWidth = 140;
                }

                // 6.12) Додаємо колонку в grid.
                grid.Columns.Add(col);
            }
        }

        /// <summary>
        /// Побудувати колонки для read-only матриці (ProfileView):
        /// - OneWay binding
        /// - IsReadOnly=true
        /// - стилі: MatrixCellTextBlockStyle
        /// </summary>
        public static void BuildReadOnly(DataTable? table, DataGrid grid)
        {
            // 1) Нема таблиці — нема роботи.
            if (table is null)
                return;

            // 2) Забороняємо auto-generation.
            grid.AutoGenerateColumns = false;

            // 3) Чистимо колонки.
            grid.Columns.Clear();

            // 4) Заморожуємо Day.
            grid.FrozenColumnCount = 1;

            // 5) Стиль для TextBlock (відображення).
            var tbStyle = (Style)Application.Current.FindResource("MatrixCellTextBlockStyle");

            // 6) Колонки по DataTable.
            foreach (DataColumn column in table.Columns)
            {
                // 6.1) Header.
                var header = string.IsNullOrWhiteSpace(column.Caption)
                    ? column.ColumnName
                    : column.Caption;

                // 6.2) OneWay binding (profile — read-only).
                var binding = new Binding($"[{column.ColumnName}]")
                {
                    Mode = BindingMode.OneWay
                };

                // 6.3) Колонка.
                var col = new DataGridTextColumn
                {
                    Header = header,
                    Binding = binding,
                    IsReadOnly = true,
                    ElementStyle = tbStyle
                };

                // 6.4) Day — вузька; employee — star+minwidth для нормального горизонтального скролу.
                if (column.ColumnName == DayColumnName)
                {
                    col.Width = 60;
                }
                else
                {
                    col.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                    col.MinWidth = 140;
                }

                // 6.5) Додаємо.
                grid.Columns.Add(col);
            }
        }
    }
}
