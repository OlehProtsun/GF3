using BusinessLogicLayer.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinFormsApp.View.Availability;
using WinFormsApp.View.Employee;
using WinFormsApp.View.Main;

namespace WinFormsApp.Presenter
{
    public class MainPresenter
    {
        private readonly IMainView _mainView;
        private readonly IServiceProvider _sp;

        private EmployeeView? _employeeView; // один екземпляр на життєвий цикл presenter-а
        private AvailabilityView? _availabilityView;

        public MainPresenter(IMainView mainView, IServiceProvider sp)
        {
            _mainView = mainView;
            _sp = sp;

            _mainView.ShowEmployeeView += OnShowEmployeeViewAsync;
            _mainView.ShowAvailabilityView += OnShowAvailabilityViewAsync;
        }

        private Task OnShowAvailabilityViewAsync(CancellationToken token)
        {
            if (_availabilityView == null || _availabilityView.IsDisposed)
            {
                _availabilityView = _sp.GetRequiredService<AvailabilityView>();
                if (_mainView is Form mdiParent)
                {
                    _availabilityView.MdiParent = mdiParent;
                    _availabilityView.Dock = DockStyle.Fill;
                }
            }

            if (_availabilityView.WindowState == FormWindowState.Minimized)
                _availabilityView.WindowState = FormWindowState.Normal;

            _availabilityView.BringToFront();
            _availabilityView.Show();
            return Task.CompletedTask;
        }

        private Task OnShowEmployeeViewAsync(CancellationToken ct)
        {
            // колишній вміст ShowEmployeeView(object?, EventArgs)
            if (_employeeView == null || _employeeView.IsDisposed)
            {
                _employeeView = _sp.GetRequiredService<EmployeeView>();
                if (_mainView is Form mdiParent)
                {
                    _employeeView.MdiParent = mdiParent;
                    _employeeView.Dock = DockStyle.Fill;
                }
            }

            if (_employeeView.WindowState == FormWindowState.Minimized)
                _employeeView.WindowState = FormWindowState.Normal;

            _employeeView.BringToFront();
            _employeeView.Show();
            return Task.CompletedTask;
        }
    }
}
