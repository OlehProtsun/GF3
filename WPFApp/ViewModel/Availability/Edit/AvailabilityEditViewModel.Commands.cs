/*
  Опис файлу: цей модуль містить реалізацію компонента AvailabilityEditViewModel.Commands у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System.Threading.Tasks;
using System.Windows;
using WPFApp.MVVM.Commands;

namespace WPFApp.ViewModel.Availability.Edit
{
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class AvailabilityEditViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class AvailabilityEditViewModel
    {
        
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand SaveCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand SaveCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand CancelCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand CancelCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand CancelInformationCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand CancelInformationCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand CancelEmployeeCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand CancelEmployeeCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand CancelBindCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand CancelBindCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand AddEmployeeCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand AddEmployeeCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand RemoveEmployeeCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand RemoveEmployeeCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand SearchEmployeeCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand SearchEmployeeCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand AddBindCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand AddBindCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand DeleteBindCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand DeleteBindCommand { get; }

        
        
        

        private async Task SaveWithValidationAsync()
        {
            
            var ok = await Application.Current.Dispatcher
                .InvokeAsync(() => ValidateBeforeSave(showDialog: true));

            if (!ok)
                return;

            await _owner.SaveAsync().ConfigureAwait(false);
        }

        private async Task AddEmployeeAsync()
        {
            
            int empId = SelectedEmployee?.Id ?? 0;

            
            if (empId <= 0)
            {
                _owner.ShowError("Select employee first.");
                return;
            }

            
            var header = _employeeNames.TryGetValue(empId, out var name)
                ? name
                : $"Employee #{empId}";

            
            if (!TryAddEmployeeColumn(empId, header))
            {
                _owner.ShowInfo("This employee is already added.");
                return;
            }

            
            await _owner.FlashNavWorkingSuccessAsync();
        }


        private async Task RemoveEmployeeAsync()
        {
            
            int empId = SelectedEmployee?.Id ?? 0;

            
            if (empId <= 0)
            {
                _owner.ShowError("Select employee first.");
                return;
            }

            
            if (!RemoveEmployeeColumn(empId))
            {
                _owner.ShowInfo("This employee is not in the group.");
                return;
            }

            
            await _owner.FlashNavWorkingSuccessAsync();
        }
        private Task SearchEmployeeAsync()
        {
            
            
            _owner.ApplyEmployeeFilter(EmployeeSearchText);

            return Task.CompletedTask;
        }
    }
}
