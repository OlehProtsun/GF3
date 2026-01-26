namespace WPFApp.ViewModel.Availability.Helpers
{
    /// <summary>
    /// EmployeeListItem — мінімальна модель для відображення працівника в UI-списках.
    ///
    /// Чому не EmployeeModel напряму:
    /// - у списках часто потрібно лише Id + красивий підпис
    /// - не хочеться тягнути зайві поля/навігаційні властивості в UI
    /// - спрощує DataTemplate і зменшує зв’язність UI з DAL-моделями
    ///
    /// Цей клас immutable (init-only), бо:
    /// - елементи списку працівників зазвичай не редагуються в UI
    /// - immutable-об’єкти простіше тримати в пам’яті і кешах
    /// </summary>
    public sealed class EmployeeListItem
    {
        /// <summary>
        /// Id працівника (ключ).
        /// Використовується для:
        /// - додавання/видалення колонки в матриці
        /// - звернень до сервісів/логіки по employeeId
        /// </summary>
        public int Id { get; init; }

        /// <summary>
        /// FullName — відображуване ім’я (наприклад "Ivan Petrenko").
        /// Значення за замовчуванням — "" щоб не мати null в UI.
        /// </summary>
        public string FullName { get; init; } = string.Empty;

        /// <summary>
        /// ToString — корисно для:
        /// - debugger view
        /// - деяких WPF fallback сценаріїв (коли binding не вказаний явно)
        /// </summary>
        public override string ToString() => FullName;
    }
}
