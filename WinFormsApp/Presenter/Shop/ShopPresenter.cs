using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp.View.Shop;
using WinFormsApp.ViewModel;


namespace WinFormsApp.Presenter.Shop
{
    public partial class ShopPresenter
    {
        public ShopPresenter(IShopView view, IShopService service)
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

            _view.SetShopListBindingSource(_bindingSource);
        }

        public Task InitializeAsync() =>
            _view.RunBusyAsync(
                ct => LoadShopsAsync(innerCt => _service.GetAllAsync(innerCt), ct, selectId: null),
                _view.LifetimeToken,
                "Loading shops...");
    }
}
