using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsApp.View.Main
{
    public interface IMainView
    {
        void BeginWindowDrag();

        event Func<CancellationToken, Task>? ShowEmployeeView;
        event Func<CancellationToken, Task>? ShowAvailabilityView;
        event Func<CancellationToken, Task>? ShowShopView;
        event Func<CancellationToken, Task>? ShowContainerView;

    }
}
