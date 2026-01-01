using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace WinFormsApp.Presenter.Employee
{
    public partial class EmployeePresenter
    {
        private CancellationToken BeginNewListOperation(CancellationToken outerCt, out int version)
        {
            try { _listOpCts?.Cancel(); } catch { /* ignore */ }
            _listOpCts?.Dispose();

            _listOpCts = CancellationTokenSource.CreateLinkedTokenSource(outerCt);
            version = Interlocked.Increment(ref _listOpVersion);
            return _listOpCts.Token;
        }

        private Task UpdateBindingSourceOnUiAsync(List<EmployeeModel> list, int? selectId)
        {
            var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

            _ui.Post(_ =>
            {
                try
                {
                    _bindingSource.DataSource = list;

                    if (selectId.HasValue)
                    {
                        var idx = list.Select((e, i) => (e, i))
                                      .FirstOrDefault(x => x.e.Id == selectId.Value).i;

                        if (idx >= 0 && idx < _bindingSource.Count)
                            _bindingSource.Position = idx;
                    }

                    tcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, null);

            return tcs.Task;
        }

        private async Task LoadEmployeesAsync(
            Func<CancellationToken, Task<List<EmployeeModel>>> loader,
            CancellationToken outerCt,
            int? selectId)
        {
            var ct = BeginNewListOperation(outerCt, out var version);

            var list = await loader(ct);
            ct.ThrowIfCancellationRequested();

            if (version != Volatile.Read(ref _listOpVersion)) return;

            await UpdateBindingSourceOnUiAsync(list, selectId);
        }
    }
}
