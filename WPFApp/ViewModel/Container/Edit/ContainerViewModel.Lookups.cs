/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerViewModel.Lookups у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WPFApp.ViewModel.Container.Edit
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class ContainerViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class ContainerViewModel
    {
        
        
        

        
        
        
        
        private readonly List<ShopModel> _allShops = new();

        
        
        
        private readonly List<AvailabilityGroupModel> _allAvailabilityGroups = new();

        
        
        
        private readonly List<EmployeeModel> _allEmployees = new();

        
        
        

        
        
        
        
        private string? _lastShopFilter;

        
        
        
        private string? _lastAvailabilityFilter;

        
        
        
        private string? _lastEmployeeFilter;

        
        
        

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
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

            
            _lastShopFilter = null;
            _lastAvailabilityFilter = null;
            _lastEmployeeFilter = null;

            
            await RunOnUiThreadAsync(() =>
            {
                ScheduleEditVm.SetLookups(shops, groups, employees);
            }).ConfigureAwait(false);
        }

        
        
        

        
        
        
        
        
        
        
        
        private void ResetScheduleFilters()
        {
            
            ScheduleEditVm.ShopSearchText = string.Empty;
            ScheduleEditVm.AvailabilitySearchText = string.Empty;
            ScheduleEditVm.EmployeeSearchText = string.Empty;

            
            _lastShopFilter = null;
            _lastAvailabilityFilter = null;
            _lastEmployeeFilter = null;

            
            ScheduleEditVm.SetLookups(_allShops, _allAvailabilityGroups, _allEmployees);
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

        
        
        

        private void ApplyShopFilter(string? raw)
        {
            ApplyFilter(
                raw,
                ref _lastShopFilter,
                _allShops,
                s => ContainsIgnoreCase(s.Name, _lastShopFilter ?? string.Empty),
                ScheduleEditVm.SetShops);
        }

        private void ApplyAvailabilityFilter(string? raw)
        {
            ApplyFilter(
                raw,
                ref _lastAvailabilityFilter,
                _allAvailabilityGroups,
                g => ContainsIgnoreCase(g.Name, _lastAvailabilityFilter ?? string.Empty),
                ScheduleEditVm.SetAvailabilityGroups);
        }

        private void ApplyEmployeeFilter(string? raw)
        {
            ApplyFilter(
                raw,
                ref _lastEmployeeFilter,
                _allEmployees,
                e => ContainsIgnoreCase(e.FirstName, _lastEmployeeFilter ?? string.Empty)
                  || ContainsIgnoreCase(e.LastName, _lastEmployeeFilter ?? string.Empty),
                ScheduleEditVm.SetEmployees);
        }

        
        
        
        
        private static bool ContainsIgnoreCase(string? source, string value)
            => (source ?? string.Empty).Contains(value, StringComparison.OrdinalIgnoreCase);

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        private void ApplyFilter<T>(
            string? raw,
            ref string? last,
            IReadOnlyList<T> all,
            Func<T, bool> predicate,
            Action<IEnumerable<T>> apply)
        {
            var term = raw?.Trim() ?? string.Empty;

            
            if (string.Equals(last, term, StringComparison.OrdinalIgnoreCase))
                return;

            last = term;

            
            if (string.IsNullOrWhiteSpace(term))
            {
                apply(all);
                return;
            }

            
            var filtered = new List<T>(capacity: Math.Min(all.Count, 256));
            for (int i = 0; i < all.Count; i++)
            {
                var item = all[i];
                if (predicate(item))
                    filtered.Add(item);
            }

            apply(filtered);
        }
    }
}
