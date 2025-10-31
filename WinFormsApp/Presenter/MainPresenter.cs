using BusinessLogicLayer.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinFormsApp.View.Employee;
using WinFormsApp.View.Main;

namespace WinFormsApp.Presenter
{
    public class MainPresenter
    {
        private readonly IMainView _mainView;
        private readonly IServiceProvider _sp;

        private EmployeeView? _employeeView; // один екземпляр на життєвий цикл presenter-а

        public MainPresenter(IMainView mainView, IServiceProvider sp)
        {
            _mainView = mainView;
            _sp = sp;

            _mainView.ShowEmployeeView += OnShowEmployeeViewAsync;
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
