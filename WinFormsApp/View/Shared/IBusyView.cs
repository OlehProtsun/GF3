using System;
using System.Threading;
using System.Threading.Tasks;

namespace WinFormsApp.View.Shared
{
    public interface IBusyView
    {
        CancellationToken LifetimeToken { get; }

        void ShowBusy(string? text = null);
        void HideBusy();
        Task RunBusyAsync(Func<CancellationToken, Task> action, CancellationToken ct, string? text = null);
        Task RunBusyAsync(Func<CancellationToken, IProgress<int>?, Task> action, CancellationToken ct, string? text = null);
    }
}
