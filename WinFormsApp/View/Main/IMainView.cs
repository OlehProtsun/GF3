using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsApp.View.Main
{
    public interface IMainView
    {
        event Func<CancellationToken, Task>? ShowEmployeeView;
        event Func<CancellationToken, Task>? ShowAvailabilityView;
    }
}
