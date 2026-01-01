using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp.View.Employee;
using WinFormsApp.ViewModel;


namespace WinFormsApp.Presenter.Employee
{
    public partial class EmployeePresenter
    {
        public EmployeePresenter(IEmployeeView view, IEmployeeService service)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _service = service ?? throw new ArgumentNullException(nameof(service));

            _ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

            _view.SearchEvent += OnSearchEventAsync;
            _view.AddEvent += OnAddEventAsync;
            _view.EditEvent += OnEditEventAsync;
            _view.DeleteEvent += OnDeleteEventAsync;
            _view.SaveEvent += OnSaveEventAsync;
            _view.CancelEvent += OnCancelEventAsync;
            _view.OpenProfileEvent += OnOpenProfileAsync;

            _view.SetEmployeeListBindingSource(_bindingSource);
        }

        public Task InitializeAsync() =>
            LoadEmployeesAsync(ct => _service.GetAllAsync(ct), CancellationToken.None, selectId: null);
    }
}
