using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace WinFormsApp.Presenter.Shop
{
    public partial class ShopPresenter
    {
        private CancellationToken BeginNewListOperation(CancellationToken outerCt, out int version)
        {
            try { _listOpCts?.Cancel(); } catch { /* ignore */ }
            _listOpCts?.Dispose();

            _listOpCts = CancellationTokenSource.CreateLinkedTokenSource(outerCt);
            version = Interlocked.Increment(ref _listOpVersion);
            return _listOpCts.Token;
        }

        private Task UpdateBindingSourceOnUiAsync(List<ShopModel> list, int? selectId)
        {
            var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

            _ui.Post(_ =>
            {
                try
                {
                    _bindingSource.DataSource = list;

                    if (selectId.HasValue)
                    {
                        var idx = -1;
                        for (var i = 0; i < list.Count; i++)
                        {
                            if (list[i].Id != selectId.Value) continue;
                            idx = i;
                            break;
                        }

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

        private async Task LoadShopsAsync(
            Func<CancellationToken, Task<List<ShopModel>>> loader,
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
