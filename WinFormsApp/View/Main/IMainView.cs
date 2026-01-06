using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinFormsApp.ViewModel;

namespace WinFormsApp.View.Main
{
    public interface IMainView : WinFormsApp.View.Shared.IBusyView
    {
        NavPage ActivePage { get; }

        Control ContentHost { get; }

        event Func<CancellationToken, Task>? ShowEmployeeView;
        event Func<CancellationToken, Task>? ShowShopView;
        event Func<CancellationToken, Task>? ShowAvailabilityView;
        event Func<CancellationToken, Task>? ShowContainerView;
       
        void SetActivePage(NavPage page);
        void BeginWindowDrag();
    }
}
