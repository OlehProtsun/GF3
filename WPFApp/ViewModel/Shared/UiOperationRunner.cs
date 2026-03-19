/*
  Опис файлу: цей модуль містить реалізацію компонента UiOperationRunner у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WPFApp.ViewModel.Shared
{
    
    
    
    
    internal static class UiOperationRunner
    {
        
        
        
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
