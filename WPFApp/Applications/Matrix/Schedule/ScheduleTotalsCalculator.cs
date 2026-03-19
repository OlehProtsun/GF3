/*
  Опис файлу: цей модуль містить реалізацію компонента ScheduleTotalsCalculator у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Models;

namespace WPFApp.Applications.Matrix.Schedule
{
    /// <summary>
    /// Визначає публічний елемент `public static class ScheduleTotalsCalculator` та контракт його використання у шарі WPFApp.
    /// </summary>
    public static class ScheduleTotalsCalculator
    {
        /// <summary>
        /// Визначає публічний елемент `public sealed class TotalsResult` та контракт його використання у шарі WPFApp.
        /// </summary>
        public sealed class TotalsResult
        {
            /// <summary>
            /// Визначає публічний елемент `public int TotalEmployees { get; init; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public int TotalEmployees { get; init; }
            /// <summary>
            /// Визначає публічний елемент `public TimeSpan TotalDuration { get; init; }` та контракт його використання у шарі WPFApp.
            /// </summary>
            public TimeSpan TotalDuration { get; init; }
            /// <summary>
            /// Визначає публічний елемент `public IReadOnlyDictionary<int, TimeSpan> PerEmployeeDuration { get; init; } = new Dictionary<int, TimeSpan>();` та контракт його використання у шарі WPFApp.
            /// </summary>
            public IReadOnlyDictionary<int, TimeSpan> PerEmployeeDuration { get; init; } = new Dictionary<int, TimeSpan>();
        }

        /// <summary>
        /// Визначає публічний елемент `public static TotalsResult Calculate(IReadOnlyList<ScheduleEmployeeModel> employees, IReadOnlyList<ScheduleSlotModel> sl` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static TotalsResult Calculate(IReadOnlyList<ScheduleEmployeeModel> employees, IReadOnlyList<ScheduleSlotModel> slots)
        {
            var result = BusinessLogicLayer.Schedule.ScheduleTotalsCalculator.Calculate(employees, slots);
            return new TotalsResult
            {
                TotalEmployees = result.TotalEmployees,
                TotalDuration = result.TotalDuration,
                PerEmployeeDuration = result.PerEmployeeDuration
            };
        }

        /// <summary>
        /// Визначає публічний елемент `public static string FormatHoursMinutes(TimeSpan t) => BusinessLogicLayer.Schedule.ScheduleTotalsCalculator.FormatHoursM` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static string FormatHoursMinutes(TimeSpan t) => BusinessLogicLayer.Schedule.ScheduleTotalsCalculator.FormatHoursMinutes(t);
    }
}
