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
        private Form? _availabilityView;
        private Form? _containerView;

        public MainPresenter(IMainView mainView, IMdiViewFactory viewFactory)
        {
            _mainView = mainView;
            _viewFactory = viewFactory;

            _mainView.ShowEmployeeView += ct =>
                NavigateAsync(ct, () => _employeeView, v => _employeeView = v, _viewFactory.CreateEmployeeView, NavPage.Employee, "Opening Employee...");

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
                view = ShowMdi(getOrCreate: () => view, createView, setView, innerCt);

                // на випадок, якщо створили нову — зафіксуємо
                setView(view);

                return Task.CompletedTask;
            }, ct, busyText);
        }


        private Form ShowMdi(Func<Form?> getOrCreate, Func<Form> createView, Action<Form> set, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
                return getOrCreate() ?? throw new OperationCanceledException(ct);

            var view = getOrCreate();
            if (view is null || view.IsDisposed)
            {
                view = createView();

                if (_mainView is Form mdiParent)
                {
                    view.MdiParent = mdiParent;
                    view.Dock = DockStyle.Fill;
                }

                set(view);
            }

            if (view.WindowState == FormWindowState.Minimized)
                view.WindowState = FormWindowState.Normal;

            view.BringToFront();
            view.Show();

            return view;
        }
    }

}
