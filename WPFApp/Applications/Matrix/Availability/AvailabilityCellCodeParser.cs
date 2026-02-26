/*
  Опис файлу: цей модуль містить реалізацію компонента AvailabilityCellCodeParser у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using BusinessLogicLayer.Availability;
using BusinessLogicLayer.Contracts.Enums;

namespace WPFApp.Applications.Matrix.Availability
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public static class AvailabilityCellCodeParser` та контракт його використання у шарі WPFApp.
    /// </summary>
    public static class AvailabilityCellCodeParser
    {
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static string AnyMark => AvailabilityCodeParser.AnyMark;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static string AnyMark => AvailabilityCodeParser.AnyMark;

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static string NoneMark => AvailabilityCodeParser.NoneMark;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static string NoneMark => AvailabilityCodeParser.NoneMark;

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static bool TryNormalize(string? raw, out string normalized, out string? error, bool allowOvernight = false)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static bool TryNormalize(string? raw, out string normalized, out string? error, bool allowOvernight = false)
        {
            
            normalized = string.Empty;
            error = null;

            
            
            
            raw = (raw ?? string.Empty).Trim();

            
            
            if (raw.Length == 0)
                return true;

            
            
            
            
            
            
            
            if (!AvailabilityCodeParser.TryParse(raw, out var parsedKind, out var interval))
            {
                
                
                error = "Allowed: +, -, HH:mm-HH:mm or HH:mm - HH:mm (e.g., 09:00-18:00).";
                return false;
            }

            
            
            if (parsedKind == AvailabilityKind.INT && !string.IsNullOrWhiteSpace(interval))
            {
                
                
                
                
                
                if (!allowOvernight)
                {
                    
                    
                    var parts = interval.Split('-', 2, StringSplitOptions.TrimEntries);

                    
                    
                    
                    
                    if (parts.Length == 2
                        && BusinessLogicLayer.Schedule.ScheduleMatrixEngine.TryParseTime(parts[0], out var from)
                        && BusinessLogicLayer.Schedule.ScheduleMatrixEngine.TryParseTime(parts[1], out var to)
                        && to <= from)
                    {
                        error = "End time must be later than start time.";
                        return false;
                    }
                }

                
                
                
                
                normalized = interval.Replace(" - ", "-");
                return true;
            }

            
            
            
            
            normalized = parsedKind switch
            {
                AvailabilityKind.ANY => AnyMark,
                AvailabilityKind.NONE => NoneMark,
                _ => string.Empty
            };

            return true;
        }
    }
}