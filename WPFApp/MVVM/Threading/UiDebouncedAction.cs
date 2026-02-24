using System;
using System.Threading;
using System.Threading.Tasks;

namespace WPFApp.MVVM.Threading
{
    /// <summary>
    /// UiDebouncedAction — маленький “двигун дебаунсу” для UI-подій.
    ///
    /// Простими словами:
    /// - Коли користувач швидко клацає/друкує/міняє Selection багато разів,
    ///   ми НЕ хочемо виконувати важку/дорогу дію на кожен клік.
    /// - Ми хочемо “почекати трошки” (delay),
    ///   і якщо за цей час було ще одне оновлення — скасувати попереднє.
    ///
    /// Цей клас:
    /// 1) тримає внутрішній CancellationTokenSource (CTS) для “останнього запланованого запуску”
    /// 2) при кожному Schedule(...) скасовує попередній CTS
    /// 3) чекає delay
    /// 4) виконує action ТІЛЬКИ якщо це все ще останній актуальний запуск
    ///
    /// Важливо:
    /// - сам клас НЕ знає про WPF Dispatcher напряму,
    ///   тому ми передаємо delegate runOnUiThreadAsync, який вміє виконати Action в UI thread.
    /// - цей клас не залежить від твого ViewModel => його легко перевикористати.
    /// </summary>
    public sealed class UiDebouncedAction : IDisposable
    {
        /// <summary>
        /// Делегат, який виконує передану Action саме в UI thread.
        /// У тебе це зазвичай _owner.RunOnUiThreadAsync(() => { ... }).
        /// </summary>
        private readonly Func<Action, Task> _runOnUiThreadAsync;

        /// <summary>
        /// Скільки чекати перед виконанням дії.
        /// Якщо за цей час прийде новий Schedule(...) — старий запуск скасується.
        /// </summary>
        private readonly TimeSpan _delay;

        /// <summary>
        /// Поточний “актуальний” CTS.
        /// Кожен Schedule(...) створює новий CTS і зберігає його тут.
        /// Якщо в цей момент був попередній CTS — його скасовуємо.
        /// </summary>
        private CancellationTokenSource? _cts;

        /// <summary>
        /// Конструктор.
        /// runOnUiThreadAsync — як виконати дію в UI thread.
        /// delay — затримка debounce (наприклад 200ms).
        /// </summary>
        public UiDebouncedAction(Func<Action, Task> runOnUiThreadAsync, TimeSpan delay)
        {
            _runOnUiThreadAsync = runOnUiThreadAsync ?? throw new ArgumentNullException(nameof(runOnUiThreadAsync));
            _delay = delay;
        }

        /// <summary>
        /// Запланувати виконання action з debounce.
        ///
        /// Логіка:
        /// 1) Скасувати попередній запуск (якщо був)
        /// 2) Створити новий CTS (це “id” поточного запуску)
        /// 3) Запустити async-процедуру:
        ///    - почекати delay
        ///    - перейти в UI thread
        ///    - ще раз перевірити, що CTS все ще актуальний
        ///    - виконати action
        /// </summary>
        public void Schedule(Action action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));

            // 1) Забираємо попередній CTS (якщо був) і скасовуємо його.
            //    Interlocked.Exchange гарантує, що ми “атомарно” замінимо поле.
            var prev = Interlocked.Exchange(ref _cts, null);
            CancelAndDispose(prev);

            // 2) Створюємо новий CTS — тепер він “актуальний”
            var localCts = new CancellationTokenSource();
            _cts = localCts;

            // 3) Запускаємо асинхронну логіку (fire-and-forget).
            //    Важливо: ми НЕ робимо await тут, бо Schedule має бути миттєвим.
            _ = RunAsync(localCts, action);
        }

        /// <summary>
        /// Скасувати поточний запланований запуск (якщо є).
        /// Корисно, коли VM закривається/змінюється контекст.
        /// </summary>
        public void Cancel()
        {
            var prev = Interlocked.Exchange(ref _cts, null);
            CancelAndDispose(prev);
        }

        /// <summary>
        /// Dispose — просто скасовуємо і прибираємо ресурси.
        /// </summary>
        public void Dispose()
        {
            Cancel();
        }

        /// <summary>
        /// Внутрішня асинхронна процедура:
        /// - delay
        /// - UI thread
        /// - “stale guard” (перевірка, що запуск все ще актуальний)
        /// - виконання action
        /// </summary>
        private async Task RunAsync(CancellationTokenSource localCts, Action action)
        {
            try
            {
                var token = localCts.Token;

                // 1) Чекаємо debounce delay.
                await Task.Delay(_delay, token).ConfigureAwait(false);

                // 2) Якщо за цей час нас скасували — тихо виходимо.
                if (token.IsCancellationRequested)
                    return;

                // 3) Переходимо у UI thread
                await _runOnUiThreadAsync(() =>
                {
                    // 4) Дві важливі перевірки “актуальності”:

                    // 4.1) Якщо токен вже скасували — не виконуємо.
                    if (token.IsCancellationRequested)
                        return;

                    // 4.2) Якщо в полі _cts уже лежить інший CTS — значить був новіший Schedule().
                    //      Тоді цей запуск “застарів” і не має права змінювати стан.
                    if (!ReferenceEquals(_cts, localCts))
                        return;

                    // 5) Виконуємо реальну дію (наприклад: поставити ScheduleShopId)
                    action();
                }).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Нормальна ситуація — користувач зробив новий Schedule(), ми скасували попередній.
            }
            finally
            {
                // 1) Якщо ми все ще актуальні — чистимо поле.
                //    Якщо ні — значить _cts вже інший, чіпати його не можна.
                if (ReferenceEquals(_cts, localCts))
                    _cts = null;

                // 2) Звільняємо ресурси CTS
                //    Dispose безпечний навіть якщо cancel вже був.
                try { localCts.Dispose(); } catch { /* ignore */ }
            }
        }

        /// <summary>
        /// Малий helper: скасувати і dispose CTS.
        /// Тут try/catch — щоб не падати на “вже dispose” або інших edge-case.
        /// </summary>
        private static void CancelAndDispose(CancellationTokenSource? cts)
        {
            if (cts is null) return;

            try { cts.Cancel(); } catch { /* ignore */ }
            try { cts.Dispose(); } catch { /* ignore */ }
        }
    }
}
