using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace WinFormsApp.Presenter.Employee
{
    public partial class EmployeePresenter
    {
        private EmployeeModel? CurrentEmployee() =>
            _bindingSource.Current as EmployeeModel;

        private EmployeeModel BuildModelFromView() =>
            new()
            {
                Id = _view.Id,
                FirstName = _view.FirstName,
                LastName = _view.LastName,
                Email = _view.Email,
                Phone = _view.Phone
            };
    }
}
