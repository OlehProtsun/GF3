using BusinessLogicLayer.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp.View.Availability;
using WinFormsApp.View.Container;
using WinFormsApp.View.Employee;
using WinFormsApp.View.Main;
using WinFormsApp.ViewModel;

namespace WinFormsApp.Presenter
{
    public class MainPresenter
    {
        private readonly IMainView _mainView;
        private readonly IServiceProvider _sp;

        private EmployeeView? _employeeView;
        private AvailabilityView? _availabilityView;
        private ContainerView? _containerView;

        public MainPresenter(IMainView mainView, IServiceProvider sp)
        {
            _mainView = mainView;
            _sp = sp;

            _mainView.ShowEmployeeView += ct =>
                NavigateAsync(ct, () => _employeeView, v => _employeeView = v, NavPage.Employee, "Opening Employee...");

            _mainView.ShowAvailabilityView += ct =>
                NavigateAsync(ct, () => _availabilityView, v => _availabilityView = v, NavPage.Availability, "Opening Availability...");

            _mainView.ShowContainerView += ct =>
                NavigateAsync(ct, () => _containerView, v => _containerView = v, NavPage.Container, "Opening Container...");
        }

        private Task NavigateAsync<TView>(
            CancellationToken ct,
            Func<TView?> getView,
            Action<TView> setView,
            NavPage page,
            string busyText)
            where TView : Form
        {
            return _mainView.RunBusyAsync(innerCt =>
            {
                _mainView.SetActivePage(page);

                var view = getView();
                view = ShowMdi(getOrCreate: () => view, set: setView, innerCt);

                // на випадок, якщо створили нову — зафіксуємо
                setView(view);

                return Task.CompletedTask;
            }, ct, busyText);
        }


        private TView ShowMdi<TView>(Func<TView?> getOrCreate, Action<TView> set, CancellationToken ct)
            where TView : Form
        {
            if (ct.IsCancellationRequested)
                return getOrCreate() ?? throw new OperationCanceledException(ct);

            var view = getOrCreate();
            if (view is null || view.IsDisposed)
            {
                view = _sp.GetRequiredService<TView>();

                if (_mainView is Form mdiParent)
                {
                    view.MdiParent = mdiParent;
                    view.Dock = DockStyle.Fill;
                }

                set(view);
            }

            if (view.WindowState == FormWindowState.Minimized)
                view.WindowState = FormWindowState.Normal;

            view.BringToFront();
            view.Show();

            return view;
        }
    }

}
