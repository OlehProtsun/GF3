/*
  Опис файлу: цей модуль містить реалізацію компонента AvailabilityPreviewBuilder у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Availability;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Threading;

namespace WPFApp.Applications.Preview
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public static class AvailabilityPreviewBuilder` та контракт його використання у шарі WPFApp.
    /// </summary>
    public static class AvailabilityPreviewBuilder
    {
        
        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static (List<ScheduleEmployeeModel> Employees, List<ScheduleSlotModel> Slots)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static (List<ScheduleEmployeeModel> Employees, List<ScheduleSlotModel> Slots)
            Build(
                IReadOnlyList<AvailabilityGroupMemberModel> members,
                IReadOnlyList<AvailabilityGroupDayModel> days,
                (string from, string to)? shift1,
                (string from, string to)? shift2,
                CancellationToken ct)
        {
            
            var employees = new List<ScheduleEmployeeModel>(capacity: Math.Max(16, members.Count));

            
            var slots = new List<ScheduleSlotModel>(capacity: Math.Max(64, days.Count));

            
            var seenEmp = new HashSet<int>();

            
            var seenSlot = new HashSet<(int empId, int day, string from, string to)>();

            
            
            var daysByMember = new Dictionary<int, List<AvailabilityGroupDayModel>>(capacity: Math.Max(16, members.Count));

            
            for (int i = 0; i < days.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var d = days[i];
                if (!daysByMember.TryGetValue(d.AvailabilityGroupMemberId, out var list))
                    daysByMember[d.AvailabilityGroupMemberId] = list = new List<AvailabilityGroupDayModel>(8);

                list.Add(d);
            }

            
            for (int i = 0; i < members.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var m = members[i];

                
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

                
                for (int j = 0; j < mdays.Count; j++)
                {
                    ct.ThrowIfCancellationRequested();

                    var d = mdays[j];

                    
                    if (d.Kind == AvailabilityKind.NONE)
                        continue;

                    
                    if (d.Kind == AvailabilityKind.INT)
                    {
                        if (string.IsNullOrWhiteSpace(d.IntervalStr))
                            continue;

                        
                        if (!AvailabilityCodeParser.TryNormalizeInterval(d.IntervalStr, out var normalized))
                            continue;

                        
                        if (TrySplitInterval(normalized, out var f, out var t))
                            AddSlotUnique(m.EmployeeId, d.DayOfMonth, f, t);

                        continue;
                    }

                    
                    
                    if (d.Kind == AvailabilityKind.ANY)
                    {
                        if (shift1 != null)
                            AddSlotUnique(m.EmployeeId, d.DayOfMonth, shift1.Value.from, shift1.Value.to);

                        if (shift2 != null)
                            AddSlotUnique(m.EmployeeId, d.DayOfMonth, shift2.Value.from, shift2.Value.to);
                    }
                }
            }

            return (employees, slots);

            
            
            

            void AddSlotUnique(int empId, int day, string from, string to)
            {
                
                if (!seenSlot.Add((empId, day, from, to)))
                    return;

                
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

                
                var parts = normalized.Split('-', 2,
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (parts.Length != 2)
                    return false;

                from = parts[0];
                to = parts[1];
                return true;
            }
        }
    }
}
