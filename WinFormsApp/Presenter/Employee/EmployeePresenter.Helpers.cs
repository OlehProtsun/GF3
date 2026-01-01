using System;
using System.Collections.Generic;
using System.Text;

namespace WinFormsApp.Presenter.Employee
{
    public partial class EmployeePresenter
    {
        private async Task RunSafeAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (OperationCanceledException)
            {
                // нормальна ситуація при швидких пошуках/перемиканні
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }
    }
}
