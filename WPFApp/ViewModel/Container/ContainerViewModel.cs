using BusinessLogicLayer.Availability;
using BusinessLogicLayer.Generators;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private CancellationTokenSource? _availabilityPreviewCts;
        private int _availabilityPreviewVersion;
        private string? _availabilityPreviewRequestKey;

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

            await ScheduleEditVm.RefreshScheduleMatrixAsync(ct);
            await ScheduleEditVm.RefreshAvailabilityPreviewMatrixAsync(
                block.Model.Year, block.Model.Month,
                new List<ScheduleSlotModel>(),
                new List<ScheduleEmployeeModel>(),
                previewKey: $"CLEAR|{block.Model.Year}|{block.Model.Month}",
                ct);

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
            await ScheduleEditVm.RefreshScheduleMatrixAsync(ct);

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
            await ScheduleEditVm.RefreshScheduleMatrixAsync(ct);

            ScheduleCancelTarget = Mode == ContainerSection.ScheduleProfile
                ? ContainerSection.ScheduleProfile
                : ContainerSection.Profile;

            await UpdateAvailabilityPreviewAsync(ct);
            await SwitchToScheduleEditAsync();
        }
        internal async Task SaveScheduleAsync(CancellationToken ct = default)
        {
            MatrixRefreshDiagnostics.Step("ACTION: SaveScheduleAsync START");
            MatrixRefreshDiagnostics.Snapshot("Before save");

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
                    MatrixRefreshDiagnostics.Step(
                          $"CellStyles total={block.CellStyles.Count}, colored={cellStyles.Count}"
                        );
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

            MatrixRefreshDiagnostics.Step("ACTION: SaveScheduleAsync DONE");
            MatrixRefreshDiagnostics.Snapshot("After save");

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
                        await ScheduleProfileVm.SetProfileAsync(detailed, employees, slots, cellStyles, ct);
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

            await ScheduleProfileVm.SetProfileAsync(detailed, employees, slots, cellStyles, ct);
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
            MatrixRefreshDiagnostics.Step("ACTION: GenerateScheduleAsync START");
            MatrixRefreshDiagnostics.Snapshot("Before generation");

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
            var slots = await _generator.GenerateAsync(model, fullGroups, employees, ct)
                       ?? new List<ScheduleSlotModel>(); // (опціонально) щоб прибрати warning про null

            MatrixRefreshDiagnostics.Step($"ACTION: GenerateScheduleAsync GENERATED slots={slots.Count}");
            MatrixRefreshDiagnostics.Snapshot("After generation");

            // ✅ 5) Оновлюємо слоти
            block.Slots.Clear();
            foreach (var slot in slots)
                block.Slots.Add(slot);

            // ✅ 6) Оновлюємо матрицю
            await ScheduleEditVm.RefreshScheduleMatrixAsync(ct);
            MatrixRefreshDiagnostics.Step("ACTION: GenerateScheduleAsync RefreshScheduleMatrixAsync DONE");

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

        internal async Task AddScheduleEmployeeAsync(CancellationToken ct = default)
        {
            if (ScheduleEditVm.SelectedBlock is null)
                return;

            var employee = ScheduleEditVm.SelectedEmployee;
            if (employee is null)
            {
                ShowError("Select employee first.");
                return;
            }

            if (ScheduleEditVm.SelectedBlock.Employees.Any(e => e.EmployeeId == employee.Id))
            {
                ShowInfo("This employee is already added.");
                return;
            }

            ScheduleEditVm.SelectedBlock.Employees.Add(new ScheduleEmployeeModel
            {
                EmployeeId = employee.Id,
                Employee = employee
            });

            await ScheduleEditVm.RefreshScheduleMatrixAsync(ct);
            await UpdateAvailabilityPreviewAsync(ct);
        }

        internal async Task RemoveScheduleEmployeeAsync(CancellationToken ct = default)
        {
            if (ScheduleEditVm.SelectedBlock is null)
                return;

            var selected = ScheduleEditVm.SelectedScheduleEmployee;
            if (selected is null)
            {
                ShowError("Select employee first.");
                return;
            }

            var toRemove = ScheduleEditVm.SelectedBlock.Employees.FirstOrDefault(e => e.EmployeeId == selected.EmployeeId);
            if (toRemove is null)
            {
                ShowInfo("This employee is not in the group.");
                return;
            }

            ScheduleEditVm.SelectedBlock.Employees.Remove(toRemove);

            var slotsToRemove = ScheduleEditVm.SelectedBlock.Slots
                .Where(s => s.EmployeeId == selected.EmployeeId)
                .ToList();

            foreach (var slot in slotsToRemove)
                ScheduleEditVm.SelectedBlock.Slots.Remove(slot);

            ScheduleEditVm.RemoveCellStylesForEmployee(selected.EmployeeId);

            await ScheduleEditVm.RefreshScheduleMatrixAsync(ct);
            await UpdateAvailabilityPreviewAsync(ct);
        }

        // ContainerViewModel.cs
        internal async Task AddScheduleBlockAsync(CancellationToken ct = default)
        {
            if (ScheduleEditVm.IsEdit)
                return;

            if (ScheduleEditVm.SelectedBlock is null)
                return;

            if (ScheduleEditVm.Blocks.Count >= MaxOpenedSchedules)
            {
                ShowInfo($"You can open max {MaxOpenedSchedules} schedules.");
                return;
            }

            var block = CreateDefaultBlock(ScheduleEditVm.SelectedBlock.Model.ContainerId);
            ScheduleEditVm.Blocks.Add(block);
            ScheduleEditVm.SelectedBlock = block;

            await ScheduleEditVm.RefreshScheduleMatrixAsync(ct);
            await UpdateAvailabilityPreviewAsync(ct);
        }

        internal async Task SelectScheduleBlockAsync(ScheduleBlockViewModel block, CancellationToken ct = default)
        {
            if (!ScheduleEditVm.Blocks.Contains(block))
                return;

            ScheduleEditVm.SelectedBlock = block;
            ScheduleEditVm.ClearValidationErrors();

            if (block.ValidationErrors.Count > 0)
                ScheduleEditVm.SetValidationErrors(block.ValidationErrors);

            await ScheduleEditVm.RefreshScheduleMatrixAsync(ct);
            await UpdateAvailabilityPreviewAsync(ct);
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
                await ScheduleEditVm.RefreshScheduleMatrixAsync(ct);
                await ScheduleEditVm.RefreshAvailabilityPreviewMatrixAsync(
                    block.Model.Year, block.Model.Month,
                    new List<ScheduleSlotModel>(),
                    new List<ScheduleEmployeeModel>(),
                    previewKey: $"CLEAR|{block.Model.Year}|{block.Model.Month}",
                    ct);
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
                CancelAvailabilityPreviewPipeline();
                _availabilityPreviewRequestKey = null;
                await ScheduleEditVm.RefreshAvailabilityPreviewMatrixAsync(
                    year, month,
                    new List<ScheduleSlotModel>(),
                    new List<ScheduleEmployeeModel>(),
                    previewKey: $"CLEAR|{year}|{month}",
                    ct);

                return;
            }

            var selectedGroupId = ScheduleEditVm.SelectedBlock.SelectedAvailabilityGroupId;
            if (selectedGroupId <= 0)
            {
                CancelAvailabilityPreviewPipeline();
                _availabilityPreviewRequestKey = null;
                await ScheduleEditVm.RefreshAvailabilityPreviewMatrixAsync(
                    year, month,
                    new List<ScheduleSlotModel>(),
                    new List<ScheduleEmployeeModel>(),
                    previewKey: $"CLEAR|{year}|{month}",
                    ct);

                return;
            }

            static string CanonShift(string s)
            {
                s = (s ?? "").Trim();
                // прибираємо різницю між "09:00-15:00" і "09:00 - 15:00"
                s = s.Replace(" - ", "-").Replace(" -", "-").Replace("- ", "-");
                return s;
            }

            var previewKey =
                $"{selectedGroupId}|{year}|{month}|{CanonShift(ScheduleEditVm.ScheduleShift1)}|{CanonShift(ScheduleEditVm.ScheduleShift2)}";

            if (ScheduleEditVm.IsAvailabilityPreviewCurrent(previewKey))
            {
                MatrixRefreshDiagnostics.RecordAvailabilityPreviewRequest(previewKey, skipped: true);
                _availabilityPreviewRequestKey = previewKey;
                return;
            }

            if (previewKey == _availabilityPreviewRequestKey && _availabilityPreviewCts != null && !_availabilityPreviewCts.IsCancellationRequested)
            {
                MatrixRefreshDiagnostics.RecordAvailabilityPreviewRequest(previewKey, skipped: true);
                return;
            }

            MatrixRefreshDiagnostics.RecordAvailabilityPreviewRequest(previewKey, skipped: false);
            _availabilityPreviewRequestKey = previewKey;

            CancelAvailabilityPreviewPipeline();
            var localCts = _availabilityPreviewCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var version = ++_availabilityPreviewVersion;

            try
            {
                var (group, members, days) = await _availabilityGroupService.LoadFullAsync(selectedGroupId, localCts.Token);

                if (localCts.IsCancellationRequested || version != _availabilityPreviewVersion)
                    return;

                if (group.Year != year || group.Month != month)
                {
                    await ScheduleEditVm.RefreshAvailabilityPreviewMatrixAsync(
                       year, month,
                       new List<ScheduleSlotModel>(),
                       new List<ScheduleEmployeeModel>(),
                       previewKey: $"CLEAR|{year}|{month}",
                       ct);

                    return;
                }

                var shift1 = TrySplitShift(ScheduleEditVm.ScheduleShift1);
                var shift2 = TrySplitShift(ScheduleEditVm.ScheduleShift2);

                var result = await Task.Run(() =>
                {
                    var employees = new List<ScheduleEmployeeModel>();
                    var slots = new List<ScheduleSlotModel>();
                    var seenEmp = new HashSet<int>();
                    var seenSlot = new HashSet<string>();

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
                                    AddSlotUnique(m.EmployeeId, d.DayOfMonth, from, to, slots, seenSlot);

                                continue;
                            }

                            if (d.Kind == AvailabilityKind.ANY)
                            {
                                if (shift1 != null) AddSlotUnique(m.EmployeeId, d.DayOfMonth, shift1.Value.from, shift1.Value.to, slots, seenSlot);
                                if (shift2 != null) AddSlotUnique(m.EmployeeId, d.DayOfMonth, shift2.Value.from, shift2.Value.to, slots, seenSlot);
                            }
                        }
                    }

                    return (Employees: employees, Slots: slots);
                }, localCts.Token);

                if (localCts.IsCancellationRequested || version != _availabilityPreviewVersion)
                    return;

                // Build matrix off UI thread to keep scrolling smooth.
                await ScheduleEditVm.RefreshAvailabilityPreviewMatrixAsync(
                    year, month,
                    result.Slots,
                    result.Employees,
                    previewKey,
                    localCts.Token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                ShowError(ex);
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

            static void AddSlotUnique(
                int empId,
                int day,
                string from,
                string to,
                List<ScheduleSlotModel> slots,
                HashSet<string> seenSlot)
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

            (string from, string to)? TrySplitShift(string rawShift)
            {
                if (!TryNormalizeShiftRange(rawShift, out var normalized, out _))
                    return null;

                return TrySplitInterval(normalized, out var f, out var t) ? (f, t) : null;
            }
        }

        internal void CancelScheduleEditWork()
        {
            CancelAvailabilityPreviewPipeline();
            _availabilityPreviewVersion++;
            _availabilityPreviewRequestKey = null;
        }

        private void CancelAvailabilityPreviewPipeline()
        {
            _availabilityPreviewCts?.Cancel();
            _availabilityPreviewCts?.Dispose();
            _availabilityPreviewCts = null;
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
            if (Mode == ContainerSection.ScheduleEdit || Mode == ContainerSection.ScheduleProfile)
                CleanupScheduleEdit();

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
            MatrixRefreshDiagnostics.Step("NAV: SwitchToProfile");
            if (Mode == ContainerSection.ScheduleEdit || Mode == ContainerSection.ScheduleProfile)
                CleanupScheduleEdit();

            CurrentSection = ProfileVm;
            Mode = ContainerSection.Profile;
            return Task.CompletedTask;
        }

        private Task SwitchToScheduleEditAsync()
        {
            MatrixRefreshDiagnostics.Step("NAV: SwitchToScheduleEdit");
            CurrentSection = ScheduleEditVm;
            Mode = ContainerSection.ScheduleEdit;
            return Task.CompletedTask;
        }

        private Task SwitchToScheduleProfileAsync()
        {
            MatrixRefreshDiagnostics.Step("NAV: SwitchToScheduleProfile");
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

        private void CleanupScheduleEdit()
        {
            // зупиняє preview pipeline в ContainerViewModel
            CancelScheduleEditWork(); // :contentReference[oaicite:4]{index=4}

            // зупиняє CTS у ScheduleEditVm (матриця/preview)
            ScheduleEditVm.CancelBackgroundWork(); // :contentReference[oaicite:5]{index=5}

            // (опціонально, але рекомендую) звільнити важкі DataView/DataTable і вкладені binding-и
            ScheduleEditVm.ResetForNew(); // :contentReference[oaicite:6]{index=6}
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

            MatrixRefreshDiagnostics.RecordEmployeesSync(
                $"START groupId={groupId} blockId={block.Model.Id} employeesBefore={block.Employees.Count}");

            // ---- HOT SPOT instrumentation: LoadFullAsync ----
            var snapLoad = MatrixRefreshDiagnostics.AllocSnapshot();
            var swLoad = Stopwatch.StartNew();

            var (grp, members, days) =
                await _availabilityGroupService.LoadFullAsync(groupId, ct).ConfigureAwait(false);

            swLoad.Stop();

            // якщо сервіс раптом повертає null усередині tuple (навіть якщо типи non-nullable)
            members ??= new List<AvailabilityGroupMemberModel>();
            days ??= new List<AvailabilityGroupDayModel>();

            MatrixRefreshDiagnostics.RecordEmployeesSync(
                $"LOAD_FULL DONE groupId={groupId} durMs={swLoad.Elapsed.TotalMilliseconds:0} " +
                $"members={members.Count} days={days.Count} grpYM={grp.Year}/{grp.Month}");

            MatrixRefreshDiagnostics.RecordAllocDelta("EMP_SYNC:LOAD_FULL_ALLOC", snapLoad, $"groupId={groupId}");
            MatrixRefreshDiagnostics.Snapshot($"After LoadFullAsync (EMP_SYNC) groupId={groupId}");

            if (ct.IsCancellationRequested)
                return;

            // Якщо блок вже закрили/прибрали — виходимо раніше (до UI)
            if (!ScheduleEditVm.Blocks.Contains(block))
                return;

            // ---- Normalize members: один запис на EmployeeId (пріоритет тому, де Employee != null) ----
            var duplicates = 0;
            var memberByEmpId = new Dictionary<int, AvailabilityGroupMemberModel>(capacity: Math.Max(16, members.Count));

            foreach (var m in members)
            {
                var empId = m.EmployeeId;

                if (memberByEmpId.TryGetValue(empId, out var existing))
                {
                    duplicates++;
                    if (existing.Employee == null && m.Employee != null)
                        memberByEmpId[empId] = m;
                }
                else
                {
                    memberByEmpId[empId] = m;
                }
            }

            if (duplicates > 0)
            {
                MatrixRefreshDiagnostics.RecordEmployeesSync(
                    $"WARN groupId={groupId} duplicateMembers={duplicates} uniqueMembers={memberByEmpId.Count}");
            }

            bool changed = false;
            int removed = 0, added = 0, empRefFixed = 0, existingDuplicates = 0;

            await RunOnUiThreadAsync(() =>
            {
                if (!ScheduleEditVm.Blocks.Contains(block))
                    return;

                // зберегти вже введені MinHoursMonth (на випадок дублікатів — беремо перший)
                // ❗ ВАЖЛИВО: робимо int? щоб не ловити "int? -> int"
                var oldMin = new Dictionary<int, int?>();
                foreach (var e in block.Employees)
                {
                    if (!oldMin.ContainsKey(e.EmployeeId))
                        oldMin[e.EmployeeId] = e.MinHoursMonth;
                }

                // індекс існуючих працівників у блоці (і відлов дублікатів)
                var existingById = new Dictionary<int, ScheduleEmployeeModel>();
                for (int i = 0; i < block.Employees.Count; i++)
                {
                    var e = block.Employees[i];
                    if (!existingById.TryAdd(e.EmployeeId, e))
                        existingDuplicates++;
                }

                // 1) прибрати тих, кого нема в групі
                for (int i = block.Employees.Count - 1; i >= 0; i--)
                {
                    var e = block.Employees[i];
                    if (!memberByEmpId.ContainsKey(e.EmployeeId))
                    {
                        block.Employees.RemoveAt(i);
                        removed++;
                        changed = true;
                    }
                }

                // 2) додати відсутніх (і підтягнути Employee reference)
                foreach (var kv in memberByEmpId)
                {
                    var empId = kv.Key;
                    var m = kv.Value;

                    if (existingById.TryGetValue(empId, out var existing))
                    {
                        if (existing.Employee == null && m.Employee != null)
                        {
                            existing.Employee = m.Employee;
                            empRefFixed++;
                            changed = true;
                        }
                        continue;
                    }

                    oldMin.TryGetValue(empId, out var min);

                    block.Employees.Add(new ScheduleEmployeeModel
                    {
                        EmployeeId = empId,
                        Employee = m.Employee,
                        MinHoursMonth = min // <- int? (як у твоїй моделі, судячи з помилки)
                    });

                    added++;
                    changed = true;
                }
            }).ConfigureAwait(false);

            MatrixRefreshDiagnostics.RecordEmployeesSync(
                $"DONE groupId={groupId} blockId={block.Model.Id} changed={changed} " +
                $"removed={removed} added={added} empRefFixed={empRefFixed} existingDupInBlock={existingDuplicates} " +
                $"employeesAfter={block.Employees.Count}");

            // ✅ рефреш матриці тільки якщо реально були зміни і блок ще активний
            if (changed && !ct.IsCancellationRequested && ReferenceEquals(ScheduleEditVm.SelectedBlock, block))
            {
                MatrixRefreshDiagnostics.Step("SyncEmployees: triggering RefreshScheduleMatrixAsync (changed=true)");
                await ScheduleEditVm.RefreshScheduleMatrixAsync(ct).ConfigureAwait(false);
            }
        }
    }
}
