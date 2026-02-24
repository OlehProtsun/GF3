using System.Threading.Tasks;
using System.Windows;
using WPFApp.MVVM.Commands;

namespace WPFApp.ViewModel.Availability.Edit
{
    /// <summary>
    /// Команди: лише оголошення properties + реалізація handlers.
    /// </summary>
    public sealed partial class AvailabilityEditViewModel
    {
        // Публічні команди (ініціалізуються у constructor в основному файлі).
        public AsyncRelayCommand SaveCommand { get; }
        public AsyncRelayCommand CancelCommand { get; }
        public AsyncRelayCommand CancelInformationCommand { get; }
        public AsyncRelayCommand CancelEmployeeCommand { get; }
        public AsyncRelayCommand CancelBindCommand { get; }
        public AsyncRelayCommand AddEmployeeCommand { get; }
        public AsyncRelayCommand RemoveEmployeeCommand { get; }
        public AsyncRelayCommand SearchEmployeeCommand { get; }
        public AsyncRelayCommand AddBindCommand { get; }
        public AsyncRelayCommand DeleteBindCommand { get; }

        // ----------------------------
        // Command handlers
        // ----------------------------

        private async Task SaveWithValidationAsync()
        {
            // Валідацію виконуємо на UI thread (щоб візуалка WPF оновилась стабільно)
            var ok = await Application.Current.Dispatcher
                .InvokeAsync(() => ValidateBeforeSave(showDialog: true));

            if (!ok)
                return;

            await _owner.SaveAsync().ConfigureAwait(false);
        }

        private async Task AddEmployeeAsync()
        {
            // 1) Беремо id вибраного працівника.
            int empId = SelectedEmployee?.Id ?? 0;

            // 2) Якщо не вибрано — показуємо помилку.
            if (empId <= 0)
            {
                _owner.ShowError("Select employee first.");
                return;
            }

            // 3) Підбираємо header (caption) для колонки.
            var header = _employeeNames.TryGetValue(empId, out var name)
                ? name
                : $"Employee #{empId}";

            // 4) Пробуємо додати колонку.
            if (!TryAddEmployeeColumn(empId, header))
            {
                _owner.ShowInfo("This employee is already added.");
                return;
            }

            // 5) Показуємо короткий Working -> Success popup
            await _owner.FlashNavWorkingSuccessAsync();
        }


        private async Task RemoveEmployeeAsync()
        {
            // 1) Беремо id вибраного працівника.
            int empId = SelectedEmployee?.Id ?? 0;

            // 2) Якщо не вибрано — повідомляємо.
            if (empId <= 0)
            {
                _owner.ShowError("Select employee first.");
                return;
            }

            // 3) Пробуємо видалити колонку.
            if (!RemoveEmployeeColumn(empId))
            {
                _owner.ShowInfo("This employee is not in the group.");
                return;
            }

            // 4) Показуємо короткий Working -> Success popup
            await _owner.FlashNavWorkingSuccessAsync();
        }
        private Task SearchEmployeeAsync()
        {
            // 1) Делегуємо фільтрацію в owner.
            //    Owner — центральне місце логіки пошуку/фільтрації.
            _owner.ApplyEmployeeFilter(EmployeeSearchText);

            return Task.CompletedTask;
        }
    }
}
