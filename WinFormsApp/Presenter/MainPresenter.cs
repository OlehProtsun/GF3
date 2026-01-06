using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp.View.Main;
using WinFormsApp.ViewModel;

namespace WinFormsApp.Presenter
{
    public class MainPresenter
    {
        private readonly IMainView _mainView;
        private readonly IMdiViewFactory _viewFactory;

        private Form? _employeeView;
        private Form? _shopView;
        private Form? _availabilityView;
        private Form? _containerView;

        public MainPresenter(IMainView mainView, IMdiViewFactory viewFactory)
        {
            _mainView = mainView;
            _viewFactory = viewFactory;

            _mainView.ShowEmployeeView += ct =>
                NavigateAsync(ct, () => _employeeView, v => _employeeView = v, _viewFactory.CreateEmployeeView, NavPage.Employee, "Opening Employee...");

            _mainView.ShowShopView += ct =>
                NavigateAsync(ct, () => _shopView, v => _shopView = v, _viewFactory.CreateShopView, NavPage.Shop, "Opening Shop...");

            _mainView.ShowAvailabilityView += ct =>
                NavigateAsync(ct, () => _availabilityView, v => _availabilityView = v, _viewFactory.CreateAvailabilityView, NavPage.Availability, "Opening Availability...");

            _mainView.ShowContainerView += ct =>
                NavigateAsync(ct, () => _containerView, v => _containerView = v, _viewFactory.CreateContainerView, NavPage.Container, "Opening Container...");
        }

        private Task NavigateAsync(
            CancellationToken ct,
            Func<Form?> getView,
            Action<Form> setView,
            Func<Form> createView,
            NavPage page,
            string busyText)
        {
            return _mainView.RunBusyAsync(innerCt =>
            {
                _mainView.SetActivePage(page);

                var view = getView();
                view = ShowInPanel(getOrCreate: () => view, createView, setView, innerCt);

                // на випадок, якщо створили нову — зафіксуємо
                setView(view);

                return Task.CompletedTask;
            }, ct, busyText);
        }


        private Form ShowInPanel(
            Func<Form?> getOrCreate,
            Func<Form> createView,
            Action<Form> set,
            CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
                return getOrCreate() ?? throw new OperationCanceledException(ct);

            var view = getOrCreate();
            if (view is null || view.IsDisposed)
            {
                view = createView();

                // КЛЮЧОВЕ: вбудовуємо Form як Control у Panel
                view.TopLevel = false;
                view.FormBorderStyle = FormBorderStyle.None;
                view.Dock = DockStyle.Fill;

                _mainView.ContentHost.Controls.Add(view);
                set(view);
            }

            // Сховати всі інші форми в хості (щоб не нашаровувались)
            foreach (Control c in _mainView.ContentHost.Controls)
                if (c is Form f && f != view) f.Hide();

            view.Show();
            view.BringToFront();

            return view;
        }

    }

}
