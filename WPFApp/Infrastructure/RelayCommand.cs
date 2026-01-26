using System;
using System.Windows;
using System.Windows.Input;

namespace WPFApp.Infrastructure
{
    /// <summary>
    /// RelayCommand — синхронна команда ICommand.
    ///
    /// Принципи:
    /// - Execute: Action
    /// - CanExecute: Func<bool> (optional)
    /// - RaiseCanExecuteChanged: вручну повідомити WPF, що CanExecute змінився
    ///
    /// Оптимізації:
    /// - RaiseCanExecuteChanged маршалиться на UI thread (Dispatcher), якщо викликано з background thread.
    /// - Нема прив’язки до CommandManager.RequerySuggested:
    ///   це зменшує глобальні “requery” і дає більш передбачувану поведінку,
    ///   а в вашому проекті ви і так явно викликаєте RaiseCanExecuteChanged().
    /// </summary>
    public sealed class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            // execute обов’язковий.
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));

            // canExecute може бути null => команда завжди доступна.
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            // Якщо canExecute заданий — викликаємо його.
            // Якщо ні — true.
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object? parameter)
        {
            // Виконуємо дію.
            _execute();
        }

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            // Беремо handler в локальну змінну.
            var handler = CanExecuteChanged;

            // Якщо ніхто не підписаний — вихід.
            if (handler is null)
                return;

            // Dispatcher з Application.Current (якщо є).
            var dispatcher = Application.Current?.Dispatcher;

            // Якщо dispatcher є і ми не на UI — піднімаємо на UI.
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(new Action(() => handler(this, EventArgs.Empty)));
                return;
            }

            // Інакше — викликаємо одразу.
            handler(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// RelayCommand&lt;T&gt; — типізована синхронна команда.
    ///
    /// Примітка по параметру:
    /// - WPF передає параметр як object?
    /// - Ми робимо строгий cast, щоб помилки binding були видимі під час розробки.
    /// </summary>
    public sealed class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            // Конвертуємо parameter у T?.
            var value = Cast(parameter);

            // Якщо canExecute не заданий — true.
            return _canExecute?.Invoke(value) ?? true;
        }

        public void Execute(object? parameter)
        {
            // Конвертуємо parameter у T?.
            var value = Cast(parameter);

            // Виконуємо дію.
            _execute(value);
        }

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            var handler = CanExecuteChanged;
            if (handler is null)
                return;

            var dispatcher = Application.Current?.Dispatcher;

            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(new Action(() => handler(this, EventArgs.Empty)));
                return;
            }

            handler(this, EventArgs.Empty);
        }

        private static T? Cast(object? parameter)
        {
            // Якщо parameter null — це нормальний кейс => default(T?).
            if (parameter is null)
                return default;

            // Якщо тип сумісний — повертаємо.
            if (parameter is T t)
                return t;

            // Якщо тип несумісний — це помилка binding/виклику (краще побачити одразу).
            throw new InvalidCastException(
                $"RelayCommand<{typeof(T).Name}> received parameter of type '{parameter.GetType().Name}', which cannot be cast.");
        }
    }
}
