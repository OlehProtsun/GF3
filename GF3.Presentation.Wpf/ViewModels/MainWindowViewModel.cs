using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace GF3.Presentation.Wpf.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IServiceProvider _serviceProvider;
        private ViewModelBase? _currentPageViewModel;
        private NavPage _activePage;
        private bool _isBusy;
        private string _busyText = "Loading...";

        public MainWindowViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            ShowEmployeeCommand = new RelayCommand(() => NavigateTo(NavPage.Employee));
            ShowShopCommand = new RelayCommand(() => NavigateTo(NavPage.Shop));
            ShowAvailabilityCommand = new RelayCommand(() => NavigateTo(NavPage.Availability));
            ShowContainerCommand = new RelayCommand(() => NavigateTo(NavPage.Container));
            CloseCommand = new RelayCommand(() => Application.Current.MainWindow?.Close());
            MinimizeCommand = new RelayCommand(() => Application.Current.MainWindow!.WindowState = WindowState.Minimized);

            NavigateTo(NavPage.Employee);
        }

        public RelayCommand ShowEmployeeCommand { get; }
        public RelayCommand ShowShopCommand { get; }
        public RelayCommand ShowAvailabilityCommand { get; }
        public RelayCommand ShowContainerCommand { get; }
        public RelayCommand CloseCommand { get; }
        public RelayCommand MinimizeCommand { get; }

        public ViewModelBase? CurrentPageViewModel
        {
            get => _currentPageViewModel;
            private set
            {
                _currentPageViewModel = value;
                RaisePropertyChanged();
            }
        }

        public NavPage ActivePage
        {
            get => _activePage;
            private set
            {
                _activePage = value;
                RaisePropertyChanged();
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                _isBusy = value;
                RaisePropertyChanged();
            }
        }

        public string BusyText
        {
            get => _busyText;
            private set
            {
                _busyText = value;
                RaisePropertyChanged();
            }
        }

        private void NavigateTo(NavPage page)
        {
            ActivePage = page;
            CurrentPageViewModel = page switch
            {
                NavPage.Employee => _serviceProvider.GetRequiredService<EmployeePageViewModel>(),
                NavPage.Shop => _serviceProvider.GetRequiredService<ShopPageViewModel>(),
                NavPage.Availability => _serviceProvider.GetRequiredService<AvailabilityPageViewModel>(),
                NavPage.Container => _serviceProvider.GetRequiredService<ContainerPageViewModel>(),
                _ => null
            };
        }
    }
}
