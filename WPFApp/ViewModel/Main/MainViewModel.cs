/*
  Опис файлу: цей модуль містить реалізацію компонента MainViewModel у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WPFApp.ViewModel.Availability.Main;
using WPFApp.ViewModel.Container.Edit;
using WPFApp.ViewModel.Employee;
using WPFApp.ViewModel.Shop;
using WPFApp.ViewModel.Database;
using WPFApp.ViewModel.Home;
using WPFApp.ViewModel.Information;
using WPFApp.MVVM.Core;
using WPFApp.MVVM.Commands;

namespace WPFApp.ViewModel.Main
{
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class MainViewModel : ObservableObject, IDisposable` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class MainViewModel : ObservableObject, IDisposable
    {
        
        
        

        
        
        
        private readonly IServiceProvider _sp;

        
        
        

        
        
        
        
        private readonly CancellationTokenSource _lifetimeCts = new();

        
        
        
        
        private CancellationTokenSource? _navOpCts;

        
        
        
        
        private readonly SemaphoreSlim _navGate = new(1, 1);

        
        
        

        
        
        
        private HomeViewModel? _homeVm;
        private EmployeeViewModel? _employeeVm;
        private ShopViewModel? _shopVm;
        private AvailabilityViewModel? _availabilityVm;
        private ContainerViewModel? _containerVm;
        private InformationViewModel? _informationVm;
        private DatabaseViewModel? _databaseVm;

        
        
        

        private NavPage _activePage = NavPage.None;

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public NavPage ActivePage` та контракт його використання у шарі WPFApp.
        /// </summary>
        public NavPage ActivePage
        {
            get => _activePage;
            private set
            {
                
                if (SetProperty(ref _activePage, value))
                {
                    
                    
                    
                    RaiseNavStateProperties();
                    RaiseNavCanExecute();
                }
            }
        }

        private object? _currentViewModel;

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public object? CurrentViewModel` та контракт його використання у шарі WPFApp.
        /// </summary>
        public object? CurrentViewModel
        {
            get => _currentViewModel;
            private set => SetProperty(ref _currentViewModel, value);
        }

        private bool _isBusy;

        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public bool IsBusy` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                
                if (SetProperty(ref _isBusy, value))
                {
                    
                    RaiseNavStateProperties();

                    
                    RaiseNavCanExecute();
                }
            }
        }

        private string? _busyText;

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public string? BusyText` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string? BusyText
        {
            get => _busyText;
            private set => SetProperty(ref _busyText, value);
        }

        
        
        

        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public bool IsHomeEnabled => !IsBusy && ActivePage != NavPage.Home;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool IsHomeEnabled => !IsBusy && ActivePage != NavPage.Home;

        /// <summary>
        /// Визначає публічний елемент `public bool IsEmployeeEnabled => !IsBusy && ActivePage != NavPage.Employee;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool IsEmployeeEnabled => !IsBusy && ActivePage != NavPage.Employee;

        /// <summary>
        /// Визначає публічний елемент `public bool IsShopEnabled => !IsBusy && ActivePage != NavPage.Shop;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool IsShopEnabled => !IsBusy && ActivePage != NavPage.Shop;

        /// <summary>
        /// Визначає публічний елемент `public bool IsAvailabilityEnabled => !IsBusy && ActivePage != NavPage.Availability;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool IsAvailabilityEnabled => !IsBusy && ActivePage != NavPage.Availability;

        /// <summary>
        /// Визначає публічний елемент `public bool IsContainerEnabled => !IsBusy && ActivePage != NavPage.Container;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool IsContainerEnabled => !IsBusy && ActivePage != NavPage.Container;

        /// <summary>
        /// Визначає публічний елемент `public bool IsInformationEnabled => !IsBusy && ActivePage != NavPage.Information;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool IsInformationEnabled => !IsBusy && ActivePage != NavPage.Information;

        /// <summary>
        /// Визначає публічний елемент `public bool IsDatabaseEnabled => !IsBusy && ActivePage != NavPage.Database;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool IsDatabaseEnabled => !IsBusy && ActivePage != NavPage.Database;

        
        
        

        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand ShowHomeCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand ShowHomeCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand ShowEmployeeCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand ShowEmployeeCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand ShowShopCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand ShowShopCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand ShowAvailabilityCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand ShowAvailabilityCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand ShowContainerCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand ShowContainerCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand ShowInformationCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand ShowInformationCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand ShowDatabaseCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand ShowDatabaseCommand { get; }

        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand CloseCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand CloseCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand MinimizeCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand MinimizeCommand { get; }

        
        
        
        private readonly AsyncRelayCommand[] _navCommands;

        
        
        

        /// <summary>
        /// Визначає публічний елемент `public MainViewModel(IServiceProvider sp)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public MainViewModel(IServiceProvider sp)
        {
            
            _sp = sp ?? throw new ArgumentNullException(nameof(sp));

            
            ShowHomeCommand = CreateNavCommand(
                page: NavPage.Home,
                busyText: "Opening Home...",
                getOrCreateVm: () => _homeVm ??= _sp.GetRequiredService<HomeViewModel>(),
                isEnabled: () => IsHomeEnabled);

            ShowEmployeeCommand = CreateNavCommand(
                page: NavPage.Employee,
                busyText: "Opening Employee...",
                getOrCreateVm: () => _employeeVm ??= _sp.GetRequiredService<EmployeeViewModel>(),
                isEnabled: () => IsEmployeeEnabled);

            ShowShopCommand = CreateNavCommand(
                page: NavPage.Shop,
                busyText: "Opening Shop...",
                getOrCreateVm: () => _shopVm ??= _sp.GetRequiredService<ShopViewModel>(),
                isEnabled: () => IsShopEnabled);

            ShowAvailabilityCommand = CreateNavCommand(
                page: NavPage.Availability,
                busyText: "Opening Availability...",
                getOrCreateVm: () => _availabilityVm ??= _sp.GetRequiredService<AvailabilityViewModel>(),
                isEnabled: () => IsAvailabilityEnabled);

            ShowContainerCommand = CreateNavCommand(
                page: NavPage.Container,
                busyText: "Opening Container...",
                getOrCreateVm: () => _containerVm ??= _sp.GetRequiredService<ContainerViewModel>(),
                isEnabled: () => IsContainerEnabled);

            ShowInformationCommand = CreateNavCommand(
                page: NavPage.Information,
                busyText: "Opening Information...",
                getOrCreateVm: () => _informationVm ??= _sp.GetRequiredService<InformationViewModel>(),
                isEnabled: () => IsInformationEnabled);

            ShowDatabaseCommand = CreateNavCommand(
                page: NavPage.Database,
                busyText: "Opening Database...",
                getOrCreateVm: () => _databaseVm ??= _sp.GetRequiredService<DatabaseViewModel>(),
                isEnabled: () => IsDatabaseEnabled);

            
            _navCommands = new[]
            {
                ShowHomeCommand,
                ShowEmployeeCommand,
                ShowShopCommand,
                ShowAvailabilityCommand,
                ShowContainerCommand,
                ShowInformationCommand,
                ShowDatabaseCommand
            };

            
            
            CloseCommand = new AsyncRelayCommand(() =>
            {
                Application.Current?.Shutdown();
                return Task.CompletedTask;
            });

            
            MinimizeCommand = new AsyncRelayCommand(() =>
            {
                var wnd = Application.Current?.MainWindow;
                if (wnd != null)
                    wnd.WindowState = WindowState.Minimized;

                return Task.CompletedTask;
            });
        }

        
        
        
        
        
        
        
        private AsyncRelayCommand CreateNavCommand(
            NavPage page,
            string busyText,
            Func<object> getOrCreateVm,
            Func<bool> isEnabled)
        {
            
            
            return new AsyncRelayCommand(
                execute: () => NavigateAsync(page, busyText, getOrCreateVm),
                canExecute: isEnabled);
        }

        
        
        

        
        
        
        
        
        
        
        
        private async Task NavigateAsync(NavPage page, string busyText, Func<object> getOrCreateVm)
        {
            
            var lifetimeToken = _lifetimeCts.Token;

            
            await _navGate.WaitAsync(lifetimeToken).ConfigureAwait(false);

            try
            {
                
                
                _navOpCts?.Cancel();
                _navOpCts?.Dispose();

                
                
                
                _navOpCts = CancellationTokenSource.CreateLinkedTokenSource(lifetimeToken);

                
                var navToken = _navOpCts.Token;

                
                await RunBusyAsync(async () =>
                {
                    
                    navToken.ThrowIfCancellationRequested();

                    
                    var viewModel = getOrCreateVm();

                    
                    
                    
                    await EnsureInitializedIfNeededAsync(viewModel, navToken).ConfigureAwait(false);

                    
                    navToken.ThrowIfCancellationRequested();

                    
                    
                    
                    
                    ActivePage = page;
                    CurrentViewModel = viewModel;

                }, busyText, navToken).ConfigureAwait(false);
            }
            finally
            {
                
                _navGate.Release();
            }
        }

        
        
        
        
        
        
        
        private static Task EnsureInitializedIfNeededAsync(object viewModel, CancellationToken ct)
        {
            
            
            return viewModel switch
            {
                AvailabilityViewModel availabilityVm => availabilityVm.EnsureInitializedAsync(ct),
                HomeViewModel homeVm => homeVm.EnsureInitializedAsync(ct),
                EmployeeViewModel employeeVm => employeeVm.EnsureInitializedAsync(ct),
                ShopViewModel shopVm => shopVm.EnsureInitializedAsync(ct),
                ContainerViewModel containerVm => containerVm.EnsureInitializedAsync(ct),
                InformationViewModel _ => Task.CompletedTask,
                DatabaseViewModel _ => Task.CompletedTask,
                _ => Task.CompletedTask
            };
        }

        
        
        

        
        
        
        
        
        
        
        
        private async Task RunBusyAsync(Func<Task> action, string? text, CancellationToken ct)
        {
            
            IsBusy = true;

            
            BusyText = text;

            try
            {
                
                await action().ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                
                
            }
            finally
            {
                
                IsBusy = false;

                
                BusyText = null;
            }
        }

        
        
        

        
        
        
        
        private void RaiseNavStateProperties()
        {
            Raise(nameof(IsHomeEnabled));
            Raise(nameof(IsEmployeeEnabled));
            Raise(nameof(IsShopEnabled));
            Raise(nameof(IsAvailabilityEnabled));
            Raise(nameof(IsContainerEnabled));
            Raise(nameof(IsInformationEnabled));
            Raise(nameof(IsDatabaseEnabled));
        }

        
        
        
        
        private void RaiseNavCanExecute()
        {
            
            for (int i = 0; i < _navCommands.Length; i++)
                _navCommands[i].RaiseCanExecuteChanged();
        }

        
        
        

        /// <summary>
        /// Визначає публічний елемент `public void Dispose()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void Dispose()
        {
            
            try { _lifetimeCts.Cancel(); } catch {  }

            
            try { _navOpCts?.Cancel(); } catch {  }

            
            _navOpCts?.Dispose();
            _lifetimeCts.Dispose();

            
            _navGate.Dispose();
        }
    }
}
