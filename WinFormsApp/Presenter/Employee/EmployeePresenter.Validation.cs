using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace WinFormsApp.Presenter.Employee
{
    public partial class EmployeePresenter
    {
        private static Dictionary<string, string> Validate(EmployeeModel m)
        {
            var map = new Dictionary<string, string>(capacity: 4);

            if (string.IsNullOrWhiteSpace(m.FirstName))
                map[nameof(m.FirstName)] = "First name is required.";

            if (string.IsNullOrWhiteSpace(m.LastName))
                map[nameof(m.LastName)] = "Last name is required.";

            if (!string.IsNullOrWhiteSpace(m.Email) && !EmailRegex.IsMatch(m.Email))
                map[nameof(m.Email)] = "Invalid email format.";

            if (!string.IsNullOrWhiteSpace(m.Phone) && !PhoneRegex.IsMatch(m.Phone))
                map[nameof(m.Phone)] = "Invalid phone number.";

            return map;
        }
    }
}
