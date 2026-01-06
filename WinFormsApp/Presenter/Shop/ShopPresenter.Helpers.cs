using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WinFormsApp.Presenter.Shop
{
    public partial class ShopPresenter
    {
        private Task RunBusySafeAsync(Func<CancellationToken, Task> action, CancellationToken ct, string? busyText)
        {
            return _view.RunBusyAsync(async innerCt =>
            {
                try
                {
                    await action(innerCt);
                }
                catch (OperationCanceledException)
                {
                    // нормальна ситуація при швидких пошуках/перемиканні
                }
                catch (Exception ex)
                {
                    _view.ShowError(RootMessage(ex));
                }
            }, ct, busyText);
        }

        private static string RootMessage(Exception ex)
        {
            var root = ex;
            while (root.InnerException != null) root = root.InnerException;
            return root.Message;
        }
    }
}
