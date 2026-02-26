/*
  Опис файлу: цей модуль містить реалізацію компонента ScheduleMatrixConstants у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
namespace WPFApp.Applications.Matrix.Schedule
{
    /// <summary>
    /// Визначає публічний елемент `public static class ScheduleMatrixConstants` та контракт його використання у шарі WPFApp.
    /// </summary>
    public static class ScheduleMatrixConstants
    {
        /// <summary>
        /// Визначає публічний елемент `public static string DayColumnName => BusinessLogicLayer.Schedule.ScheduleMatrixConstants.DayColumnName;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static string DayColumnName => BusinessLogicLayer.Schedule.ScheduleMatrixConstants.DayColumnName;
        /// <summary>
        /// Визначає публічний елемент `public static string ConflictColumnName => BusinessLogicLayer.Schedule.ScheduleMatrixConstants.ConflictColumnName;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static string ConflictColumnName => BusinessLogicLayer.Schedule.ScheduleMatrixConstants.ConflictColumnName;
        /// <summary>
        /// Визначає публічний елемент `public static string WeekendColumnName => BusinessLogicLayer.Schedule.ScheduleMatrixConstants.WeekendColumnName;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static string WeekendColumnName => BusinessLogicLayer.Schedule.ScheduleMatrixConstants.WeekendColumnName;
        /// <summary>
        /// Визначає публічний елемент `public static string EmptyMark => BusinessLogicLayer.Schedule.ScheduleMatrixConstants.EmptyMark;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static string EmptyMark => BusinessLogicLayer.Schedule.ScheduleMatrixConstants.EmptyMark;
        /// <summary>
        /// Визначає публічний елемент `public static string[] TimeFormats => BusinessLogicLayer.Schedule.ScheduleMatrixConstants.TimeFormats;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static string[] TimeFormats => BusinessLogicLayer.Schedule.ScheduleMatrixConstants.TimeFormats;
    }
}
