/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerViewModel.Schedules.BlockFactory у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using WPFApp.ViewModel.Container.ScheduleEdit.Helpers;

namespace WPFApp.ViewModel.Container.Edit
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class ContainerViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class ContainerViewModel
    {
        
        
        
        
        
        
        
        
        
        
        
        
        
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
                SelectedAvailabilityGroupId = 0,
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

                
                
                AvailabilityGroupId = model.AvailabilityGroupId
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
    }
}
