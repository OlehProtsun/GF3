using System;
using System.Collections.Generic;
using System.Text;

namespace WinFormsApp.View.Employee
{
    public partial class EmployeeView
    {
        public void ClearInputs()
        {
            Id = 0;
            FirstName = string.Empty;
            LastName = string.Empty;
            Email = string.Empty;
            Phone = string.Empty;
        }

        public void ClearValidationErrors()
        {
            errorProvider.SetError(inputFirstName, "");
            errorProvider.SetError(inputLastName, "");
            errorProvider.SetError(inputEmail, "");
            errorProvider.SetError(inputPhone, "");
        }

        public void SetValidationErrors(IReadOnlyDictionary<string, string> errors)
        {
            ClearValidationErrors();

            foreach (var kv in errors)
            {
                switch (kv.Key)
                {
                    case nameof(FirstName): errorProvider.SetError(inputFirstName, kv.Value); break;
                    case nameof(LastName): errorProvider.SetError(inputLastName, kv.Value); break;
                    case nameof(Email): errorProvider.SetError(inputEmail, kv.Value); break;
                    case nameof(Phone): errorProvider.SetError(inputPhone, kv.Value); break;
                }
            }
        }
    }
}
