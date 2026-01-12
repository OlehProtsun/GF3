using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using WPFApp.Infrastructure;
using WPFApp.Service;

namespace WPFApp.ViewModel.Employee
{
    public enum EmployeeSection
    {
        List,
        Edit,
        Profile
    }

    public sealed class EmployeeViewModel : ViewModelBase
    {
        private readonly IEmployeeService _employeeService;

        private bool _initialized;
        private int? _openedProfileEmployeeId;

        private object _currentSection = null!;
        public object CurrentSection
        {
            get => _currentSection;
            private set => SetProperty(ref _currentSection, value);
        }

        private EmployeeSection _mode = EmployeeSection.List;
        public EmployeeSection Mode
        {
            get => _mode;
            private set => SetProperty(ref _mode, value);
        }

        public EmployeeSection CancelTarget { get; private set; } = EmployeeSection.List;

        public EmployeeListViewModel ListVm { get; }
        public EmployeeEditViewModel EditVm { get; }
        public EmployeeProfileViewModel ProfileVm { get; }

        public EmployeeViewModel(IEmployeeService employeeService)
        {
            _employeeService = employeeService;

            ListVm = new EmployeeListViewModel(this);
            EditVm = new EmployeeEditViewModel(this);
            ProfileVm = new EmployeeProfileViewModel(this);

            CurrentSection = ListVm;
        }

        public async Task EnsureInitializedAsync(CancellationToken ct = default)
        {
            if (_initialized) return;

            _initialized = true;
            await LoadEmployeesAsync(ct);
        }

        internal async Task SearchAsync(CancellationToken ct = default)
        {
            var term = ListVm.SearchText;
            var list = string.IsNullOrWhiteSpace(term)
                ? await _employeeService.GetAllAsync(ct)
                : await _employeeService.GetByValueAsync(term, ct);

            ListVm.SetItems(list);
        }

        internal Task StartAddAsync(CancellationToken ct = default)
        {
            EditVm.ResetForNew();
            CancelTarget = EmployeeSection.List;
            return SwitchToEditAsync();
        }

        internal async Task EditSelectedAsync(CancellationToken ct = default)
        {
            var selected = ListVm.SelectedItem;
            if (selected is null) return;

            var latest = await _employeeService.GetAsync(selected.Id, ct) ?? selected;

            EditVm.SetEmployee(latest);

            CancelTarget = Mode == EmployeeSection.Profile
                ? EmployeeSection.Profile
                : EmployeeSection.List;

            await SwitchToEditAsync();
        }

