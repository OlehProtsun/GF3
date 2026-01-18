using BusinessLogicLayer.Availability;
using BusinessLogicLayer.Generators;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WPFApp.Infrastructure;
using WPFApp.Service;
using WPFApp.ViewModel.Dialogs;

namespace WPFApp.ViewModel.Container
{
    public enum ContainerSection
    {
        List,
        Edit,
        Profile,
        ScheduleEdit,
        ScheduleProfile
    }

    public sealed class ContainerViewModel : ViewModelBase
    {
        private readonly IContainerService _containerService;
        private readonly IScheduleService _scheduleService;
        private readonly IAvailabilityGroupService _availabilityGroupService;
        private readonly IShopService _shopService;
        private readonly IEmployeeService _employeeService;
        private readonly IScheduleGenerator _generator;
        private readonly IColorPickerService _colorPickerService;

        private readonly List<ShopModel> _allShops = new();
        private readonly List<AvailabilityGroupModel> _allAvailabilityGroups = new();
        private readonly List<EmployeeModel> _allEmployees = new();

        private bool _initialized;
        private int? _openedProfileContainerId;

        private object _currentSection = null!;
        private const int MaxOpenedSchedules = 20;
        public object CurrentSection
        {
            get => _currentSection;
            private set => SetProperty(ref _currentSection, value);
        }

        private ContainerSection _mode = ContainerSection.List;
        public ContainerSection Mode
        {
            get => _mode;
            private set => SetProperty(ref _mode, value);
        }

        public ContainerSection CancelTarget { get; private set; } = ContainerSection.List;
        public ContainerSection ScheduleCancelTarget { get; private set; } = ContainerSection.Profile;

        public ContainerListViewModel ListVm { get; }
        public ContainerEditViewModel EditVm { get; }
        public ContainerProfileViewModel ProfileVm { get; }
        public ContainerScheduleEditViewModel ScheduleEditVm { get; }
        public ContainerScheduleProfileViewModel ScheduleProfileVm { get; }

        public ContainerViewModel(
            IContainerService containerService,
            IScheduleService scheduleService,
            IAvailabilityGroupService availabilityGroupService,
            IShopService shopService,
            IEmployeeService employeeService,
            IScheduleGenerator generator,
            IColorPickerService colorPickerService)
        {
            _containerService = containerService;
            _scheduleService = scheduleService;
            _availabilityGroupService = availabilityGroupService;
            _shopService = shopService;
            _employeeService = employeeService;
            _generator = generator;
            _colorPickerService = colorPickerService;

            ListVm = new ContainerListViewModel(this);
            EditVm = new ContainerEditViewModel(this);
            ProfileVm = new ContainerProfileViewModel(this);
            ScheduleEditVm = new ContainerScheduleEditViewModel(this);
            ScheduleProfileVm = new ContainerScheduleProfileViewModel(this);

            CurrentSection = ListVm;
        }

        internal bool TryPickScheduleCellColor(System.Windows.Media.Color? initialColor, out System.Windows.Media.Color color)
            => _colorPickerService.TryPickColor(initialColor, out color);

        public async Task EnsureInitializedAsync(CancellationToken ct = default)
        {
            if (_initialized) return;

            _initialized = true;
            await LoadContainersAsync(ct);
        }

        internal async Task SearchAsync(CancellationToken ct = default)
        {
            var term = ListVm.SearchText;
            var list = string.IsNullOrWhiteSpace(term)
                ? await _containerService.GetAllAsync(ct)
                : await _containerService.GetByValueAsync(term, ct);

            ListVm.SetItems(list);
        }

        internal Task StartAddAsync(CancellationToken ct = default)
        {
            EditVm.ResetForNew();
            CancelTarget = ContainerSection.List;
            return SwitchToEditAsync();
        }

        internal async Task EditSelectedAsync(CancellationToken ct = default)
        {
            var id = GetCurrentContainerId();
            if (id <= 0) return;

            var latest = await _containerService.GetAsync(id, ct);
            if (latest is null) return;

            EditVm.SetContainer(latest);

            CancelTarget = Mode == ContainerSection.Profile
                ? ContainerSection.Profile
                : ContainerSection.List;

            await SwitchToEditAsync();
        }


