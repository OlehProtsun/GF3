/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerScheduleEditViewModel.Selection у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Models;
using System;
using System.Data;
using WPFApp.ViewModel.Dialogs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using BusinessLogicLayer.Availability;
using WPFApp.UI.Dialogs;
using WPFApp.MVVM.Threading;
using WPFApp.Applications.Preview;



namespace WPFApp.ViewModel.Container.ScheduleEdit
{
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class ContainerScheduleEditViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class ContainerScheduleEditViewModel
    {
        
        
        

        
        
        
        
        
        
        
        
        
        
        
        
        private static readonly TimeSpan SelectionDebounceDelay = TimeSpan.FromMilliseconds(200);

        
        
        
        
        private readonly UiDebouncedAction _shopSelectionDebounce;

        
        
        
        
        
        
        private readonly UiDebouncedAction _availabilitySelectionDebounce;

        
        
        
        
        private void CancelSelectionDebounce()
        {
            _shopSelectionDebounce.Cancel();
            _availabilitySelectionDebounce.Cancel();
        }


        
        
        

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        private void ScheduleShopSelectionChange(int newId)
        {
            _shopSelectionDebounce.Schedule(() =>
            {
                
                
                
                if (SelectedShop?.Id != newId)
                    return;

                
                
                ScheduleShopId = newId;
            });
        }


        
        
        

        
        
        
        
        
        
        
        
        
        
        
        
        private void ScheduleAvailabilitySelectionChange(int newId)
        {
            _availabilitySelectionDebounce.Schedule(() =>
            {
                
                
                if (SelectedAvailabilityGroup?.Id != newId)
                    return;

                
                if (SelectedBlock is null)
                    return;

                
                SelectedBlock.SelectedAvailabilityGroupId = newId;

                
                
                
                InvalidateGeneratedSchedule(clearPreviewMatrix: true);
                SafeForget(LoadAvailabilityContextAsync(newId));
            });
        }


        
        
        

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        internal void InvalidateGeneratedSchedule(bool clearPreviewMatrix = true)
        {
            if (SelectedBlock is null)
                return;

            
            if (SelectedBlock.Slots.Count > 0)
                SelectedBlock.Slots.Clear();

            
            ScheduleMatrix = new DataView();

            
            if (clearPreviewMatrix)
            {
                AvailabilityPreviewMatrix = new DataView();
                _availabilityPreviewKey = null; 
            }

            
            RecalculateTotals();

            
            MatrixChanged?.Invoke(this, EventArgs.Empty);
        }

        private async Task LoadAvailabilityContextAsync(int availabilityGroupId)
        {
            if (SelectedBlock is null)
                return;

            if (availabilityGroupId <= 0)
            {
                await _owner.RunOnUiThreadAsync(() =>
                {
                    AvailabilityPreviewMatrix = new DataView();
                    _availabilityPreviewKey = null;
                    MatrixChanged?.Invoke(this, EventArgs.Empty);
                }).ConfigureAwait(false);

                return;
            }

            var year = ScheduleYear;
            var month = ScheduleMonth;
            if (year < 1 || month < 1 || month > 12)
                return;

            var expectedGroupId = availabilityGroupId;
            var expectedYear = year;
            var expectedMonth = month;

            var previewKey = $"AV|{availabilityGroupId}|{year}|{month}";

            try
            {
                
                var (group, members, days) =
                    await _availabilityGroupService.LoadFullAsync(availabilityGroupId, CancellationToken.None)
                                                   .ConfigureAwait(false);

                
                if (SelectedAvailabilityGroup?.Id != expectedGroupId)
                    return;
                if (ScheduleYear != expectedYear || ScheduleMonth != expectedMonth)
                    return;

                
                var employees = await ResolveEmployeesForMembersAsync(members, CancellationToken.None)
                    .ConfigureAwait(false);

                List<ScheduleEmployeeModel> scheduleEmployeesSnapshot = new();

                await _owner.RunOnUiThreadAsync(() =>
                {
                    ReplaceScheduleEmployeesFromAvailability(employees);
                    scheduleEmployeesSnapshot = SelectedBlock.Employees.ToList();
                }).ConfigureAwait(false);

                
                var periodMatched = (group.Year == year && group.Month == month);
                if (!periodMatched)
                {
                    await _owner.RunOnUiThreadAsync(() =>
                    {
                        AvailabilityPreviewMatrix = new DataView();
                        _availabilityPreviewKey = null;
                        MatrixChanged?.Invoke(this, EventArgs.Empty);
                    }).ConfigureAwait(false);

                    return;
                }

                
                var shift1 = TryParseShiftIntervalText(ScheduleShift1);
                var shift2 = TryParseShiftIntervalText(ScheduleShift2);

                var (_, availabilitySlots) = AvailabilityPreviewBuilder.Build(
                    members,
                    days,
                    shift1,
                    shift2,
                    CancellationToken.None);

                
                if (SelectedAvailabilityGroup?.Id != expectedGroupId)
                    return;
                if (ScheduleYear != expectedYear || ScheduleMonth != expectedMonth)
                    return;

                
                await RefreshAvailabilityPreviewMatrixAsync(
                        year, month,
                        availabilitySlots,
                        scheduleEmployeesSnapshot,
                        previewKey: previewKey)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _owner.RunOnUiThreadAsync(() =>
                {
                    CustomMessageBox.Show("Error", ex.Message, CustomMessageBoxIcon.Error, okText: "OK");
                }).ConfigureAwait(false);
            }
        }

        private void ReplaceScheduleEmployeesFromAvailability(IEnumerable<EmployeeModel> employees)
        {

            SetAvailabilityEmployees(employees);


            if (SelectedBlock is null)
                return;

            var availabilityIds = (employees ?? Enumerable.Empty<EmployeeModel>())
                .Where(e => e != null)
                .Select(e => e.Id)
                .Distinct()
                .ToHashSet();

            
            var manualEmployees = SelectedBlock.Employees
                .Where(se => se != null && !availabilityIds.Contains(se.EmployeeId))
                .ToList();

            
            var oldMin = SelectedBlock.Employees
                .Where(se => se != null && availabilityIds.Contains(se.EmployeeId))
                .GroupBy(x => x.EmployeeId)
                .ToDictionary(g => g.Key, g => g.First().MinHoursMonth);

            SelectedBlock.Employees.Clear();

            foreach (var emp in employees
                .Where(e => e != null)
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName))
            {
                var min = oldMin.TryGetValue(emp.Id, out var v) ? (v ?? 0) : 0;

                SelectedBlock.Employees.Add(new ScheduleEmployeeModel
                {
                    EmployeeId = emp.Id,
                    Employee = emp,
                    MinHoursMonth = min
                });
            }

            
            foreach (var se in manualEmployees)
            {
                if (se == null) continue;
                if (se.MinHoursMonth == null) se.MinHoursMonth = 0; 
                if (SelectedBlock.Employees.Any(x => x.EmployeeId == se.EmployeeId))
                    continue;

                SelectedBlock.Employees.Add(se);
            }

            
            _manualEmployeeIds.Clear();
            foreach (var se in manualEmployees)
                if (se != null && se.EmployeeId > 0)
                    _manualEmployeeIds.Add(se.EmployeeId);

            RebindMinHoursEmployeesView();
        }


