/*
  Опис файлу: цей модуль містить реалізацію компонента AvailabilityViewModel.NavStatus у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WPFApp.View.Dialogs;

namespace WPFApp.ViewModel.Availability.Main
{
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class AvailabilityViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class AvailabilityViewModel
    {
        private bool _isNavStatusVisible;
        /// <summary>
        /// Визначає публічний елемент `public bool IsNavStatusVisible` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool IsNavStatusVisible
        {
            get => _isNavStatusVisible;
            private set => SetProperty(ref _isNavStatusVisible, value);
        }

        private UIStatusKind _navStatus = UIStatusKind.Success;
        /// <summary>
        /// Визначає публічний елемент `public UIStatusKind NavStatus` та контракт його використання у шарі WPFApp.
        /// </summary>
        public UIStatusKind NavStatus
        {
            get => _navStatus;
            private set => SetProperty(ref _navStatus, value);
        }

        private CancellationTokenSource? _navUiCts;

        private CancellationToken ResetNavUiCts(CancellationToken outer)
        {
            _navUiCts?.Cancel();
            _navUiCts?.Dispose();
            _navUiCts = CancellationTokenSource.CreateLinkedTokenSource(outer);
            return _navUiCts.Token;
        }

        private Task ShowNavWorkingAsync()
            => RunOnUiThreadAsync(() =>
            {
                NavStatus = UIStatusKind.Working;
                IsNavStatusVisible = true;
            });

        private Task HideNavStatusAsync()
            => RunOnUiThreadAsync(() => IsNavStatusVisible = false);

        private Task WaitForUiIdleAsync()
            => Application.Current.Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle).Task;

        private async Task ShowNavSuccessThenAutoHideAsync(CancellationToken ct, int ms = 900)
        {
            await RunOnUiThreadAsync(() =>
            {
                NavStatus = UIStatusKind.Success;
                IsNavStatusVisible = true;
            }).ConfigureAwait(false);

            try { await Task.Delay(ms, ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { return; }

            await HideNavStatusAsync().ConfigureAwait(false);
        }
    }
}
