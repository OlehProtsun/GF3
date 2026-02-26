/*
  Опис файлу: цей модуль містить реалізацію компонента AvailabilityProfileView у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Windows;
using System.Windows.Controls;
using WPFApp.View.Availability.Helpers;
using WPFApp.ViewModel.Availability.Profile;

namespace WPFApp.View.Availability
{
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public partial class AvailabilityProfileView : UserControl` та контракт його використання у шарі WPFApp.
    /// </summary>
    public partial class AvailabilityProfileView : UserControl
    {
        
        private AvailabilityProfileViewModel? _vm;

        /// <summary>
        /// Визначає публічний елемент `public AvailabilityProfileView()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AvailabilityProfileView()
        {
            InitializeComponent();

            
            DataContextChanged += OnDataContextChanged;

            
            Unloaded += OnUnloaded;

            
            Loaded += OnLoaded;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AttachViewModel(DataContext as AvailabilityProfileViewModel);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AttachViewModel(DataContext as AvailabilityProfileViewModel);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DetachViewModel();
        }

        private void AttachViewModel(AvailabilityProfileViewModel? viewModel)
        {
            
            if (ReferenceEquals(_vm, viewModel))
                return;

            
            DetachViewModel();

            
            _vm = viewModel;

            
            if (_vm is null)
                return;

            
            _vm.MatrixChanged += VmOnMatrixChanged;

            
            AvailabilityMatrixGridBuilder.BuildReadOnly(_vm.ProfileAvailabilityMonths.Table, dataGridAvailabilityMonthProfile);
        }

        private void DetachViewModel()
        {
            
            if (_vm is null)
                return;

            
            _vm.MatrixChanged -= VmOnMatrixChanged;

            
            _vm = null;
        }

        private void VmOnMatrixChanged(object? sender, EventArgs e)
        {
            
            if (_vm is null)
                return;

            
            
            if (!Dispatcher.CheckAccess())
            {
                _ = Dispatcher.BeginInvoke(new Action(() => VmOnMatrixChanged(sender, e)));
                return;
            }

            
            AvailabilityMatrixGridBuilder.BuildReadOnly(_vm.ProfileAvailabilityMonths.Table, dataGridAvailabilityMonthProfile);
        }
    }
}