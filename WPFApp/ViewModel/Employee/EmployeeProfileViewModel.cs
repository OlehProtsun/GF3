using BusinessLogicLayer.Contracts.Employees;
using WPFApp.MVVM.Commands;
using WPFApp.MVVM.Core;
using WPFApp.ViewModel.Employee.Helpers;

namespace WPFApp.ViewModel.Employee
{
    /// <summary>
    /// EmployeeProfileViewModel — read-only профіль працівника.
    ///
    /// Покращення:
    /// - CancelProfileCommand = BackCommand (без дублювання AsyncRelayCommand)
    /// - Edit/Delete мають canExecute (лише коли EmployeeId > 0)
    /// - SetProfile синхронізує owner.ListVm.SelectedItem = model,
    ///   щоб owner.EditSelectedAsync/DeleteSelectedAsync працювали гарантовано на правильному employee.
    /// - FullName/Email/Phone формуються через EmployeeDisplayHelper (менше повторів).
    /// </summary>
    public sealed class EmployeeProfileViewModel : ViewModelBase
    {
        private readonly EmployeeViewModel _owner;

        private int _employeeId;
        public int EmployeeId
        {
            get => _employeeId;
            set
            {
                // При зміні Id оновлюємо canExecute для Edit/Delete.
                if (SetProperty(ref _employeeId, value))
                    UpdateCommands();
            }
        }

        private string _fullName = string.Empty;
        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string _phone = string.Empty;
        public string Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }

        public AsyncRelayCommand BackCommand { get; }
        public AsyncRelayCommand CancelProfileCommand { get; }
        public AsyncRelayCommand EditCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }

        private readonly AsyncRelayCommand[] _idDependentCommands;

        public EmployeeProfileViewModel(EmployeeViewModel owner)
        {
            _owner = owner;

            // Back/Cancel — одна логіка.
            BackCommand = new AsyncRelayCommand(() => _owner.CancelAsync());
            CancelProfileCommand = BackCommand;

            // Edit/Delete — доступні лише якщо профіль завантажено (Id>0).
            EditCommand = new AsyncRelayCommand(() => _owner.EditSelectedAsync(), () => EmployeeId > 0);
            DeleteCommand = new AsyncRelayCommand(() => _owner.DeleteSelectedAsync(), () => EmployeeId > 0);

            _idDependentCommands = new[] { EditCommand, DeleteCommand };
        }

        public void SetProfile(EmployeeDto model)
        {
            // 1) Синхронізуємо selection у owner’і (важливо для owner-методів).
            _owner.ListVm.SelectedItem = model;

            // 2) Заповнюємо поля.
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
