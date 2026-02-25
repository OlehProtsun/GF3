using System;
using System.Threading;
using System.Threading.Tasks;

namespace WPFApp.ViewModel.Shared
{
    /// <summary>
    /// Shared wrapper for UI navigation status orchestration (working/success/hide/error).
    /// Keeps flow steps consistent across ViewModel modules without changing business logic.
    /// </summary>
    internal static class UiOperationRunner
    {
        /// <summary>
        /// Executes a navigation flow where the body always completes successfully unless it throws/cancels.
        /// </summary>
        internal static async Task RunNavStatusFlowAsync(
            CancellationToken outerToken,
            Func<CancellationToken, CancellationToken> createUiToken,
            Func<Task> showWorkingAsync,
            Func<Task> waitForUiIdleAsync,
            Func<CancellationToken, Task> bodyAsync,
            Func<CancellationToken, int, Task> showSuccessThenAutoHideAsync,
            Func<Task> hideAsync,
            Action<Exception> showError,
            int successDelayMs)
        {
            var uiToken = createUiToken(outerToken);

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

        /// <summary>
        /// Executes a navigation flow where the body can return <c>false</c> for a non-error early exit.
        /// </summary>
        internal static async Task RunNavStatusFlowAsync(
            CancellationToken outerToken,
            Func<CancellationToken, CancellationToken> createUiToken,
            Func<Task> showWorkingAsync,
            Func<Task> waitForUiIdleAsync,
            Func<CancellationToken, Task<bool>> bodyAsync,
            Func<CancellationToken, int, Task> showSuccessThenAutoHideAsync,
            Func<Task> hideAsync,
            Action<Exception> showError,
            int successDelayMs)
        {
            var uiToken = createUiToken(outerToken);

            await showWorkingAsync().ConfigureAwait(false);
            await waitForUiIdleAsync().ConfigureAwait(false);

            try
            {
                var completed = await bodyAsync(uiToken).ConfigureAwait(false);
                if (!completed)
                {
                    await hideAsync().ConfigureAwait(false);
                    return;
                }

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
