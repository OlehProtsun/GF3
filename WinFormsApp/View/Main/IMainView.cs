using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinFormsApp.ViewModel;

namespace WinFormsApp.View.Main
{
    public interface IMainView
    {
        NavPage ActivePage { get; }


        event Func<CancellationToken, Task>? ShowEmployeeView;
        event Func<CancellationToken, Task>? ShowAvailabilityView;
        event Func<CancellationToken, Task>? ShowContainerView;
       
        void SetActivePage(NavPage page);
        void BeginWindowDrag();
        void ShowBusy(string? text = null);
        void HideBusy();
        Task RunBusyAsync(Func<CancellationToken, Task> action, CancellationToken ct, string? text = null);
    }
}

