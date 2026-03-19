/*
  Опис файлу: цей модуль містить реалізацію компонента AvailabilityListView у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using WPFApp.ViewModel.Availability.List;

namespace WPFApp.View.Availability
{
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public partial class AvailabilityListView : UserControl` та контракт його використання у шарі WPFApp.
    /// </summary>
    public partial class AvailabilityListView : UserControl
    {
        /// <summary>
        /// Визначає публічний елемент `public AvailabilityListView()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AvailabilityListView()
        {
            InitializeComponent();

            
            
            
            
            dataGridAvailabilityList.MouseDoubleClick += DataGridAvailabilityList_MouseDoubleClick;
        }

        
        
        
        private void InputSearch_KeyDown(object sender, KeyEventArgs e)
        {
            
            if (e.Key != Key.Enter)
                return;

            
            if (DataContext is not AvailabilityListViewModel vm)
                return;

            
            if (vm.SearchCommand.CanExecute(null))
                vm.SearchCommand.Execute(null);

            
            e.Handled = true;
        }

        
        
        
        private void DataGridAvailabilityList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            
            TryOpenProfileFromUi();

            
            e.Handled = true;
        }

        
        
        
        
        private void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TryOpenProfileFromUi();
            e.Handled = true;
        }

        
        
        
        private void TryOpenProfileFromUi()
        {
            
            if (DataContext is not AvailabilityListViewModel vm)
                return;

            
            if (vm.OpenProfileCommand.CanExecute(null))
                vm.OpenProfileCommand.Execute(null);
        }

        
        
        
        
        
        
        
        
        private void RowHitArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            
            if (sender is not DependencyObject dep)
                return;

            
            
            
            
            var row = ItemsControl.ContainerFromElement(dataGridAvailabilityList, dep) as DataGridRow;

            
            if (row != null)
            {
                row.IsSelected = true;
                row.Focus();

                
                e.Handled = true;
            }
        }
    }
}
