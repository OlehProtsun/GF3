using BusinessLogicLayer.Services.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using WinFormsApp.View.Employee;

namespace WinFormsApp.Presenter.Employee
{
    public partial class EmployeePresenter
    {
        private readonly IEmployeeView _view;
        private readonly IEmployeeService _service;
        private readonly BindingSource _bindingSource = new();

        private readonly SynchronizationContext _ui;

        // анти-гонки запитів (пошук/рефреш списку)
        private CancellationTokenSource? _listOpCts;
        private int _listOpVersion;

        private static readonly Regex EmailRegex =
            new(@"^\S+@\S+\.\S+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex PhoneRegex =
            new(@"^[0-9+\-\s()]{5,}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }
}
