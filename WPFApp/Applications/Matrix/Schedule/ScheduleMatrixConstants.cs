namespace WPFApp.Applications.Matrix.Schedule
{
    /// <summary>
    /// Один “центр правди” для назв колонок і форматів часу.
    /// Це важливо, бо і VM, і builder матриці мають використовувати
    /// однакові імена, інакше DataTable/DataView не співпадуть.
    /// </summary>
    public static class ScheduleMatrixConstants
    {
        // Назва колонки, де лежить номер дня місяця (1..31)
        public const string DayColumnName = "DayOfMonth";

        // Назва колонки, яка показує “чи є конфлікт” у цей день
        // (наприклад, слот без працівника або перетин інтервалів)
        public const string ConflictColumnName = "Conflict";

        // Технічна колонка: чи це вихідний (для стилів рядка у WPF)
        public const string WeekendColumnName = "IsWeekend";

        // Маркер “порожньо”, який відображається в клітинці, коли інтервалів нема
        public const string EmptyMark = "-";

        // Які формати часу ми приймаємо (09:00 або 9:00)
        public static readonly string[] TimeFormats = { @"h\:mm", @"hh\:mm" };
    }
}
