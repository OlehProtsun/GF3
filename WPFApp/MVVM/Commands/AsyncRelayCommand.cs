using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WPFApp.MVVM.Commands
{
    /// <summary>
    /// AsyncRelayCommand — async ICommand для задач типу:
    /// - завантажити дані з сервісу
    /// - зберегти зміни
    /// - навігація/ініціалізація
    ///
    /// Основні проблеми базового async-void ICommand:
    /// - exception з await може впасти в SynchronizationContext і “завалити” застосунок
    /// - потрібна блокіровка повторних запусків (reentrancy)
    ///
    /// Покращення:
    /// 1) Внутрішній стан IsRunning (через Interlocked), щоб не запускати повторно.
    /// 2) Ловимо exception:
    ///    - викликаємо onException (якщо передали)
    ///    - піднімаємо ExecutionFailed (event) для зовнішнього логування/діалогу
    /// 3) Cancel():
    ///    - якщо execute підтримує token, можна скасувати поточне виконання
    /// 4) RaiseCanExecuteChanged маршалиться на UI thread (Dispatcher).
    /// </summary>
    public sealed class AsyncRelayCommand : ICommand
    {
        // Основний execute делегат уніфікований під CancellationToken.
        private readonly Func<CancellationToken, Task> _execute;

        // Optional canExecute.
        private readonly Func<bool>? _canExecute;

        // Optional exception handler.
        private readonly Action<Exception>? _onException;

        // 0 = не виконується, 1 = виконується.
        private int _isRunning;

        // CTS поточного виконання (для Cancel()).
        private CancellationTokenSource? _executionCts;

        /// <summary>
        /// Подія: виконання завершилось помилкою (exception не був кинутий далі).
        /// Можна підписатись і показати CustomMessageBox.
        /// </summary>
        public event EventHandler<Exception>? ExecutionFailed;

        /// <summary>
        /// Подія CanExecuteChanged — WPF використовує її, щоб оновити enabled стан кнопок.
        /// </summary>
        public event EventHandler? CanExecuteChanged;

        /// <summary>
        /// IsRunning — чи команда зараз виконується.
        /// </summary>
        public bool IsRunning => Volatile.Read(ref _isRunning) != 0;

        /// <summary>
        /// Конструктор для існуючого стилю: Func&lt;Task&gt;.
        /// Всередині обгортаємо у Func&lt;CancellationToken, Task&gt;.
        /// </summary>
        public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null, Action<Exception>? onException = null)
            : this(
                execute: ct => execute(), // token ігнорується
                canExecute: canExecute,
                onException: onException)
        {
            // Тут нічого робити не треба: все зроблено в “головному” конструкторі.
        }

        /// <summary>
        /// Конструктор з підтримкою CancellationToken.
        /// Рекомендовано для довгих операцій, які можуть бути скасовані.
        /// </summary>
        public AsyncRelayCommand(Func<CancellationToken, Task> execute, Func<bool>? canExecute = null, Action<Exception>? onException = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _onException = onException;
        }

        public bool CanExecute(object? parameter)
        {
            // Команда НЕ може виконуватись повторно, якщо вже IsRunning.
            if (IsRunning)
                return false;

            // Якщо canExecute не заданий — true.
            return _canExecute?.Invoke() ?? true;
        }

        public async void Execute(object? parameter)
        {
            // 1) Якщо зараз не можна виконати — вихід.
            if (!CanExecute(parameter))
                return;

            // 2) Ставимо стан "running" атомарно.
            //    Якщо хтось паралельно встиг — додатковий захист.
            if (Interlocked.Exchange(ref _isRunning, 1) == 1)
                return;

            // 3) Після зміни IsRunning — просимо WPF перерахувати CanExecute.
            RaiseCanExecuteChanged();

            // 4) Готуємо CTS для цього запуску (для Cancel()).
            //    Якщо попередній CTS ще висить — dispose його (безпека).
            _executionCts?.Dispose();
            _executionCts = new CancellationTokenSource();

            try
            {
                // 5) Виконуємо делегат.
                //    Важливо: НЕ робимо ConfigureAwait(false), щоб повернутись в UI контекст за замовчуванням.
                await _execute(_executionCts.Token);
            }
            catch (OperationCanceledException) when (_executionCts.IsCancellationRequested)
            {
                // 6) Скасування — це НЕ помилка.
                //    Нічого не робимо.
            }
            catch (Exception ex)
            {
                // 7) Будь-який інший exception:
                //    - НЕ даємо йому “вивалитись” в UI SynchronizationContext (і завалити додаток).
                //    - віддаємо наверх через event/handler.
                _onException?.Invoke(ex);
                ExecutionFailed?.Invoke(this, ex);
            }
            finally
            {
                // 8) Скидаємо running стан.
                Interlocked.Exchange(ref _isRunning, 0);

                // 9) Після завершення — оновлюємо CanExecute.
                RaiseCanExecuteChanged();

                // 10) CTS більше не потрібний (але Dispose зробимо при наступному запуску або в Cancel()).
            }
        }

        /// <summary>
        /// Cancel — скасувати поточне виконання (якщо воно є).
        /// Працює лише якщо execute делегат поважає CancellationToken.
        /// </summary>
        public void Cancel()
        {
            // Якщо CTS є — просимо cancel.
            _executionCts?.Cancel();
        }

        /// <summary>
        /// RaiseCanExecuteChanged — підняти CanExecuteChanged.
        /// Якщо виклик з background thread — маршалимо на UI.
        /// </summary>
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
    }
}
