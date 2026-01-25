using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WPFApp.Infrastructure;
using WPFApp.ViewModel.Availability;
using WPFApp.ViewModel.Container.Edit;
using WPFApp.ViewModel.Employee;
using WPFApp.ViewModel.Shop;

namespace WPFApp.ViewModel
{
    public sealed class MainViewModel : ObservableObject, IDisposable
    {
        private readonly IServiceProvider _sp;
        private readonly CancellationTokenSource _navCts = new();

        private EmployeeViewModel? _employeeVm;
        private ShopViewModel? _shopVm;
        private AvailabilityViewModel? _availabilityVm;
        private ContainerViewModel? _containerVm;

        private NavPage _activePage = NavPage.None;
        public NavPage ActivePage
        {
            get => _activePage;
            private set
            {
                if (SetProperty(ref _activePage, value))
                {
                    Raise(nameof(IsEmployeeEnabled));
                    Raise(nameof(IsShopEnabled));
                    Raise(nameof(IsAvailabilityEnabled));
                    Raise(nameof(IsContainerEnabled));
                }
            }
        }

        private object? _currentViewModel;
        public object? CurrentViewModel
        {
            get => _currentViewModel;
            private set => SetProperty(ref _currentViewModel, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    Raise(nameof(IsEmployeeEnabled));
                    Raise(nameof(IsShopEnabled));
                    Raise(nameof(IsAvailabilityEnabled));
                    Raise(nameof(IsContainerEnabled));
                    RaiseNavCanExecute();
                }
            }
        }

        private string? _busyText;
        public string? BusyText
        {
            get => _busyText;
            private set => SetProperty(ref _busyText, value);
        }

        public bool IsEmployeeEnabled => !IsBusy && ActivePage != NavPage.Employee;
        public bool IsShopEnabled => !IsBusy && ActivePage != NavPage.Shop;
        public bool IsAvailabilityEnabled => !IsBusy && ActivePage != NavPage.Availability;
        public bool IsContainerEnabled => !IsBusy && ActivePage != NavPage.Container;

        public AsyncRelayCommand ShowEmployeeCommand { get; }
        public AsyncRelayCommand ShowShopCommand { get; }
        public AsyncRelayCommand ShowAvailabilityCommand { get; }
        public AsyncRelayCommand ShowContainerCommand { get; }

        public AsyncRelayCommand CloseCommand { get; }
        public AsyncRelayCommand MinimizeCommand { get; }

        public MainViewModel(IServiceProvider sp)
        {
            _sp = sp;

            ShowEmployeeCommand = new AsyncRelayCommand(
                () => NavigateAsync(NavPage.Employee, "Opening Employee...", () => _employeeVm ??= _sp.GetRequiredService<EmployeeViewModel>()),
                () => IsEmployeeEnabled);

            ShowShopCommand = new AsyncRelayCommand(
                () => NavigateAsync(NavPage.Shop, "Opening Shop...", () => _shopVm ??= _sp.GetRequiredService<ShopViewModel>()),
                () => IsShopEnabled);

            ShowAvailabilityCommand = new AsyncRelayCommand(
                () => NavigateAsync(NavPage.Availability, "Opening Availability...", () => _availabilityVm ??= _sp.GetRequiredService<AvailabilityViewModel>()),
                () => IsAvailabilityEnabled);

            ShowContainerCommand = new AsyncRelayCommand(
                () => NavigateAsync(NavPage.Container, "Opening Container...", () => _containerVm ??= _sp.GetRequiredService<ContainerViewModel>()),
                () => IsContainerEnabled);

            CloseCommand = new AsyncRelayCommand(() =>
            {
                Application.Current.Shutdown();
                return Task.CompletedTask;
            });

            MinimizeCommand = new AsyncRelayCommand(() =>
            {
                if (Application.Current.MainWindow != null)
                    Application.Current.MainWindow.WindowState = WindowState.Minimized;
                return Task.CompletedTask;
            });
        }

        private async Task NavigateAsync(NavPage page, string busyText, Func<object> getOrCreateVm)
        {
            var ct = _navCts.Token;

            await RunBusyAsync(async () =>
            {
                ct.ThrowIfCancellationRequested();

                ActivePage = page;
                var viewModel = getOrCreateVm();
                CurrentViewModel = viewModel;

                switch (viewModel)
                {
                    case AvailabilityViewModel availabilityVm:
                        await availabilityVm.EnsureInitializedAsync(ct);
                        break;
                    case EmployeeViewModel employeeVm:
                        await employeeVm.EnsureInitializedAsync(ct);
                        break;
                    case ShopViewModel shopVm:
                        await shopVm.EnsureInitializedAsync(ct);
                        break;
                    case ContainerViewModel containerVm:
                        await containerVm.EnsureInitializedAsync(ct);
                        break;
                }
            }, busyText);
        }

        private async Task RunBusyAsync(Func<Task> action, string? text)
        {
            IsBusy = true;
            BusyText = text;
            try
            {
                await action();
            }
            finally
            {
                IsBusy = false;
                BusyText = null;
            }
        }

        private void RaiseNavCanExecute()
        {
            ShowEmployeeCommand.RaiseCanExecuteChanged();
            ShowShopCommand.RaiseCanExecuteChanged();
            ShowAvailabilityCommand.RaiseCanExecuteChanged();
            ShowContainerCommand.RaiseCanExecuteChanged();
        }

        public void Dispose()
        {
            try { _navCts.Cancel(); } catch { }
            _navCts.Dispose();
        }
    }
}
