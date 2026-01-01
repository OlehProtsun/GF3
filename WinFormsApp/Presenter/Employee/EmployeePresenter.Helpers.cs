using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WinFormsApp.Presenter.Employee
{
    public partial class EmployeePresenter
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
                    _view.ShowError(ex.Message);
                }
            }, ct, busyText);
        }
    }
}