        internal async Task SaveAsync(CancellationToken ct = default)
        {
            EditVm.ClearValidationErrors();

            var model = EditVm.ToModel();
            var errors = ValidateContainer(model);
            if (errors.Count > 0)
            {
                EditVm.SetValidationErrors(errors);
                return;
            }

            try
            {
                if (EditVm.IsEdit)
                {
                    await _containerService.UpdateAsync(model, ct);
                }
                else
                {
                    var created = await _containerService.CreateAsync(model, ct);
                    EditVm.ContainerId = created.Id;
                    model = created;
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
                return;
            }

            ShowInfo(EditVm.IsEdit
                ? "Container updated successfully."
                : "Container added successfully.");

            await LoadContainersAsync(ct, selectId: model.Id);

            if (CancelTarget == ContainerSection.Profile)
            {
                var profileId = _openedProfileContainerId ?? model.Id;
                if (profileId > 0)
                {
                    var latest = await _containerService.GetAsync(profileId, ct) ?? model;
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
            var currentId = GetCurrentContainerId();
            if (currentId <= 0) return;

            var currentName = Mode == ContainerSection.Profile
                ? ProfileVm.Name
                : ListVm.SelectedItem?.Name ?? string.Empty;

            if (!Confirm(string.IsNullOrWhiteSpace(currentName)
                    ? "Delete container?"
                    : $"Delete {currentName}?"))
            {
                return;
            }

            try
            {
                await _containerService.DeleteAsync(currentId, ct);
            }
            catch (Exception ex)
            {
                ShowError(ex);
                return;
            }

            ShowInfo("Container deleted successfully.");

            await LoadContainersAsync(ct, selectId: null);
            await SwitchToListAsync();
        }

        internal async Task OpenProfileAsync(CancellationToken ct = default)
        {
            var selected = ListVm.SelectedItem;
            if (selected is null) return;

            var latest = await _containerService.GetAsync(selected.Id, ct) ?? selected;

            _openedProfileContainerId = latest.Id;
            ProfileVm.SetProfile(latest);
            ListVm.SelectedItem = latest;

            await LoadSchedulesAsync(latest.Id, search: null, ct);

            CancelTarget = ContainerSection.List;
            await SwitchToProfileAsync();
        }

        internal Task CancelAsync()
        {
            EditVm.ClearValidationErrors();

            return Mode switch
            {
                ContainerSection.Edit => CancelTarget == ContainerSection.Profile
                    ? SwitchToProfileAsync()
                    : SwitchToListAsync(),
                _ => SwitchToListAsync()
            };
        }

        internal async Task SearchScheduleAsync(CancellationToken ct = default)
        {
            var containerId = GetCurrentContainerId();
            if (containerId <= 0)
            {
                ShowError("Select a container first.");
                return;
            }

            await LoadSchedulesAsync(containerId, ProfileVm.ScheduleListVm.SearchText, ct);
        }

        internal async Task StartScheduleAddAsync(CancellationToken ct = default)
        {
            var containerId = GetCurrentContainerId();
            if (containerId <= 0)
            {
                ShowError("Select a container first.");
                return;
            }

            await LoadLookupsAsync(ct);
            ResetScheduleFilters();

            ScheduleEditVm.ClearValidationErrors();
            ScheduleEditVm.ResetForNew();
            ScheduleEditVm.IsEdit = false;
            ScheduleEditVm.Blocks.Clear();

            var block = CreateDefaultBlock(containerId);
            ScheduleEditVm.Blocks.Add(block);
            ScheduleEditVm.SelectedBlock = block;

            ScheduleEditVm.RefreshScheduleMatrix();
            ScheduleEditVm.RefreshAvailabilityPreviewMatrix(block.Model.Year, block.Model.Month, new List<ScheduleSlotModel>(), new List<ScheduleEmployeeModel>());

            ScheduleCancelTarget = ContainerSection.Profile;
            await SwitchToScheduleEditAsync();
        }

        internal async Task EditSelectedScheduleAsync(CancellationToken ct = default)
        {
            var schedule = ProfileVm.ScheduleListVm.SelectedItem?.Model;
            if (schedule is null) return;

            await LoadLookupsAsync(ct);
            ResetScheduleFilters();

            var detailed = await _scheduleService.GetDetailedAsync(schedule.Id, ct);
            if (detailed is null) return;
            if (!HasGeneratedContent(detailed))
            {
                ShowError("This schedule doesn’t contain generated data and can’t be edited. Please run generation first.");
                return;
            }

            ScheduleEditVm.ClearValidationErrors();
            ScheduleEditVm.ResetForNew();
            ScheduleEditVm.IsEdit = true;

            ScheduleEditVm.Blocks.Clear();

            var block = CreateBlockFromSchedule(detailed,
                detailed.Employees?.ToList() ?? new List<ScheduleEmployeeModel>(),
                detailed.Slots?.ToList() ?? new List<ScheduleSlotModel>(),
                detailed.CellStyles?.ToList());

            block.SelectedAvailabilityGroupId = detailed.AvailabilityGroupId!.Value; // <-- саме група, з якої був генерований графік

            ScheduleEditVm.Blocks.Add(block);
            ScheduleEditVm.SelectedBlock = block;
            ScheduleEditVm.RefreshScheduleMatrix();

            ScheduleCancelTarget = Mode == ContainerSection.ScheduleProfile
                ? ContainerSection.ScheduleProfile
                : ContainerSection.Profile;

            await UpdateAvailabilityPreviewAsync();
            await SwitchToScheduleEditAsync();
        }

        internal async Task MultiOpenSchedulesAsync(IReadOnlyList<ScheduleModel> schedules, CancellationToken ct = default)
        {
            if (schedules.Count == 0)
                return;

            await LoadLookupsAsync(ct);
            ResetScheduleFilters();

            ScheduleEditVm.ClearValidationErrors();

            var keepExisting = Mode == ContainerSection.ScheduleEdit && ScheduleEditVm.IsEdit;
            if (!keepExisting)
                ScheduleEditVm.ResetForNew();

            ScheduleEditVm.IsEdit = true;

            var openedBlocks = new List<ScheduleBlockViewModel>();
            var invalidSchedules = new List<string>();
            var limitSkipped = new List<string>();

            foreach (var schedule in schedules)
            {
                // якщо вже відкритий — просто активуємо
                var existing = ScheduleEditVm.Blocks.FirstOrDefault(b => b.Model.Id == schedule.Id);
                if (existing != null)
                {
                    openedBlocks.Add(existing);
                    continue;
                }

                // ✅ ЛІМІТ 20
                if (ScheduleEditVm.Blocks.Count >= MaxOpenedSchedules)
                {
                    limitSkipped.Add(string.IsNullOrWhiteSpace(schedule.Name) ? $"Schedule {schedule.Id}" : schedule.Name);
                    continue;
                }

                var detailed = await _scheduleService.GetDetailedAsync(schedule.Id, ct);
                if (detailed is null)
                    continue;

                if (!HasGeneratedContent(detailed))
                {
                    invalidSchedules.Add(string.IsNullOrWhiteSpace(detailed.Name) ? $"Schedule {detailed.Id}" : detailed.Name);
                    continue;
                }

                var block = CreateBlockFromSchedule(detailed,
                    detailed.Employees?.ToList() ?? new List<ScheduleEmployeeModel>(),
                    detailed.Slots?.ToList() ?? new List<ScheduleSlotModel>(),
                    detailed.CellStyles?.ToList());

                block.SelectedAvailabilityGroupId = detailed.AvailabilityGroupId!.Value;

                ScheduleEditVm.Blocks.Add(block);
                openedBlocks.Add(block);
            }

            if (openedBlocks.Count == 0)
            {
                // якщо ми нічого не відкрили, але ліміт спрацював — покажемо норм повідомлення
                if (limitSkipped.Count > 0)
                {
                    ShowError($"Max open schedules limit is {MaxOpenedSchedules}. Close some tabs first.");
                    return;
                }

                ShowError("Selected schedules could not be loaded.");
                return;
            }

            if (invalidSchedules.Count > 0)
                ShowError($"Skipped schedules without generated data:{Environment.NewLine}{string.Join(Environment.NewLine, invalidSchedules)}");

            if (limitSkipped.Count > 0)
                ShowInfo($"Opened only first {MaxOpenedSchedules}. Skipped due to limit:{Environment.NewLine}{string.Join(Environment.NewLine, limitSkipped)}");

            ScheduleEditVm.SelectedBlock = openedBlocks.First();
            ScheduleEditVm.RefreshScheduleMatrix();

            ScheduleCancelTarget = Mode == ContainerSection.ScheduleProfile
                ? ContainerSection.ScheduleProfile
                : ContainerSection.Profile;

            await UpdateAvailabilityPreviewAsync(ct);
            await SwitchToScheduleEditAsync();
        }
        internal async Task SaveScheduleAsync(CancellationToken ct = default)
        {
            ScheduleEditVm.ClearValidationErrors();

            if (ScheduleEditVm.Blocks.Count == 0)
            {
                ShowError("Add at least one schedule first.");
                return;
            }

            var invalidBlocks = new List<ScheduleBlockViewModel>();

            foreach (var block in ScheduleEditVm.Blocks)
            {
                block.ValidationErrors.Clear();

                var errors = ValidateAndNormalizeSchedule(block.Model, out var normalizedShift1, out var normalizedShift2);

                foreach (var kv in errors)
                    block.ValidationErrors[kv.Key] = kv.Value;

                if (errors.Count > 0)
                {
                    invalidBlocks.Add(block);
                    continue;
                }

                block.Model.Shift1Time = normalizedShift1!;
                block.Model.Shift2Time = normalizedShift2!;
            }

            if (invalidBlocks.Count > 0)
            {
                var first = invalidBlocks.First();
                ScheduleEditVm.SelectedBlock = first;
                ScheduleEditVm.SetValidationErrors(first.ValidationErrors);
                ShowError(BuildScheduleValidationSummary(invalidBlocks));
                return;
            }

            var missingGenerated = ScheduleEditVm.Blocks.Where(block => !HasGeneratedContent(block)).ToList();
            if (missingGenerated.Count > 0)
            {
                var first = missingGenerated.First();
                ScheduleEditVm.SelectedBlock = first;
                ShowError("You can’t save a schedule until something has been generated. Please run generation first.");
                return;
            }

            var names = ScheduleEditVm.Blocks
                .Select((block, index) =>
                {
                    var name = string.IsNullOrWhiteSpace(block.Model.Name)
                        ? $"Schedule {index + 1}"
                        : block.Model.Name;
                    return $"- {name}";
                })
                .ToList();

            var confirmMessage = $"Do you want to save these schedules?{Environment.NewLine}{string.Join(Environment.NewLine, names)}";
            if (!Confirm(confirmMessage))
                return;

            foreach (var block in ScheduleEditVm.Blocks)
            {
                var employees = block.Employees
                    .GroupBy(e => e.EmployeeId)
                    .Select(g => new ScheduleEmployeeModel
                    {
                        EmployeeId = g.Key,
                        MinHoursMonth = g.First().MinHoursMonth
                    })
                    .ToList();

                var slots = block.Slots.ToList();
                var cellStyles = block.CellStyles
                    .Where(cs => cs.BackgroundColorArgb.HasValue || cs.TextColorArgb.HasValue)
                    .ToList();

                block.Model.AvailabilityGroupId = block.SelectedAvailabilityGroupId > 0
                    ? block.SelectedAvailabilityGroupId
                    : null;

                try
                {
                    await _scheduleService.SaveWithDetailsAsync(block.Model, employees, slots, cellStyles, ct);
                }
                catch (Exception ex)
                {
                    ShowError(ex);
                    return;
                }
            }


            var containerId = ScheduleEditVm.Blocks.FirstOrDefault()?.Model.ContainerId ?? GetCurrentContainerId();
            if (containerId > 0)
                await LoadSchedulesAsync(containerId, search: null, ct);

            ShowInfo("Schedules saved successfully.");

            if (ScheduleEditVm.IsEdit)
            {
                var savedBlock = ScheduleEditVm.Blocks.FirstOrDefault();
                if (savedBlock != null)
                {
                    var detailed = await _scheduleService.GetDetailedAsync(savedBlock.Model.Id, ct);
                    if (detailed != null)
                    {
                        var employees = detailed.Employees?.ToList() ?? new List<ScheduleEmployeeModel>();
                        var slots = detailed.Slots?.ToList() ?? new List<ScheduleSlotModel>();
                        var cellStyles = detailed.CellStyles?.ToList() ?? new List<ScheduleCellStyleModel>();
                        ScheduleProfileVm.SetProfile(detailed, employees, slots, cellStyles);
                        ProfileVm.ScheduleListVm.SelectedItem =
                            ProfileVm.ScheduleListVm.Items.FirstOrDefault(x => x.Model.Id == detailed.Id);

                    }
                }

                ScheduleCancelTarget = ContainerSection.Profile;
                await SwitchToScheduleProfileAsync();
                return;
            }

            await SwitchToProfileAsync();
        }

        internal async Task DeleteSelectedScheduleAsync(CancellationToken ct = default)
        {
            var schedule = ProfileVm.ScheduleListVm.SelectedItem?.Model;
            if (schedule is null) return;

            if (!Confirm($"Delete schedule {schedule.Name}?"))
                return;

            await _scheduleService.DeleteAsync(schedule.Id, ct);

            await LoadSchedulesAsync(schedule.ContainerId, search: null, ct);

            ShowInfo("Schedule deleted successfully.");
            await SwitchToProfileAsync();
        }

        internal async Task OpenScheduleProfileAsync(CancellationToken ct = default)
        {
            var schedule = ProfileVm.ScheduleListVm.SelectedItem?.Model;
            if (schedule is null) return;

            var detailed = await _scheduleService.GetDetailedAsync(schedule.Id, ct);
            if (detailed is null) return;

            var employees = detailed.Employees?.ToList() ?? new List<ScheduleEmployeeModel>();
            var slots = detailed.Slots?.ToList() ?? new List<ScheduleSlotModel>();
            var cellStyles = detailed.CellStyles?.ToList() ?? new List<ScheduleCellStyleModel>();

            ScheduleProfileVm.SetProfile(detailed, employees, slots, cellStyles);
            ProfileVm.ScheduleListVm.SelectedItem =
                ProfileVm.ScheduleListVm.Items.FirstOrDefault(x => x.Model.Id == detailed.Id);


            ScheduleCancelTarget = ContainerSection.Profile;
            await SwitchToScheduleProfileAsync();
        }

        internal Task CancelScheduleAsync()
        {
            ScheduleEditVm.ClearValidationErrors();

            return Mode switch
            {
                ContainerSection.ScheduleEdit => ScheduleCancelTarget == ContainerSection.ScheduleProfile
                    ? SwitchToScheduleProfileAsync()
                    : SwitchToProfileAsync(),
                ContainerSection.ScheduleProfile => SwitchToProfileAsync(),
                _ => SwitchToProfileAsync()
            };
        }

        internal async Task GenerateScheduleAsync(CancellationToken ct = default)
        {
            await RunOnUiThreadAsync(() => Keyboard.ClearFocus());

            if (ScheduleEditVm.SelectedBlock is null)
                return;

            var block = ScheduleEditVm.SelectedBlock;
            var model = block.Model;

            var errors = ValidateAndNormalizeSchedule(model, out var normalizedShift1, out var normalizedShift2);
            if (errors.Count > 0)
            {
                ScheduleEditVm.SetValidationErrors(errors);
                ShowError("Please fix the highlighted fields.");
                return;
            }

            model.Shift1Time = normalizedShift1!;
            model.Shift2Time = normalizedShift2!;

            var selectedGroupId = block.SelectedAvailabilityGroupId;
            if (selectedGroupId <= 0)
            {
                ShowError("Select an availability group.");
                return;
            }

            // ВАЖЛИВО: зберігаємо групу в моделі (потрібно для Save / Edit)
            model.AvailabilityGroupId = selectedGroupId;

            // ✅ 1) Синхронізуємо працівників у блоці з вибраною групою
            //    (додає/прибирає працівників і НЕ затирає MinHoursMonth)
            await SyncEmployeesFromAvailabilityGroupAsync(selectedGroupId, ct);

            // ✅ 2) Витягуємо повні дані групи (members + days)
            var (group, members, days) = await _availabilityGroupService.LoadFullAsync(selectedGroupId, ct);

            if (group.Year != model.Year || group.Month != model.Month)
            {
                ShowError("Selected availability group is for a different month/year.");
                return;
            }

            // прив'язуємо days до members
            var daysByMember = days
                .GroupBy(d => d.AvailabilityGroupMemberId)
                .ToDictionary(g => g.Key, g => (ICollection<AvailabilityGroupDayModel>)g.ToList());

            foreach (var m in members)
            {
                m.Days = daysByMember.TryGetValue(m.Id, out var list)
                    ? list
                    : new List<AvailabilityGroupDayModel>();
            }

            group.Members = members;

            // ✅ 3) Формуємо employees ДЛЯ генератора з block.Employees (там вже введені MinHoursMonth)
            //    На всяк випадок фільтруємо по членству в групі.
            var memberEmpIds = members.Select(m => m.EmployeeId).Distinct().ToHashSet();

            var employees = block.Employees
                .Where(e => memberEmpIds.Contains(e.EmployeeId))
                .GroupBy(e => e.EmployeeId)
                .Select(g =>
                {
                    var first = g.First();
                    return new ScheduleEmployeeModel
                    {
                        EmployeeId = first.EmployeeId,
                        Employee = first.Employee,          // має бути підтягутий в SyncEmployeesFromAvailabilityGroupAsync
                        MinHoursMonth = first.MinHoursMonth // ✅ головне: не губимо
                    };
                })
                .ToList();

            if (employees.Count == 0)
            {
                ShowError("No employees found for selected availability group.");
                return;
            }

            var fullGroups = new List<AvailabilityGroupModel>(capacity: 1) { group };

            // ✅ 4) Генерація
            var slots = await _generator.GenerateAsync(model, fullGroups, employees, ct);

            // ✅ 5) Оновлюємо слоти. Employees НЕ чіпаємо, щоб не стерти MinHoursMonth в UI
            block.Slots.Clear();
            foreach (var slot in slots)
                block.Slots.Add(slot);

            ScheduleEditVm.RefreshScheduleMatrix();
            ShowInfo("Slots generated. Review before saving.");
        }

        internal Task<(AvailabilityGroupModel group, List<AvailabilityGroupMemberModel> members, List<AvailabilityGroupDayModel> days)>
    AvailabilityGroupService_LoadFullAsync(int groupId, CancellationToken ct)
        {
            return _availabilityGroupService.LoadFullAsync(groupId, ct);
        }

        internal Task RunOnUiThreadAsync(Action action)
        {
            if (Application.Current?.Dispatcher is null)
            {
                action();
                return Task.CompletedTask;
            }

            if (Application.Current.Dispatcher.CheckAccess())
            {
                action();
                return Task.CompletedTask;
            }

            return Application.Current.Dispatcher.InvokeAsync(action).Task;
        }


        internal Task SearchScheduleShopsAsync(CancellationToken ct = default)
        {
            ApplyShopFilter(ScheduleEditVm.ShopSearchText);
            return Task.CompletedTask;
        }

        internal Task SearchScheduleAvailabilityAsync(CancellationToken ct = default)
        {
            ApplyAvailabilityFilter(ScheduleEditVm.AvailabilitySearchText);
            return Task.CompletedTask;
        }

        internal Task SearchScheduleEmployeesAsync(CancellationToken ct = default)
        {
            ApplyEmployeeFilter(ScheduleEditVm.EmployeeSearchText);
            return Task.CompletedTask;
        }

        internal Task AddScheduleEmployeeAsync(CancellationToken ct = default)
        {
            if (ScheduleEditVm.SelectedBlock is null)
                return Task.CompletedTask;

            var employee = ScheduleEditVm.SelectedEmployee;
            if (employee is null)
            {
                ShowError("Select employee first.");
                return Task.CompletedTask;
            }

            if (ScheduleEditVm.SelectedBlock.Employees.Any(e => e.EmployeeId == employee.Id))
            {
                ShowInfo("This employee is already added.");
                return Task.CompletedTask;
            }

            ScheduleEditVm.SelectedBlock.Employees.Add(new ScheduleEmployeeModel
            {
                EmployeeId = employee.Id,
                Employee = employee
            });

            ScheduleEditVm.RefreshScheduleMatrix();
            return UpdateAvailabilityPreviewAsync(ct);
        }

        internal Task RemoveScheduleEmployeeAsync(CancellationToken ct = default)
        {
            if (ScheduleEditVm.SelectedBlock is null)
                return Task.CompletedTask;

            var selected = ScheduleEditVm.SelectedScheduleEmployee;
            if (selected is null)
            {
                ShowError("Select employee first.");
                return Task.CompletedTask;
            }

            var toRemove = ScheduleEditVm.SelectedBlock.Employees.FirstOrDefault(e => e.EmployeeId == selected.EmployeeId);
            if (toRemove is null)
            {
                ShowInfo("This employee is not in the group.");
                return Task.CompletedTask;
            }

            ScheduleEditVm.SelectedBlock.Employees.Remove(toRemove);

            var slotsToRemove = ScheduleEditVm.SelectedBlock.Slots
                .Where(s => s.EmployeeId == selected.EmployeeId)
                .ToList();
            foreach (var slot in slotsToRemove)
                ScheduleEditVm.SelectedBlock.Slots.Remove(slot);

            ScheduleEditVm.RemoveCellStylesForEmployee(selected.EmployeeId);
            ScheduleEditVm.RefreshScheduleMatrix();
            return UpdateAvailabilityPreviewAsync(ct);
        }

        // ContainerViewModel.cs
        internal Task AddScheduleBlockAsync(CancellationToken ct = default)
        {
            if (ScheduleEditVm.IsEdit)
                return Task.CompletedTask;

            if (ScheduleEditVm.SelectedBlock is null)
                return Task.CompletedTask;

            if (ScheduleEditVm.Blocks.Count >= MaxOpenedSchedules)
            {
                ShowInfo($"You can open max {MaxOpenedSchedules} schedules.");
                return Task.CompletedTask;
            }

            var block = CreateDefaultBlock(ScheduleEditVm.SelectedBlock.Model.ContainerId);
            ScheduleEditVm.Blocks.Add(block);
            ScheduleEditVm.SelectedBlock = block;
            ScheduleEditVm.RefreshScheduleMatrix();

            return UpdateAvailabilityPreviewAsync(ct);
        }

        internal Task SelectScheduleBlockAsync(ScheduleBlockViewModel block, CancellationToken ct = default)
        {
            if (!ScheduleEditVm.Blocks.Contains(block))
                return Task.CompletedTask;

            ScheduleEditVm.SelectedBlock = block;
            ScheduleEditVm.ClearValidationErrors();

            if (block.ValidationErrors.Count > 0)
                ScheduleEditVm.SetValidationErrors(block.ValidationErrors);

            return UpdateAvailabilityPreviewAsync(ct);
        }

        internal async Task CloseScheduleBlockAsync(ScheduleBlockViewModel block, CancellationToken ct = default)
        {
            if (!ScheduleEditVm.Blocks.Contains(block))
                return;

            if (!Confirm("Are you sure you want to close this schedule?"))
                return;

            var index = ScheduleEditVm.Blocks.IndexOf(block);
            ScheduleEditVm.Blocks.Remove(block);

            if (ScheduleEditVm.Blocks.Count == 0)
            {
                ScheduleEditVm.SelectedBlock = null;
                ScheduleEditVm.RefreshScheduleMatrix();
                ScheduleEditVm.RefreshAvailabilityPreviewMatrix(1, 1, new List<ScheduleSlotModel>(), new List<ScheduleEmployeeModel>());
                return;
            }

            var nextIndex = Math.Max(0, Math.Min(index, ScheduleEditVm.Blocks.Count - 1));
            var next = ScheduleEditVm.Blocks[nextIndex];
            ScheduleEditVm.SelectedBlock = next;

            if (next.ValidationErrors.Count > 0)
                ScheduleEditVm.SetValidationErrors(next.ValidationErrors);
            else
                ScheduleEditVm.ClearValidationErrors();

            await UpdateAvailabilityPreviewAsync(ct);
        }

        internal async Task UpdateAvailabilityPreviewAsync(CancellationToken ct = default)
        {
            if (ScheduleEditVm.SelectedBlock is null)
                return;

            var year = ScheduleEditVm.ScheduleYear;
            var month = ScheduleEditVm.ScheduleMonth;

            if (year < 1 || month < 1 || month > 12)
            {
                ScheduleEditVm.RefreshAvailabilityPreviewMatrix(year, month, new List<ScheduleSlotModel>(), new List<ScheduleEmployeeModel>());
                return;
            }

            var selectedGroupId = ScheduleEditVm.SelectedBlock.SelectedAvailabilityGroupId;
            if (selectedGroupId <= 0)
            {
                ScheduleEditVm.RefreshAvailabilityPreviewMatrix(year, month, new List<ScheduleSlotModel>(), new List<ScheduleEmployeeModel>());
                return;
            }

            (string from, string to)? shift1 = TrySplitShift(ScheduleEditVm.ScheduleShift1);
            (string from, string to)? shift2 = TrySplitShift(ScheduleEditVm.ScheduleShift2);

            var employees = new List<ScheduleEmployeeModel>();
            var slots = new List<ScheduleSlotModel>();
            var seenEmp = new HashSet<int>();
            var seenSlot = new HashSet<string>();

            var (group, members, days) = await _availabilityGroupService.LoadFullAsync(selectedGroupId, ct);

            if (group.Year != year || group.Month != month)
            {
                ScheduleEditVm.RefreshAvailabilityPreviewMatrix(year, month, new List<ScheduleSlotModel>(), new List<ScheduleEmployeeModel>());
                return;
            }

            var daysByMember = days
                .GroupBy(d => d.AvailabilityGroupMemberId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var m in members)
            {
                if (seenEmp.Add(m.EmployeeId))
                {
                    employees.Add(new ScheduleEmployeeModel
                    {
                        EmployeeId = m.EmployeeId,
                        Employee = m.Employee
                    });
                }

                if (!daysByMember.TryGetValue(m.Id, out var mdays) || mdays.Count == 0)
                    continue;

                foreach (var d in mdays)
                {
                    if (d.Kind == AvailabilityKind.NONE)
                        continue;

                    if (d.Kind == AvailabilityKind.INT)
                    {
                        if (string.IsNullOrWhiteSpace(d.IntervalStr))
                            continue;

                        if (!AvailabilityCodeParser.TryNormalizeInterval(d.IntervalStr, out var normalized))
                            continue;

                        if (TrySplitInterval(normalized, out var from, out var to))
                            AddSlotUnique(m.EmployeeId, d.DayOfMonth, from, to);

                        continue;
                    }

                    if (d.Kind == AvailabilityKind.ANY)
                    {
                        if (shift1 != null) AddSlotUnique(m.EmployeeId, d.DayOfMonth, shift1.Value.from, shift1.Value.to);
                        if (shift2 != null) AddSlotUnique(m.EmployeeId, d.DayOfMonth, shift2.Value.from, shift2.Value.to);
                    }
                }
            }

            ScheduleEditVm.RefreshAvailabilityPreviewMatrix(year, month, slots, employees);

            void AddSlotUnique(int empId, int day, string from, string to)
            {
                var key = $"{empId}|{day}|{from}|{to}";
                if (!seenSlot.Add(key)) return;

                slots.Add(new ScheduleSlotModel
                {
                    EmployeeId = empId,
                    DayOfMonth = day,
                    FromTime = from,
                    ToTime = to,
                    SlotNo = 1,
                    Status = SlotStatus.UNFURNISHED
                });
            }

            static bool TrySplitInterval(string normalized, out string from, out string to)
            {
                from = to = string.Empty;
                var parts = normalized.Split('-', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length != 2) return false;
                from = parts[0];
                to = parts[1];
                return true;
            }

            (string from, string to)? TrySplitShift(string rawShift)
            {
                if (!TryNormalizeShiftRange(rawShift, out var normalized, out _))
                    return null;

                return TrySplitInterval(normalized, out var f, out var t) ? (f, t) : null;
            }
        }

        private async Task LoadContainersAsync(CancellationToken ct, int? selectId = null)
        {
            var list = await _containerService.GetAllAsync(ct);
            ListVm.SetItems(list);

            if (selectId.HasValue)
                ListVm.SelectedItem = list.FirstOrDefault(c => c.Id == selectId.Value);
        }

        private async Task LoadSchedulesAsync(int containerId, string? search, CancellationToken ct)
        {
            var schedules = await _scheduleService.GetByContainerAsync(containerId, search, ct);
            ProfileVm.ScheduleListVm.SetItems(schedules);
        }

        private async Task LoadLookupsAsync(CancellationToken ct)
        {
            var groups = (await _availabilityGroupService.GetAllAsync(ct)).ToList();
            var shops = await _shopService.GetAllAsync(ct);
            var employees = await _employeeService.GetAllAsync(ct);

            _allAvailabilityGroups.Clear();
            _allAvailabilityGroups.AddRange(groups);

            _allShops.Clear();
            _allShops.AddRange(shops);

            _allEmployees.Clear();
            _allEmployees.AddRange(employees);

            ScheduleEditVm.SetLookups(shops, groups, employees);
        }

        private int GetCurrentContainerId()
        {
            if (Mode == ContainerSection.Profile || Mode == ContainerSection.ScheduleEdit || Mode == ContainerSection.ScheduleProfile)
                return ProfileVm.ContainerId;

            return ListVm.SelectedItem?.Id ?? 0;
        }

        private Task SwitchToListAsync()
        {
            CurrentSection = ListVm;
            Mode = ContainerSection.List;
            return Task.CompletedTask;
        }

        private Task SwitchToEditAsync()
        {
            CurrentSection = EditVm;
            Mode = ContainerSection.Edit;
            return Task.CompletedTask;
        }

        private Task SwitchToProfileAsync()
        {
            CurrentSection = ProfileVm;
            Mode = ContainerSection.Profile;
            return Task.CompletedTask;
        }

        private Task SwitchToScheduleEditAsync()
        {
            CurrentSection = ScheduleEditVm;
            Mode = ContainerSection.ScheduleEdit;
            return Task.CompletedTask;
        }

        private Task SwitchToScheduleProfileAsync()
        {
            CurrentSection = ScheduleProfileVm;
            Mode = ContainerSection.ScheduleProfile;
            return Task.CompletedTask;
        }

        private ScheduleBlockViewModel CreateDefaultBlock(int containerId)
        {
            var model = new ScheduleModel
            {
                ContainerId = containerId,
                Year = DateTime.Today.Year,
                Month = DateTime.Today.Month,
                PeoplePerShift = 1,
                MaxHoursPerEmpMonth = 1,
                MaxConsecutiveDays = 1,
                MaxConsecutiveFull = 1,
                MaxFullPerMonth = 1,
                Shift1Time = string.Empty,
                Shift2Time = string.Empty,
                Note = string.Empty
            };

            var block = new ScheduleBlockViewModel
            {
                Model = model,
                SelectedAvailabilityGroupId = GetDefaultAvailabilityGroupId(model.Year, model.Month)
            };

            return block;
        }

        private ScheduleBlockViewModel CreateBlockFromSchedule(
            ScheduleModel model,
            IList<ScheduleEmployeeModel> employees,
            IList<ScheduleSlotModel> slots,
            IList<ScheduleCellStyleModel>? cellStyles = null)
        {
            var copy = new ScheduleModel
            {
                Id = model.Id,
                ContainerId = model.ContainerId,
                ShopId = model.ShopId,
                Name = model.Name,
                Year = model.Year,
                Month = model.Month,
                PeoplePerShift = model.PeoplePerShift,
                Shift1Time = model.Shift1Time,
                Shift2Time = model.Shift2Time,
                MaxHoursPerEmpMonth = model.MaxHoursPerEmpMonth,
                MaxConsecutiveDays = model.MaxConsecutiveDays,
                MaxConsecutiveFull = model.MaxConsecutiveFull,
                MaxFullPerMonth = model.MaxFullPerMonth,
                Note = model.Note,
                Shop = model.Shop,

                AvailabilityGroupId = model.AvailabilityGroupId // <-- ДОДАТИ
            };

            var block = new ScheduleBlockViewModel
            {
                Model = copy,
                SelectedAvailabilityGroupId =
                    copy.AvailabilityGroupId
                    ?? GetDefaultAvailabilityGroupId(copy.Year, copy.Month)
            };

            foreach (var emp in employees)
                block.Employees.Add(emp);

            foreach (var slot in slots)
                block.Slots.Add(slot);

            if (cellStyles != null)
            {
                foreach (var style in cellStyles)
                    block.CellStyles.Add(style);
            }

            return block;
        }

        private static bool HasGeneratedContent(ScheduleModel schedule)
        {
            return schedule.AvailabilityGroupId is not null
                && schedule.AvailabilityGroupId > 0
                && schedule.Slots != null
                && schedule.Slots.Count > 0;
        }

        private static bool HasGeneratedContent(ScheduleBlockViewModel block)
        {
            return block.SelectedAvailabilityGroupId > 0
                && block.Slots.Count > 0;
        }

        private int GetDefaultAvailabilityGroupId(int year, int month)
        {
            return _allAvailabilityGroups
                .Where(g => g.Year == year && g.Month == month)
                .Select(g => g.Id)
                .FirstOrDefault();
        }

        private void ResetScheduleFilters()
        {
            ScheduleEditVm.ShopSearchText = string.Empty;
            ScheduleEditVm.AvailabilitySearchText = string.Empty;
            ScheduleEditVm.EmployeeSearchText = string.Empty;

            ScheduleEditVm.SetLookups(_allShops, _allAvailabilityGroups, _allEmployees);
        }

        private void ApplyShopFilter(string? raw)
        {
            var term = raw?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(term))
            {
                ScheduleEditVm.SetShops(_allShops);
                return;
            }

            var filtered = _allShops
                .Where(s => ContainsIgnoreCase(s.Name, term))
                .ToList();

            ScheduleEditVm.SetShops(filtered);
        }

        private void ApplyAvailabilityFilter(string? raw)
        {
            var term = raw?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(term))
            {
                ScheduleEditVm.SetAvailabilityGroups(_allAvailabilityGroups);
                return;
            }

            var filtered = _allAvailabilityGroups
                .Where(g => ContainsIgnoreCase(g.Name, term))
                .ToList();

            ScheduleEditVm.SetAvailabilityGroups(filtered);
        }

        private void ApplyEmployeeFilter(string? raw)
        {
            var term = raw?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(term))
            {
                ScheduleEditVm.SetEmployees(_allEmployees);
                return;
            }

            var filtered = _allEmployees
                .Where(e => ContainsIgnoreCase(e.FirstName, term) || ContainsIgnoreCase(e.LastName, term))
                .ToList();

            ScheduleEditVm.SetEmployees(filtered);
        }

