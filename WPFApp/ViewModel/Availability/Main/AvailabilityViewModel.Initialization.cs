/*
  Опис файлу: цей модуль містить реалізацію компонента AvailabilityViewModel.Initialization у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WPFApp.ViewModel.Availability.Main
{
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class AvailabilityViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class AvailabilityViewModel
    {
        
        private bool _initialized;

        
        private Task? _initializeTask;

        
        private readonly object _initLock = new();

        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public Task EnsureInitializedAsync(CancellationToken ct = default)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public Task EnsureInitializedAsync(CancellationToken ct = default)
        {
            
            if (_initialized)
                return Task.CompletedTask;

            
            if (_initializeTask != null)
                return _initializeTask;

            
            lock (_initLock)
            {
                
                if (_initialized)
                    return Task.CompletedTask;

                
                if (_initializeTask != null)
                    return _initializeTask;

                
                _initializeTask = InitializeCoreAsync(ct);

                
                return _initializeTask;
            }
        }

        private async Task InitializeCoreAsync(CancellationToken ct)
        {
            try
            {
                
                await LoadAllGroupsAsync(ct);

                
                await LoadEmployeesAsync(ct);

                
                await LoadBindsAsync(ct);

                
                _initialized = true;
            }
            catch
            {
                
                
                
                lock (_initLock)
                {
                    _initializeTask = null;
                    _initialized = false;
                }

                
                throw;
            }
        }

        
        
        

        private async Task LoadAllGroupsAsync(CancellationToken ct = default)
        {
            var list = await _availabilityService.GetAllAsync(ct);

            await RunOnUiThreadAsync(() => ListVm.SetItems(list));
        }

        private async Task LoadEmployeesAsync(CancellationToken ct = default)
        {
            var employees = (await _employeeService.GetAllAsync(ct)).ToList();

            await RunOnUiThreadAsync(() =>
            {
                _allEmployees.Clear();
                _employeeNames.Clear();

                _allEmployees.AddRange(employees);

                foreach (var e in employees)
                    _employeeNames[e.Id] = $"{e.FirstName} {e.LastName}";

                EditVm.SetEmployees(_allEmployees, _employeeNames);
            });
        }

        private async Task LoadBindsAsync(CancellationToken ct = default)
        {
            var binds = await _bindService.GetAllAsync(ct);

            await RunOnUiThreadAsync(() => EditVm.SetBinds(binds));
        }
    }
}
