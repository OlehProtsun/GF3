using DataAccessLayer.Models;
using WPFApp.Infrastructure;

namespace WPFApp.ViewModel.Employee
{
    public sealed class EmployeeProfileViewModel : ViewModelBase
    {
        private readonly EmployeeViewModel _owner;

        private int _employeeId;
        public int EmployeeId
        {
            get => _employeeId;
            set => SetProperty(ref _employeeId, value);
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

        public EmployeeProfileViewModel(EmployeeViewModel owner)
        {
            _owner = owner;

            BackCommand = new AsyncRelayCommand(() => _owner.CancelAsync());
            CancelProfileCommand = new AsyncRelayCommand(() => _owner.CancelAsync());
            EditCommand = new AsyncRelayCommand(() => _owner.EditSelectedAsync());
            DeleteCommand = new AsyncRelayCommand(() => _owner.DeleteSelectedAsync());
        }

        public void SetProfile(EmployeeModel model)
        {
            EmployeeId = model.Id;
            FullName = $"{model.FirstName} {model.LastName}".Trim();
            Email = string.IsNullOrWhiteSpace(model.Email) ? "—" : model.Email;
            Phone = string.IsNullOrWhiteSpace(model.Phone) ? "—" : model.Phone;
        }
    }
}