        private static bool ContainsIgnoreCase(string? source, string value)
            => (source ?? string.Empty).Contains(value, StringComparison.OrdinalIgnoreCase);

        private static Dictionary<string, string> ValidateContainer(ContainerModel model)
        {
            var errors = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(model.Name))
                errors[nameof(ContainerEditViewModel.Name)] = "Name is required.";

            return errors;
        }

        private static Dictionary<string, string> ValidateAndNormalizeSchedule(
            ScheduleModel model,
            out string? normalizedShift1,
            out string? normalizedShift2)
        {
            var errors = new Dictionary<string, string>();
            normalizedShift1 = null;
            normalizedShift2 = null;

            if (model.ContainerId <= 0)
                errors[nameof(ContainerScheduleEditViewModel.ScheduleContainerId)] = "Select a container.";
            if (model.ShopId <= 0)
                errors[nameof(ContainerScheduleEditViewModel.ScheduleShopId)] = "Select a shop.";
            if (string.IsNullOrWhiteSpace(model.Name))
                errors[nameof(ContainerScheduleEditViewModel.ScheduleName)] = "Name is required.";
            if (model.Year < 1900)
                errors[nameof(ContainerScheduleEditViewModel.ScheduleYear)] = "Year is invalid.";
            if (model.Month < 1 || model.Month > 12)
                errors[nameof(ContainerScheduleEditViewModel.ScheduleMonth)] = "Month must be 1-12.";
            if (model.PeoplePerShift <= 0)
                errors[nameof(ContainerScheduleEditViewModel.SchedulePeoplePerShift)] = "People per shift must be greater than zero.";
            if (model.MaxHoursPerEmpMonth <= 0)
                errors[nameof(ContainerScheduleEditViewModel.ScheduleMaxHoursPerEmp)] = "Max hours per employee must be greater than zero.";

            if (string.IsNullOrWhiteSpace(model.Shift1Time))
            {
                errors[nameof(ContainerScheduleEditViewModel.ScheduleShift1)] = "Shift1 is required.";
            }
            else if (!TryNormalizeShiftRange(model.Shift1Time, out normalizedShift1, out var err1))
            {
                errors[nameof(ContainerScheduleEditViewModel.ScheduleShift1)] = err1 ?? "Invalid shift1 format.";
            }

            if (string.IsNullOrWhiteSpace(model.Shift2Time))
            {
                errors[nameof(ContainerScheduleEditViewModel.ScheduleShift2)] = "Shift2 is required.";
            }
            else if (!TryNormalizeShiftRange(model.Shift2Time, out normalizedShift2, out var err2))
            {
                errors[nameof(ContainerScheduleEditViewModel.ScheduleShift2)] = err2 ?? "Invalid shift2 format.";
            }

            model.Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim();

            return errors;
        }

