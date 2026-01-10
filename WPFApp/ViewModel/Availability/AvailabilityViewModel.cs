using BusinessLogicLayer.Availability;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using WPFApp.Infrastructure;

namespace WPFApp.ViewModel.Availability
{
    public enum AvailabilitySection
    {
        List,
        Edit,
        Profile
    }

    public sealed class AvailabilityViewModel : ViewModelBase
    {
        private readonly IAvailabilityGroupService _availabilityService;
        private readonly IEmployeeService _employeeService;
        private readonly IBindService _bindService;

        private readonly List<EmployeeModel> _allEmployees = new();
        private readonly Dictionary<int, string> _employeeNames = new();

        private bool _initialized;
        private int? _openedProfileGroupId;

        private object _currentSection = null!;
        public object CurrentSection
        {
            get => _currentSection;
            private set => SetProperty(ref _currentSection, value);
        }

        private AvailabilitySection _mode = AvailabilitySection.List;
        public AvailabilitySection Mode
        {
            get => _mode;
            private set => SetProperty(ref _mode, value);
        }

        public AvailabilitySection CancelTarget { get; private set; } = AvailabilitySection.List;

        public AvailabilityListViewModel ListVm { get; }
        public AvailabilityEditViewModel EditVm { get; }
        public AvailabilityProfileViewModel ProfileVm { get; }

        public AsyncRelayCommand ShowListCommand { get; }
        public AsyncRelayCommand ShowEditCommand { get; }
        public AsyncRelayCommand ShowProfileCommand { get; }

        public AvailabilityViewModel(
            IAvailabilityGroupService availabilityService,
            IEmployeeService employeeService,
            IBindService bindService)
        {
            _availabilityService = availabilityService;
            _employeeService = employeeService;
            _bindService = bindService;

            ListVm = new AvailabilityListViewModel(this);
            EditVm = new AvailabilityEditViewModel(this);
            ProfileVm = new AvailabilityProfileViewModel(this);

            ShowListCommand = new AsyncRelayCommand(() => SwitchToListAsync());
            ShowEditCommand = new AsyncRelayCommand(() => SwitchToEditAsync());
            ShowProfileCommand = new AsyncRelayCommand(() => SwitchToProfileAsync());

            CurrentSection = ListVm;
        }

        public async Task EnsureInitializedAsync(CancellationToken ct = default)
        {
            if (_initialized) return;

            _initialized = true;
            await LoadAllGroupsAsync(ct);
            await LoadEmployeesAsync(ct);
            await LoadBindsAsync(ct);
        }

        internal async Task SearchAsync(CancellationToken ct = default)
        {
            var term = ListVm.SearchText;
            var list = string.IsNullOrWhiteSpace(term)
                ? await _availabilityService.GetAllAsync(ct)
                : await _availabilityService.GetByValueAsync(term, ct);

            ListVm.SetItems(list);
        }

        internal async Task StartAddAsync(CancellationToken ct = default)
        {
            await LoadEmployeesAsync(ct);
            ResetEmployeeSearch();
            EditVm.ResetForNew();

            CancelTarget = AvailabilitySection.List;
            await SwitchToEditAsync();
        }

        internal async Task EditSelectedAsync(CancellationToken ct = default)
        {
            var selected = ListVm.SelectedItem;
            if (selected is null) return;

            await LoadEmployeesAsync(ct);

            var (group, members, days) = await _availabilityService.LoadFullAsync(selected.Id, ct);
            EditVm.LoadGroup(group, members, days, _employeeNames);

            CancelTarget = Mode == AvailabilitySection.Profile
                ? AvailabilitySection.Profile
                : AvailabilitySection.List;

            await SwitchToEditAsync();
        }

