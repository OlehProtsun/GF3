using System;
using System.Threading;

namespace WPFApp.ViewModel.Availability.Edit
{
    /// <summary>
    /// Батчинг:
    /// - EnterMatrixUpdate(): агрегує багато змін матриці в 1 UI-нотифікацію
    /// - EnterDateSync(): агрегує одночасні зміни Year+Month (щоб RegenerateGroupDays був один раз)
    /// </summary>
    public sealed partial class AvailabilityEditViewModel
    {
        // ----------------------------
        // Matrix batching
        // ----------------------------

        private IDisposable EnterMatrixUpdate()
        {
            // 1) Збільшуємо depth — означає “ми в батчі”.
            _matrixUpdateDepth++;

            // 2) Повертаємо scope-об’єкт, який на Dispose “закриє” батч.
            return new MatrixUpdateScope(this);
        }

        private sealed class MatrixUpdateScope : IDisposable
        {
            private AvailabilityEditViewModel? _vm;

            public MatrixUpdateScope(AvailabilityEditViewModel vm)
                => _vm = vm;

            public void Dispose()
            {
                // 1) Робимо Dispose безпечним при повторному виклику.
                var vm = Interlocked.Exchange(ref _vm, null);
                if (vm is null)
                    return;

                // 2) Зменшуємо depth.
                vm._matrixUpdateDepth--;

                // 3) Якщо це останній scope і накопичено pending change — відправляємо один сигнал.
                if (vm._matrixUpdateDepth == 0 && vm._pendingMatrixChanged)
                {
                    vm._pendingMatrixChanged = false;
                    vm.NotifyMatrixChangedCore();
                }
            }
        }

        private void NotifyMatrixChanged()
        {
            // 1) Якщо зараз в батчі — не сповіщаємо UI одразу.
            if (_matrixUpdateDepth > 0)
            {
                // 2) Лише запам’ятовуємо, що “щось змінилося”.
                _pendingMatrixChanged = true;
                return;
            }

            // 3) Якщо не в батчі — сповіщаємо одразу.
            NotifyMatrixChangedCore();
        }

        private void NotifyMatrixChangedCore()
        {
            // 1) Піднімаємо подію.
            MatrixChanged?.Invoke(this, EventArgs.Empty);

            // 2) Для WPF корисно також явно “торкнути” AvailabilityDays,
            //    бо DataView може бути тим самим об’єктом, але структура/дані всередині змінилися.
            OnPropertyChanged(nameof(AvailabilityDays));
        }

        // ----------------------------
        // Date sync batching (Year+Month)
        // ----------------------------

        private IDisposable EnterDateSync()
        {
            // 1) Збільшуємо depth — означає “йде пакетна зміна дати”.
            _dateSyncDepth++;

            // 2) Повертаємо scope.
            return new DateSyncScope(this);
        }

        private sealed class DateSyncScope : IDisposable
        {
            private AvailabilityEditViewModel? _vm;

            public DateSyncScope(AvailabilityEditViewModel vm)
                => _vm = vm;

            public void Dispose()
            {
                // 1) Dispose safe.
                var vm = Interlocked.Exchange(ref _vm, null);
                if (vm is null)
                    return;

                // 2) Зменшуємо depth.
                vm._dateSyncDepth--;

                // 3) Якщо це був останній scope — робимо одну регенерацію рядків.
                if (vm._dateSyncDepth == 0)
                    vm.RegenerateGroupDays();
            }
        }
    }
}
