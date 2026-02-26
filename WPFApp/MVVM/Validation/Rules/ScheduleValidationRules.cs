/*
  Опис файлу: цей модуль містить реалізацію компонента ScheduleValidationRules у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using WPFApp.Applications.Matrix.Schedule;

namespace WPFApp.MVVM.Validation.Rules
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public static class ScheduleValidationRules` та контракт його використання у шарі WPFApp.
    /// </summary>
    public static class ScheduleValidationRules
    {
        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public const string K_ScheduleShopId = "PendingSelectedShop";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public const string K_ScheduleShopId = "PendingSelectedShop";
        /// <summary>
        /// Визначає публічний елемент `public const string K_ScheduleName = "ScheduleName";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public const string K_ScheduleName = "ScheduleName";
        /// <summary>
        /// Визначає публічний елемент `public const string K_ScheduleYear = "ScheduleYear";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public const string K_ScheduleYear = "ScheduleYear";
        /// <summary>
        /// Визначає публічний елемент `public const string K_ScheduleMonth = "ScheduleMonth";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public const string K_ScheduleMonth = "ScheduleMonth";
        /// <summary>
        /// Визначає публічний елемент `public const string K_SchedulePeoplePerShift = "SchedulePeoplePerShift";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public const string K_SchedulePeoplePerShift = "SchedulePeoplePerShift";
        /// <summary>
        /// Визначає публічний елемент `public const string K_ScheduleShift1 = "ScheduleShift1";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public const string K_ScheduleShift1 = "ScheduleShift1";
        /// <summary>
        /// Визначає публічний елемент `public const string K_ScheduleShift2 = "ScheduleShift2";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public const string K_ScheduleShift2 = "ScheduleShift2";
        /// <summary>
        /// Визначає публічний елемент `public const string K_ScheduleMaxHoursPerEmp = "ScheduleMaxHoursPerEmp";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public const string K_ScheduleMaxHoursPerEmp = "ScheduleMaxHoursPerEmp";
        /// <summary>
        /// Визначає публічний елемент `public const string K_ScheduleMaxConsecutiveDays = "ScheduleMaxConsecutiveDays";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public const string K_ScheduleMaxConsecutiveDays = "ScheduleMaxConsecutiveDays";
        /// <summary>
        /// Визначає публічний елемент `public const string K_ScheduleMaxConsecutiveFull = "ScheduleMaxConsecutiveFull";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public const string K_ScheduleMaxConsecutiveFull = "ScheduleMaxConsecutiveFull";
        /// <summary>
        /// Визначає публічний елемент `public const string K_ScheduleMaxFullPerMonth = "ScheduleMaxFullPerMonth";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public const string K_ScheduleMaxFullPerMonth = "ScheduleMaxFullPerMonth";
        /// <summary>
        /// Визначає публічний елемент `public const string K_ScheduleNote = "ScheduleNote";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public const string K_ScheduleNote = "ScheduleNote";

        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static IReadOnlyDictionary<string, string> ValidateAll(ScheduleModel? model)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static IReadOnlyDictionary<string, string> ValidateAll(ScheduleModel? model)
        {
            var errors = new Dictionary<string, string>(StringComparer.Ordinal);

            
            if (model is null)
                return errors;

            
            AddIfError(errors, K_ScheduleShopId, ValidateShopId(model.ShopId));

            
            AddIfError(errors, K_ScheduleName, ValidateName(model.Name));

            
            AddIfError(errors, K_ScheduleYear, ValidateYear(model.Year));
            AddIfError(errors, K_ScheduleMonth, ValidateMonth(model.Month));

            
            AddIfError(errors, K_SchedulePeoplePerShift, ValidatePeoplePerShift(model.PeoplePerShift));

            
            var shift1Err = ValidateShift(model.Shift1Time, required: true, out var s1From, out var s1To);
            AddIfError(errors, K_ScheduleShift1, shift1Err);

            
            var shift2Err = ValidateShift(model.Shift2Time, required: false, out var s2From, out var s2To);
            AddIfError(errors, K_ScheduleShift2, shift2Err);

            
            
            if (shift1Err is null && shift2Err is null && s1From.HasValue && s1To.HasValue && s2From.HasValue && s2To.HasValue)
            {
                if (IntervalsOverlap(s1From.Value, s1To.Value, s2From.Value, s2To.Value))
                {
                    
                    AddIfError(errors, K_ScheduleShift2, "Shift2 overlaps Shift1.");
                }
            }

            
            AddIfError(errors, K_ScheduleMaxHoursPerEmp, ValidateNonNegative(model.MaxHoursPerEmpMonth, "Max hours per employee must be >= 0."));
            AddIfError(errors, K_ScheduleMaxConsecutiveDays, ValidateNonNegative(model.MaxConsecutiveDays, "Max consecutive days must be >= 0."));
            AddIfError(errors, K_ScheduleMaxConsecutiveFull, ValidateNonNegative(model.MaxConsecutiveFull, "Max consecutive full must be >= 0."));
            AddIfError(errors, K_ScheduleMaxFullPerMonth, ValidateNonNegative(model.MaxFullPerMonth, "Max full per month must be >= 0."));

            
            if (model.MaxConsecutiveFull > model.MaxConsecutiveDays)
                AddIfError(errors, K_ScheduleMaxConsecutiveFull, "Max consecutive full cannot be greater than max consecutive days.");

            
            AddIfError(errors, K_ScheduleNote, ValidateNote(model.Note));

            return errors;
        }

        
        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static string? ValidateProperty(ScheduleModel? model, string vmPropertyName)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static string? ValidateProperty(ScheduleModel? model, string vmPropertyName)
        {
            if (model is null || string.IsNullOrWhiteSpace(vmPropertyName))
                return null;

            
            return vmPropertyName switch
            {
                K_ScheduleShopId => ValidateShopId(model.ShopId),
                K_ScheduleName => ValidateName(model.Name),
                K_ScheduleYear => ValidateYear(model.Year),
                K_ScheduleMonth => ValidateMonth(model.Month),
                K_SchedulePeoplePerShift => ValidatePeoplePerShift(model.PeoplePerShift),
                K_ScheduleShift1 => ValidateShift(model.Shift1Time, required: true, out _, out _),
                K_ScheduleShift2 => ValidateShift(model.Shift2Time, required: false, out _, out _),
                K_ScheduleMaxHoursPerEmp => ValidateNonNegative(model.MaxHoursPerEmpMonth, "Max hours per employee must be >= 0."),
                K_ScheduleMaxConsecutiveDays => ValidateNonNegative(model.MaxConsecutiveDays, "Max consecutive days must be >= 0."),
                K_ScheduleMaxConsecutiveFull => ValidateNonNegative(model.MaxConsecutiveFull, "Max consecutive full must be >= 0."),
                K_ScheduleMaxFullPerMonth => ValidateNonNegative(model.MaxFullPerMonth, "Max full per month must be >= 0."),
                K_ScheduleNote => ValidateNote(model.Note),
                _ => null
            };
        }

        
        
        

        private static string? ValidateShopId(int shopId)
        {
            
            if (shopId <= 0)
                return "Please select a shop.";

            return null;
        }

        private static string? ValidateName(string? name)
        {
            name = (name ?? string.Empty).Trim();

            if (name.Length == 0)
                return "Name is required.";

            if (name.Length > 100)
                return "Name is too long (max 100 chars).";

            return null;
        }

        private static string? ValidateYear(int year)
        {
            
            
            if (year < 2000 || year > 2100)
                return "Year must be between 2000 and 2100.";

            return null;
        }

        private static string? ValidateMonth(int month)
        {
            if (month < 1 || month > 12)
                return "Month must be between 1 and 12.";

            return null;
        }

        private static string? ValidatePeoplePerShift(int peoplePerShift)
        {
            if (peoplePerShift <= 0)
                return "People per shift must be > 0.";

            if (peoplePerShift > 200)
                return "People per shift is too large.";

            return null;
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        private static string? ValidateShift(string? shiftText, bool required, out TimeSpan? from, out TimeSpan? to)
        {
            from = null;
            to = null;

            shiftText = (shiftText ?? string.Empty).Trim();

            if (shiftText.Length == 0)
                return required ? "Shift time is required (example: 09:00 - 18:00)." : null;

            
            var parts = shiftText.Split('-', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                return "Shift format must be: HH:mm - HH:mm.";

            
            if (!ScheduleMatrixEngine.TryParseTime(parts[0], out var f) ||
                !ScheduleMatrixEngine.TryParseTime(parts[1], out var t))
            {
                return "Shift time must be HH:mm (example: 09:00 - 18:00).";
            }

            
            
            if (t <= f)
                return "Shift end must be later than shift start.";

            
            var dur = t - f;
            if (dur.TotalMinutes < 30)
                return "Shift duration is too short.";

            if (dur.TotalHours > 24)
                return "Shift duration is too long.";

            from = f;
            to = t;
            return null;
        }

        private static string? ValidateNonNegative(int value, string messageIfInvalid)
        {
            if (value < 0)
                return messageIfInvalid;

            return null;
        }

        private static string? ValidateNote(string? note)
        {
            note = note ?? string.Empty;

            if (note.Length > 2000)
                return "Note is too long (max 2000 chars).";

            return null;
        }

        
        
        
        
        private static bool IntervalsOverlap(TimeSpan aFrom, TimeSpan aTo, TimeSpan bFrom, TimeSpan bTo)
        {
            
            return aFrom < bTo && bFrom < aTo;
        }

        
        
        
        
        
        private static void AddIfError(Dictionary<string, string> errors, string key, string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
                return;

            if (!errors.ContainsKey(key))
                errors[key] = errorMessage;
        }
    }
}
