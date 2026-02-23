using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace WPFApp.ViewModel.Dialogs
{
    /// <summary>
    /// Тип іконки для CustomMessageBox.
    /// </summary>
    public enum CustomMessageBoxIcon
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// CustomMessageBoxViewModel — VM для простого діалогу “повідомлення/підтвердження”.
    ///
    /// Дизайн:
    /// - Всі властивості є immutable (тільки get) і задаються у конструкторі.
    /// - ViewModel НЕ модифікується після створення, тому INotifyPropertyChanged не потрібен.
    /// - Закриття вікна здійснюється через event RequestClose(bool?):
    ///   * true  => OK / підтвердив
    ///   * false => Cancel / відхилив
    ///
    /// Примітка:
    /// - Якщо в майбутньому захочеш "Show details" з перемикачем (тобто HasDetails/DetailsVisible змінюються),
    ///   тоді має сенс повернути INotifyPropertyChanged і робити змінювані властивості.
    /// </summary>
    public sealed class CustomMessageBoxViewModel
    {
        // ------------------------------------------------------------
        // 1) Подія закриття (View підписується і закриває Window)
        // ------------------------------------------------------------

        /// <summary>
        /// RequestClose — сигнал у View: “закрий вікно і поверни DialogResult”.
        /// bool?:
        /// - true  => OK
        /// - false => Cancel
        /// - null  => закриття без явної відповіді (не використовується тут, але тип дозволяє)
        /// </summary>
        public event Action<bool?>? RequestClose;

        // ------------------------------------------------------------
        // 2) Текст / прапорці для UI
        // ------------------------------------------------------------

        /// <summary>Заголовок вікна.</summary>
        public string Title { get; }

        /// <summary>Основний текст повідомлення.</summary>
        public string Message { get; }

        /// <summary>Деталі (stack trace / expanded message). Може бути порожнім.</summary>
        public string Details { get; }

        /// <summary>Текст кнопки OK.</summary>
        public string OkText { get; }

        /// <summary>Текст кнопки Cancel. Якщо порожній — кнопка не показується.</summary>
        public string CancelText { get; }

        /// <summary>
        /// Чи показувати Cancel-кнопку.
        /// Логіка: якщо CancelText порожній або пробіли — кнопка не потрібна.
        /// </summary>
        public bool IsCancelVisible => !string.IsNullOrWhiteSpace(CancelText);

        /// <summary>
        /// Чи є деталі (для секції “details” у UI).
        /// </summary>
        public bool HasDetails => !string.IsNullOrWhiteSpace(Details);

        // ------------------------------------------------------------
        // 3) Іконка
        // ------------------------------------------------------------

        /// <summary>
        /// Геометрія іконки (Path.Data).
        /// Береться з ресурсів Application.Current (ResourceDictionary).
        /// Якщо ресурс не знайдено — Geometry.Empty (щоб VM не валився).
        /// </summary>
        public Geometry IconGeometry { get; }

        // Ключі ресурсів (одна точка правди, а не строкові літерали по коду)
        private const string IconWarnKey = "IconWarn";
        private const string IconErrorKey = "IconError";
        private const string IconInfoKey = "IconInfo";

        // ------------------------------------------------------------
        // 4) Команди
        // ------------------------------------------------------------

        /// <summary>
        /// OK — завершує діалог позитивно.
        /// </summary>
        public ICommand OkCommand { get; }

        /// <summary>
        /// Cancel — завершує діалог негативно.
        /// Якщо IsCancelVisible=false, кнопка в UI має бути прихована.
        /// </summary>
        public ICommand CancelCommand { get; }

        // ------------------------------------------------------------
        // 5) Конструктор
        // ------------------------------------------------------------

        public CustomMessageBoxViewModel(
            string title,
            string message,
            CustomMessageBoxIcon icon,
            string okText = "OK",
            string cancelText = "",
            string details = "")
        {
            // 1) Нормалізуємо тексти в null-safe вигляд.
            //    (Щоб у UI не було null binding-ів.)
            Title = title ?? string.Empty;
            Message = message ?? string.Empty;
            Details = details ?? string.Empty;

            // 2) Тексти кнопок також нормалізуємо.
            OkText = string.IsNullOrWhiteSpace(okText) ? "OK" : okText;
            CancelText = cancelText ?? string.Empty;

            // 3) Вибираємо ключ ресурсу іконки залежно від enum.
            //    Warning/Error/Info.
            var resourceKey = icon switch
            {
                CustomMessageBoxIcon.Warning => IconWarnKey,
                CustomMessageBoxIcon.Error => IconErrorKey,
                _ => IconInfoKey
            };

            // 4) Безпечно дістаємо Geometry з Application.Current.Resources.
            //    Якщо ресурс відсутній або тип не той — fallback на Geometry.Empty.
            IconGeometry = TryGetGeometryResource(resourceKey);

            // 5) Ініціалізуємо команди.
            //    Використовуємо локальний RelayCommand (нижче), який є WPF-friendly.
            OkCommand = new RelayCommand(
                execute: () => RequestClose?.Invoke(true));

            CancelCommand = new RelayCommand(
                execute: () => RequestClose?.Invoke(false),
                canExecute: () => IsCancelVisible); // якщо CancelText порожній — команду можна “вимкнути”
        }

        // ------------------------------------------------------------
        // 6) Private helpers
        // ------------------------------------------------------------

        private static Geometry TryGetGeometryResource(string resourceKey)
        {
            // 1) Якщо Application.Current == null (може бути у unit tests) — повертаємо empty.
            var app = Application.Current;
            if (app is null)
                return Geometry.Empty;

            // 2) Пробуємо знайти ресурс.
            //    FindResource кидає exception якщо не знайдено, тому використовуємо Resources.Contains + indexer.
            if (!app.Resources.Contains(resourceKey))
                return Geometry.Empty;

            // 3) Дістаємо ресурс і перевіряємо тип.
            return app.Resources[resourceKey] as Geometry ?? Geometry.Empty;
        }
    }

    /// <summary>
    /// RelayCommand — проста ICommand-реалізація.
    ///
    /// Покращення проти “мінімальної” версії:
    /// - підв’язка до CommandManager.RequerySuggested для автоматичного refresh CanExecute у WPF
    /// - RaiseCanExecuteChanged() також викликає CommandManager.InvalidateRequerySuggested()
    ///
    /// Примітка:
    /// - Якщо у проєкті вже є стандартний RelayCommand/AsyncRelayCommand в Infrastructure,
    ///   краще використовувати його, а цей клас видалити.
    ///   Але поки він локальний — робимо його максимально коректним.
    /// </summary>
    public sealed class RelayCommand : ICommand
    {
        // Делегат виконання (обов’язковий).
        private readonly Action _execute;

        // Делегат canExecute (опційний).
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            // 1) Захист від null: якщо execute == null, краще зразу отримати чітку помилку.
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));

            // 2) canExecute може бути null (тоді команда завжди доступна).
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            // 1) Якщо canExecute заданий — викликаємо його.
            // 2) Якщо ні — команда доступна завжди.
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object? parameter)
        {
            // Виконуємо дію.
            _execute();
        }

        /// <summary>
        /// CanExecuteChanged:
        /// - Для WPF стандартно підписуються на CommandManager.RequerySuggested,
        ///   щоб CanExecute перераховувався автоматично при фокусі/вводі/тощо.
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        /// <summary>
        /// Явно попросити WPF перерахувати CanExecute для команд.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            // InvalidateRequerySuggested “пхає” CommandManager підняти RequerySuggested,
            // після чого WPF викличе CanExecute для активних команд.
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