        private string BuildScheduleValidationSummary(IReadOnlyCollection<ScheduleBlockViewModel> invalidBlocks)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Fix the following schedule errors before saving:");

            foreach (var block in invalidBlocks)
            {
                var index = ScheduleEditVm.Blocks.IndexOf(block);
                var displayIndex = index >= 0 ? index + 1 : 0;
                var header = displayIndex > 0 ? $"Schedule #{displayIndex}" : "Schedule";
                var name = string.IsNullOrWhiteSpace(block.Model.Name) ? null : block.Model.Name.Trim();
                if (!string.IsNullOrWhiteSpace(name))
                    header = $"{header} \"{name}\"";

                foreach (var (field, message) in block.ValidationErrors)
                {
                    var label = GetScheduleFieldLabel(field);
                    sb.AppendLine($"- {header}: {label} — {message}");
                }
            }

            return sb.ToString().TrimEnd();
        }

        private static string GetScheduleFieldLabel(string propertyName)
        {
            return propertyName switch
            {
                nameof(ContainerScheduleEditViewModel.ScheduleContainerId) => "Container",
                nameof(ContainerScheduleEditViewModel.ScheduleShopId) => "Shop",
                nameof(ContainerScheduleEditViewModel.ScheduleName) => "Name",
                nameof(ContainerScheduleEditViewModel.ScheduleYear) => "Year",
                nameof(ContainerScheduleEditViewModel.ScheduleMonth) => "Month",
                nameof(ContainerScheduleEditViewModel.SchedulePeoplePerShift) => "People per shift",
                nameof(ContainerScheduleEditViewModel.ScheduleMaxHoursPerEmp) => "Max hours per employee",
                nameof(ContainerScheduleEditViewModel.ScheduleShift1) => "Shift 1",
                nameof(ContainerScheduleEditViewModel.ScheduleShift2) => "Shift 2",
                _ => propertyName
            };
        }

        private static bool TryParseTime(string s, out TimeSpan t)
        {
            return TimeSpan.TryParseExact(
                (s ?? string.Empty).Trim(),
                new[] { @"h\:mm", @"hh\:mm" },
                CultureInfo.InvariantCulture,
                out t);
        }

        private static bool TryNormalizeShiftRange(string? input, out string normalized, out string? error)
        {
            normalized = string.Empty;
            error = null;

            input = (input ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(input))
            {
                error = "Shift is required.";
                return false;
            }

            var parts = input.Split('-', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                error = "Format: HH:mm-HH:mm (spaces don't matter)";
                return false;
            }

            if (!TryParseTime(parts[0], out var from) || !TryParseTime(parts[1], out var to))
            {
                error = "Time must be H:mm or HH:mm (e.g. 9:00-15:00)";
                return false;
            }

            if (from >= to)
            {
                error = "From must be earlier than To";
                return false;
            }

            normalized = $"{from:hh\\:mm} - {to:hh\\:mm}";
            return true;
        }

        internal void ShowInfo(string text)
            => CustomMessageBox.Show("Info", text, CustomMessageBoxIcon.Info, okText: "OK");

        internal void ShowError(string text)
            => CustomMessageBox.Show("Error", text, CustomMessageBoxIcon.Error, okText: "OK");

        internal void ShowError(Exception ex)
        {
            var (summary, details) = ExceptionMessageBuilder.Build(ex);
            CustomMessageBox.Show("Error", summary, CustomMessageBoxIcon.Error, okText: "OK", details: details);
        }

        internal bool Confirm(string text, string? caption = null)
            => CustomMessageBox.Show(
                caption ?? "Confirm",
                text,
                CustomMessageBoxIcon.Warning,
                okText: "Yes",
                cancelText: "No");

        // ContainerViewModel.cs

        internal async Task SyncEmployeesFromAvailabilityGroupAsync(int groupId, CancellationToken ct = default)
        {
            if (ScheduleEditVm.SelectedBlock is null)
                return;

            var block = ScheduleEditVm.SelectedBlock;

            // витягуємо members, щоб знати список EmployeeId + мати Employee об'єкти
            var (_, members, _) = await _availabilityGroupService.LoadFullAsync(groupId, ct).ConfigureAwait(false);

            var groupEmpIds = members
                .Select(m => m.EmployeeId)
                .Distinct()
                .ToHashSet();

            if (!ScheduleEditVm.Blocks.Contains(block))
                return;

            await RunOnUiThreadAsync(() =>
            {
                // зберегти вже введені MinHoursMonth
                var oldMin = block.Employees
                    .GroupBy(e => e.EmployeeId)
                    .ToDictionary(g => g.Key, g => g.First().MinHoursMonth);

                // 1) прибрати тих, кого нема в групі
                for (int i = block.Employees.Count - 1; i >= 0; i--)
                {
                    if (!groupEmpIds.Contains(block.Employees[i].EmployeeId))
                        block.Employees.RemoveAt(i);
                }

                // 2) додати відсутніх з групи (і підтягнути Employee)
                foreach (var m in members)
                {
                    var existing = block.Employees.FirstOrDefault(e => e.EmployeeId == m.EmployeeId);
                    if (existing != null)
                    {
                        // оновимо Employee reference, якщо раптом null
                        existing.Employee ??= m.Employee;
                        continue;
                    }

                    block.Employees.Add(new ScheduleEmployeeModel
                    {
                        EmployeeId = m.EmployeeId,
                        Employee = m.Employee,
                        MinHoursMonth = oldMin.TryGetValue(m.EmployeeId, out var min) ? min : 0
                    });
                }

                if (ReferenceEquals(ScheduleEditVm.SelectedBlock, block))
                    ScheduleEditVm.RefreshScheduleMatrix();
            }).ConfigureAwait(false);
        }

    }
}
