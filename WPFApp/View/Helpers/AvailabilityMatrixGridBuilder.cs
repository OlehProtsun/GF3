/*
  Опис файлу: цей модуль містить реалізацію компонента AvailabilityMatrixGridBuilder у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WPFApp.Applications.Matrix.Availability;

namespace WPFApp.View.Availability.Helpers
{
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public static class AvailabilityMatrixGridBuilder` та контракт його використання у шарі WPFApp.
    /// </summary>
    public static class AvailabilityMatrixGridBuilder
    {
        
        private static readonly string DayColumnName = AvailabilityMatrixEngine.DayColumnName;

        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static void BuildEditable(DataTable? table, DataGrid grid)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static void BuildEditable(DataTable? table, DataGrid grid)
        {
            
            if (table is null)
                return;

            
            grid.AutoGenerateColumns = false;

            
            grid.Columns.Clear();

            
            grid.FrozenColumnCount = 1;

            
            
            var tbStyle = (Style)Application.Current.FindResource("MatrixCellTextBlockStyle");
            var editStyle = (Style)Application.Current.FindResource("MatrixCellTextBoxStyle");

            
            foreach (DataColumn column in table.Columns)
            {
                
                var header = string.IsNullOrWhiteSpace(column.Caption)
                    ? column.ColumnName
                    : column.Caption;

                
                
                var binding = new Binding($"[{column.ColumnName}]")
                {
                    
                    
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,

                    
                    ValidatesOnDataErrors = true,

                    
                    NotifyOnValidationError = true,

                    
                    
                    Mode = BindingMode.TwoWay
                };

                
                var col = new DataGridTextColumn
                {
                    Header = header,
                    Binding = binding,

                    
                    
                    IsReadOnly = column.ReadOnly,

                    
                    ElementStyle = tbStyle,
                    EditingElementStyle = editStyle
                };

                
                if (column.ColumnName == DayColumnName)
                {
                    
                    col.IsReadOnly = true;

                    
                    binding.Mode = BindingMode.OneWay;

                    
                    col.Width = 60;
                }
                else
                {
                    
                    
                    
                    col.Width = new DataGridLength(1, DataGridLengthUnitType.Star);

                    
                    
                }

                
                grid.Columns.Add(col);
            }
        }

        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static void BuildReadOnly(DataTable? table, DataGrid grid)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static void BuildReadOnly(DataTable? table, DataGrid grid)
        {
            
            if (table is null)
                return;

            
            grid.AutoGenerateColumns = false;

            
            grid.Columns.Clear();

            
            grid.FrozenColumnCount = 1;

            
            var tbStyle = (Style)Application.Current.FindResource("MatrixCellTextBlockStyle");

            
            foreach (DataColumn column in table.Columns)
            {
                
                var header = string.IsNullOrWhiteSpace(column.Caption)
                    ? column.ColumnName
                    : column.Caption;

                
                var binding = new Binding($"[{column.ColumnName}]")
                {
                    Mode = BindingMode.OneWay
                };

                
                var col = new DataGridTextColumn
                {
                    Header = header,
                    Binding = binding,
                    IsReadOnly = true,
                    ElementStyle = tbStyle
                };

                
                if (column.ColumnName == DayColumnName)
                {
                    col.Width = 60;
                }
                else
                {
                    col.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                    col.MinWidth = 140;
                }

                
                grid.Columns.Add(col);
            }
        }
    }
}
