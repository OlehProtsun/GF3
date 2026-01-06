using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace WinFormsApp.Presenter.Shop
{
    public partial class ShopPresenter
    {
        private ShopModel? CurrentShop() =>
            _bindingSource.Current as ShopModel;

        private ShopModel BuildModelFromView() =>
            new()
            {
                Id = _view.Id,
                Name = _view.Name,
                Address = _view.Address,
                Description = _view.Description
            };
    }
}