        internal async Task SaveAsync(CancellationToken ct = default)
        {
            EditVm.ClearValidationErrors();

            var model = EditVm.ToModel();
            var errors = Validate(model);
            if (errors.Count > 0)
            {
                EditVm.SetValidationErrors(errors);
                return;
            }

            try
            {
                if (EditVm.IsEdit)
                {
                    await _employeeService.UpdateAsync(model, ct);
                }
                else
                {
                    var created = await _employeeService.CreateAsync(model, ct);
                    EditVm.EmployeeId = created.Id;
                    model = created;
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                return;
            }

            ShowInfo(EditVm.IsEdit
                ? "Employee updated successfully."
                : "Employee added successfully.");

            await LoadEmployeesAsync(ct, selectId: model.Id);

            if (CancelTarget == EmployeeSection.Profile)
            {
                var profileId = _openedProfileEmployeeId ?? model.Id;
                if (profileId > 0)
                {
                    var latest = await _employeeService.GetAsync(profileId, ct) ?? model;
                    ProfileVm.SetProfile(latest);
                    ListVm.SelectedItem = latest;
                }

                await SwitchToProfileAsync();
            }
            else
            {
                await SwitchToListAsync();
            }
        }

        internal async Task DeleteSelectedAsync(CancellationToken ct = default)
        {
            var currentId = GetCurrentEmployeeId();
            if (currentId <= 0) return;

            var currentName = Mode == EmployeeSection.Profile
                ? ProfileVm.FullName
                : ListVm.SelectedItem is null
                    ? string.Empty
                    : $"{ListVm.SelectedItem.FirstName} {ListVm.SelectedItem.LastName}".Trim();

            if (!Confirm(string.IsNullOrWhiteSpace(currentName)
                    ? "Delete employee?"
                    : $"Delete {currentName}?"))
            {
                return;
            }

            try
            {
                await _employeeService.DeleteAsync(currentId, ct);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                return;
            }

            ShowInfo("Employee deleted successfully.");

            await LoadEmployeesAsync(ct, selectId: null);
            await SwitchToListAsync();
        }

        internal async Task OpenProfileAsync(CancellationToken ct = default)
        {
            var selected = ListVm.SelectedItem;
            if (selected is null) return;

            var latest = await _employeeService.GetAsync(selected.Id, ct) ?? selected;

            _openedProfileEmployeeId = latest.Id;
            ProfileVm.SetProfile(latest);
            ListVm.SelectedItem = latest;

            CancelTarget = EmployeeSection.List;
            await SwitchToProfileAsync();
        }

        internal Task CancelAsync()
        {
            EditVm.ClearValidationErrors();

            return Mode switch
            {
                EmployeeSection.Edit => CancelTarget == EmployeeSection.Profile
                    ? SwitchToProfileAsync()
                    : SwitchToListAsync(),
                _ => SwitchToListAsync()
            };
        }

        private async Task LoadEmployeesAsync(CancellationToken ct, int? selectId = null)
        {
            var list = await _employeeService.GetAllAsync(ct);
            ListVm.SetItems(list);

            if (selectId.HasValue)
                ListVm.SelectedItem = list.FirstOrDefault(e => e.Id == selectId.Value);
        }

        private Task SwitchToListAsync()
        {
            CurrentSection = ListVm;
            Mode = EmployeeSection.List;
            return Task.CompletedTask;
        }

        private Task SwitchToEditAsync()
        {
            CurrentSection = EditVm;
            Mode = EmployeeSection.Edit;
            return Task.CompletedTask;
        }

        private Task SwitchToProfileAsync()
        {
            CurrentSection = ProfileVm;
            Mode = EmployeeSection.Profile;
            return Task.CompletedTask;
        }

        private int GetCurrentEmployeeId()
        {
            if (Mode == EmployeeSection.Profile)
                return ProfileVm.EmployeeId;

            return ListVm.SelectedItem?.Id ?? 0;
        }

        private static Dictionary<string, string> Validate(EmployeeModel model)
        {
            var map = new Dictionary<string, string>(capacity: 4);

            if (string.IsNullOrWhiteSpace(model.FirstName))
                map[nameof(model.FirstName)] = "First name is required.";

            if (string.IsNullOrWhiteSpace(model.LastName))
                map[nameof(model.LastName)] = "Last name is required.";

            if (!string.IsNullOrWhiteSpace(model.Email) && !EmployeeEditViewModel.EmailRegex.IsMatch(model.Email))
                map[nameof(model.Email)] = "Invalid email format.";

            if (!string.IsNullOrWhiteSpace(model.Phone) && !EmployeeEditViewModel.PhoneRegex.IsMatch(model.Phone))
                map[nameof(model.Phone)] = "Invalid phone number.";

            return map;
        }

        internal void ShowInfo(string text)
            => CustomMessageBox.Show("Info", text, CustomMessageBoxIcon.Info, okText: "OK");

        internal void ShowError(string text)
            => CustomMessageBox.Show("Error", text, CustomMessageBoxIcon.Error, okText: "OK");

        private bool Confirm(string text, string? caption = null)
            => CustomMessageBox.Show(
                caption ?? "Confirm",
                text,
                CustomMessageBoxIcon.Warning,
                okText: "Yes",
                cancelText: "No");
    }
}
