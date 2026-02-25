using System;
using System.Threading;
using System.Threading.Tasks;

namespace WPFApp.ViewModel.Shared
{
    internal static class UiOperationRunner
    {
        internal static async Task RunNavStatusFlowAsync(
            CancellationToken outerToken,
            Func<CancellationToken, CancellationToken> resetToken,
            Func<Task> showWorkingAsync,
            Func<Task> waitForUiIdleAsync,
            Func<CancellationToken, Task> bodyAsync,
            Func<CancellationToken, int, Task> showSuccessThenAutoHideAsync,
            Func<Task> hideAsync,
            Action<Exception> showError,
            int successDelayMs)
        {
            var uiToken = resetToken(outerToken);

            await showWorkingAsync().ConfigureAwait(false);
            await waitForUiIdleAsync().ConfigureAwait(false);

            try
            {
                await bodyAsync(uiToken).ConfigureAwait(false);
                await waitForUiIdleAsync().ConfigureAwait(false);
                await showSuccessThenAutoHideAsync(uiToken, successDelayMs).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await hideAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await hideAsync().ConfigureAwait(false);
                showError(ex);
            }
        }
    }
}
