using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace WinFormsApp.Presenter.Shop
{
    public partial class ShopPresenter
    {
        private static Dictionary<string, string> Validate(ShopModel m)
        {
            var map = new Dictionary<string, string>(capacity: 4);

            if (string.IsNullOrWhiteSpace(m.Name))
                map[nameof(m.Name)] = "Name is required.";

            if (string.IsNullOrWhiteSpace(m.Address))
                map[nameof(m.Address)] = "Address is required.";

            return map;
        }
    }
}