        internal async Task SaveAsync(CancellationToken ct = default)
        {
            EditVm.ClearValidationErrors();

            var rawName = (EditVm.AvailabilityName ?? string.Empty).Trim();
            var isNew = EditVm.AvailabilityId == 0;
            var suffix = $"{EditVm.AvailabilityMonth:D2}.{EditVm.AvailabilityYear}";

            var finalName = isNew
                ? $"{rawName} : {suffix}"
                : rawName;

            var group = new AvailabilityGroupModel
            {
                Id = EditVm.AvailabilityId,
                Name = finalName,
                Year = EditVm.AvailabilityYear,
                Month = EditVm.AvailabilityMonth
            };

            var errors = AvailabilityGroupValidator.Validate(group);
            if (errors.Count > 0)
            {
                EditVm.SetValidationErrors(errors);
                return;
            }

            var selectedEmployees = EditVm.GetSelectedEmployeeIds();
            if (selectedEmployees.Count == 0)
            {
            ShowError("Add at least 1 employee to the group.");
                return;
            }

            var raw = EditVm.ReadGroupCodes()
                .Where(x => selectedEmployees.Contains(x.employeeId));

            if (!AvailabilityPayloadBuilder.TryBuild(raw, out var payload, out var err))
            {
            ShowError(err ?? "Invalid availability codes.");
                return;
            }

            await _availabilityService.SaveGroupAsync(group, payload, ct);
            ShowInfo(isNew
                ? "Availability Group added successfully."
                : "Availability Group updated successfully.");

            await LoadAllGroupsAsync(ct);

            if (CancelTarget == AvailabilitySection.Profile)
            {
                var profileId = _openedProfileGroupId ?? group.Id;
                if (profileId > 0)
                {
                    var (g, members, days) = await _availabilityService.LoadFullAsync(profileId, ct);
                    ProfileVm.SetProfile(g, members, days);
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
            var current = ListVm.SelectedItem;
            if (current is null) return;

            if (!Confirm($"Delete '{current.Name}' ?"))
                return;

            await _availabilityService.DeleteAsync(current.Id, ct);
            ShowInfo("Availability Group deleted successfully.");

            await LoadAllGroupsAsync(ct);
            await SwitchToListAsync();
        }

        internal async Task OpenProfileAsync(CancellationToken ct = default)
        {
            var current = ListVm.SelectedItem;
            if (current is null) return;

            _openedProfileGroupId = current.Id;

            var (group, members, days) = await _availabilityService.LoadFullAsync(current.Id, ct);
            ProfileVm.SetProfile(group, members, days);

            CancelTarget = AvailabilitySection.List;
            await SwitchToProfileAsync();
        }

        internal Task CancelAsync()
        {
            EditVm.ClearValidationErrors();
            ResetEmployeeSearch();

            return Mode switch
            {
                AvailabilitySection.Edit => CancelTarget == AvailabilitySection.Profile
                    ? SwitchToProfileAsync()
                    : SwitchToListAsync(),
                _ => SwitchToListAsync()
            };
        }

        internal void ApplyEmployeeFilter(string? raw)
        {
            var term = raw?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(term))
            {
                EditVm.SetEmployees(_allEmployees, _employeeNames);
                return;
            }

            var filtered = _allEmployees
                .Where(e => ContainsIgnoreCase(e.FirstName, term) || ContainsIgnoreCase(e.LastName, term))
                .ToList();

            EditVm.SetEmployees(filtered, _employeeNames);
        }

        internal async Task AddBindAsync()
        {
            var row = new BindRow { IsActive = true };
            EditVm.Binds.Add(row);
            EditVm.SelectedBind = row;
            await Task.CompletedTask;
        }

        internal async Task DeleteBindAsync(CancellationToken ct = default)
        {
            var bind = EditVm.SelectedBind;
            if (bind is null) return;

            if (!Confirm($"Delete bind '{bind.Key}'?", "Confirm"))
                return;

            if (bind.Id == 0)
            {
                EditVm.Binds.Remove(bind);
                return;
            }

            await _bindService.DeleteAsync(bind.Id, ct);
            await LoadBindsAsync(ct);
        }

        internal async Task UpsertBindAsync(BindRow? bind, CancellationToken ct = default)
        {
            if (bind is null) return;

            if (string.IsNullOrWhiteSpace(bind.Key) && string.IsNullOrWhiteSpace(bind.Value))
                return;

            if (string.IsNullOrWhiteSpace(bind.Key) || string.IsNullOrWhiteSpace(bind.Value))
            {
                ShowError("Bind must contain both Key and Value.");
                return;
            }

            if (!TryNormalizeKey(bind.Key, out var normalizedKey))
            {
                ShowError("Invalid hotkey format.");
                return;
            }

            bind.Key = normalizedKey;

            var model = bind.ToModel();

            if (bind.Id == 0)
                await _bindService.CreateAsync(model, ct);
            else
                await _bindService.UpdateAsync(model, ct);

            await LoadBindsAsync(ct);
        }

        internal string? FormatKeyGesture(Key key, ModifierKeys modifiers)
        {
            if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin)
                return null;

            var converter = new KeyConverter();
            var keyString = converter.ConvertToString(key);
            if (string.IsNullOrWhiteSpace(keyString)) return null;

            var parts = new List<string>();
            if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
            if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
            if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
            if (modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");

            parts.Add(keyString);
            return string.Join("+", parts);
        }

        internal bool TryNormalizeKey(string raw, out string normalized)
        {
            normalized = string.Empty;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            var converter = new KeyGestureConverter();
            try
            {
                if (converter.ConvertFromString(raw.Trim()) is KeyGesture gesture)
                {
                    normalized = gesture.GetDisplayStringForCulture(CultureInfo.InvariantCulture);
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private async Task LoadAllGroupsAsync(CancellationToken ct = default)
        {
            var list = await _availabilityService.GetAllAsync(ct);
            ListVm.SetItems(list);
        }

        private async Task LoadEmployeesAsync(CancellationToken ct = default)
        {
            var employees = await _employeeService.GetAllAsync(ct);
            _allEmployees.Clear();
            _employeeNames.Clear();
            _allEmployees.AddRange(employees);

            foreach (var e in employees)
                _employeeNames[e.Id] = $"{e.FirstName} {e.LastName}";

            EditVm.SetEmployees(_allEmployees, _employeeNames);
        }

        private async Task LoadBindsAsync(CancellationToken ct = default)
        {
            var binds = await _bindService.GetAllAsync(ct);
            EditVm.SetBinds(binds);
        }

        private Task SwitchToListAsync()
        {
            CurrentSection = ListVm;
            Mode = AvailabilitySection.List;
            return Task.CompletedTask;
        }

        private Task SwitchToEditAsync()
        {
            CurrentSection = EditVm;
            Mode = AvailabilitySection.Edit;
            return Task.CompletedTask;
        }

        private Task SwitchToProfileAsync()
        {
            CurrentSection = ProfileVm;
            Mode = AvailabilitySection.Profile;
            return Task.CompletedTask;
        }

        private void ResetEmployeeSearch()
        {
            EditVm.EmployeeSearchText = string.Empty;
            EditVm.SetEmployees(_allEmployees, _employeeNames);
        }

        private static bool ContainsIgnoreCase(string? source, string value)
            => (source ?? string.Empty).Contains(value, StringComparison.OrdinalIgnoreCase);

        internal void ShowInfo(string text)
            => MessageBox.Show(text, "Info", MessageBoxButton.OK, MessageBoxImage.Information);

        internal void ShowError(string text)
            => MessageBox.Show(text, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

        private bool Confirm(string text, string? caption = null)
            => MessageBox.Show(text, caption ?? "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }
}
