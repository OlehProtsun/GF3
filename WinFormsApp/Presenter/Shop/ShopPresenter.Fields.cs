using BusinessLogicLayer.Services.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using WinFormsApp.View.Shop;

namespace WinFormsApp.Presenter.Shop
{
    public partial class ShopPresenter
    {
        private readonly IShopView _view;
        private readonly IShopService _service;
        private readonly BindingSource _bindingSource = new();

        private readonly SynchronizationContext _ui;

        // анти-гонки запитів (пошук/рефреш списку)
        private CancellationTokenSource? _listOpCts;
        private int _listOpVersion;

    }
}
