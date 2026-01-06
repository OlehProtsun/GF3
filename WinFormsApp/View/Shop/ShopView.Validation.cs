using System;
using System.Collections.Generic;
using System.Text;

namespace WinFormsApp.View.Shop
{
    public partial class ShopView
    {
        public void ClearInputs()
        {
            Id = 0;
            Name = string.Empty;
            Address = string.Empty;
            Description = string.Empty;
        }

        public void ClearValidationErrors()
        {
            errorProvider.SetError(inputFirstName, "");
            errorProvider.SetError(inputLastName, "");
            errorProvider.SetError(inputEmail, "");
        }

        public void SetValidationErrors(IReadOnlyDictionary<string, string> errors)
        {
            ClearValidationErrors();

            foreach (var kv in errors)
            {
                switch (kv.Key)
                {
                    case nameof(Name): errorProvider.SetError(inputFirstName, kv.Value); break;
                    case nameof(Address): errorProvider.SetError(inputLastName, kv.Value); break;
                    case nameof(Description): errorProvider.SetError(inputEmail, kv.Value); break;
                }
            }
        }
    }
}
