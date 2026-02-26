/*
  Опис файлу: цей модуль містить реалізацію компонента EmployeeProfileViewModel у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Employees;
using WPFApp.MVVM.Commands;
using WPFApp.MVVM.Core;
using WPFApp.ViewModel.Employee.Helpers;

namespace WPFApp.ViewModel.Employee
{
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class EmployeeProfileViewModel : ViewModelBase` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class EmployeeProfileViewModel : ViewModelBase
    {
        private readonly EmployeeViewModel _owner;

        private int _employeeId;
        /// <summary>
        /// Визначає публічний елемент `public int EmployeeId` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int EmployeeId
        {
            get => _employeeId;
            set
            {
                
                if (SetProperty(ref _employeeId, value))
                    UpdateCommands();
            }
        }

        private string _fullName = string.Empty;
        /// <summary>
        /// Визначає публічний елемент `public string FullName` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        private string _email = string.Empty;
        /// <summary>
        /// Визначає публічний елемент `public string Email` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string _phone = string.Empty;
        /// <summary>
        /// Визначає публічний елемент `public string Phone` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }

        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand BackCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand BackCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand CancelProfileCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand CancelProfileCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand EditCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand EditCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand DeleteCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand DeleteCommand { get; }

        private readonly AsyncRelayCommand[] _idDependentCommands;

        /// <summary>
        /// Визначає публічний елемент `public EmployeeProfileViewModel(EmployeeViewModel owner)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public EmployeeProfileViewModel(EmployeeViewModel owner)
        {
            _owner = owner;

            
            BackCommand = new AsyncRelayCommand(() => _owner.CancelAsync());
            CancelProfileCommand = BackCommand;

            
            EditCommand = new AsyncRelayCommand(() => _owner.EditSelectedAsync(), () => EmployeeId > 0);
            DeleteCommand = new AsyncRelayCommand(() => _owner.DeleteSelectedAsync(), () => EmployeeId > 0);

            _idDependentCommands = new[] { EditCommand, DeleteCommand };
        }

        /// <summary>
        /// Визначає публічний елемент `public void SetProfile(EmployeeDto model)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void SetProfile(EmployeeDto model)
        {
            
            _owner.ListVm.SelectedItem = model;

            
            EmployeeId = model.Id;
            FullName = EmployeeDisplayHelper.GetFullName(model);
            Email = EmployeeDisplayHelper.TextOrDash(model.Email);
            Phone = EmployeeDisplayHelper.TextOrDash(model.Phone);
        }

        private void UpdateCommands()
        {
            for (int i = 0; i < _idDependentCommands.Length; i++)
                _idDependentCommands[i].RaiseCanExecuteChanged();
        }
    }
}