        private void OnSchedulePeriodChanged()
        {
            
            
            
            var groupId = SelectedAvailabilityGroup?.Id ?? 0;
            if (groupId <= 0)
                return;

            
            ScheduleAvailabilitySelectionChange(groupId);
        }

        private static bool TryGetAvailabilityGroupPeriod(AvailabilityGroupModel? group, out int year, out int month)
        {
            year = 0;
            month = 0;
            if (group is null) return false;

            static int ReadInt(object obj, string propName)
            {
                var p = obj.GetType().GetProperty(propName);
                if (p == null) return 0;

                var v = p.GetValue(obj);
                if (v is int i) return i;
                if (v is null) return 0;

                
                if (v is string s && int.TryParse(s, out var parsed))
                    return parsed;

                return 0;
            }

            year = ReadInt(group, "Year");
            month = ReadInt(group, "Month");

            return year > 0 && month is >= 1 and <= 12;
        }


        private bool IsAvailabilityPeriodMismatch(AvailabilityGroupModel? group, int scheduleYear, int scheduleMonth,
            out int groupYear, out int groupMonth)
        {
            groupYear = 0;
            groupMonth = 0;

            if (scheduleYear <= 0 || scheduleMonth is < 1 or > 12)
                return false;

            if (!TryGetAvailabilityGroupPeriod(group, out groupYear, out groupMonth))
                return false;

            return groupYear != scheduleYear || groupMonth != scheduleMonth;
        }

        private async Task<List<EmployeeModel>> ResolveEmployeesForMembersAsync(
    List<AvailabilityGroupMemberModel> members,
    CancellationToken ct)
        {
            
            var empById = new Dictionary<int, EmployeeModel>();
            var neededIds = new HashSet<int>();

            foreach (var m in members)
            {
                neededIds.Add(m.EmployeeId);

                if (m.Employee != null)
                    empById[m.EmployeeId] = m.Employee;
            }

            
            if (empById.Count != neededIds.Count)
            {
                var all = await _employeeService.GetAllAsync(ct).ConfigureAwait(false);
                foreach (var e in all)
                {
                    if (neededIds.Contains(e.Id))
                        empById[e.Id] = e;
                }

                
                foreach (var m in members)
                {
                    if (m.Employee == null && empById.TryGetValue(m.EmployeeId, out var e))
                        m.Employee = e;
                }
            }

            return empById.Values
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToList();
        }

        private static (string from, string to)? TryParseShiftIntervalText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            
            if (!AvailabilityCodeParser.TryNormalizeInterval(text, out var normalized))
                return null;

            var parts = normalized.Split('-', 2,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length != 2)
                return null;

            return (parts[0], parts[1]);
        }



    }
}
