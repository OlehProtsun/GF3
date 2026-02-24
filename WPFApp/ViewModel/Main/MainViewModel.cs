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
    /// MainViewModel — центральний “shell” VM, який:
    /// - тримає CurrentViewModel (поточний модуль: Employee/Shop/Availability/Container)
    /// - керує навігацією між модулями
    /// - показує Busy-стан під час ініціалізації модулів
    ///
    /// Оптимізаційні принципи:
    /// 1) Навігація має бути серіалізованою (не допускаємо паралельні переходи).
    /// 2) Новий перехід має скасовувати попередній (якщо той ще виконується).
    /// 3) IsBusy / ActivePage — єдине джерело правди для enabled-станів і CanExecute.
    /// 4) Мінімізуємо дублювання (Raise(...) і RaiseCanExecuteChanged()).
    /// </summary>
    public sealed class MainViewModel : ObservableObject, IDisposable
    {
        // =========================================================
        // 1) DI контейнер
        // =========================================================

        /// <summary>
        /// IServiceProvider — DI контейнер, з якого ми “ліниво” дістаємо модулі.
        /// </summary>
        private readonly IServiceProvider _sp;

        // =========================================================
        // 2) Lifetime / cancellation
        // =========================================================

        /// <summary>
        /// Lifetime CTS — скасовується при Dispose().
        /// Усі довгі операції мають бути прив’язані до цього токена.
        /// </summary>
        private readonly CancellationTokenSource _lifetimeCts = new();

        /// <summary>
        /// CTS поточної навігаційної операції.
        /// При запуску НОВОЇ навігації ми скасовуємо попередню.
        /// </summary>
        private CancellationTokenSource? _navOpCts;

        /// <summary>
        /// Навігаційний “gate”, щоб не було паралельних NavigateAsync.
        /// Навіть якщо хтось викличе команди програмно або натисне дуже швидко.
        /// </summary>
        private readonly SemaphoreSlim _navGate = new(1, 1);

        // =========================================================
        // 3) Кеш модулів (лінивий)
        // =========================================================

        // Ми тримаємо посилання, щоб:
        // - не створювати модулі повторно
        // - зберігати їх стан (List selections, Edit state, etc.)
        private HomeViewModel? _homeVm;
        private EmployeeViewModel? _employeeVm;
        private ShopViewModel? _shopVm;
        private AvailabilityViewModel? _availabilityVm;
        private ContainerViewModel? _containerVm;
        private InformationViewModel? _informationVm;
        private DatabaseViewModel? _databaseVm;

        // =========================================================
        // 4) Стан навігації
        // =========================================================

        private NavPage _activePage = NavPage.None;

        /// <summary>
        /// ActivePage — поточна активна сторінка (enum).
        /// Від нього залежать enabled-стани кнопок навігації.
        /// </summary>
        public NavPage ActivePage
        {
            get => _activePage;
            private set
            {
                // 1) SetProperty повертає true лише якщо значення реально змінилось.
                if (SetProperty(ref _activePage, value))
                {
                    // 2) При зміні сторінки:
                    //    - перераховуємо Is*Enabled властивості
                    //    - оновлюємо CanExecute команд навігації
                    RaiseNavStateProperties();
                    RaiseNavCanExecute();
                }
            }
        }

        private object? _currentViewModel;

        /// <summary>
        /// CurrentViewModel — об’єкт VM активного модуля.
        /// У XAML зазвичай є ContentControl + DataTemplates по типах.
        /// </summary>
        public object? CurrentViewModel
        {
            get => _currentViewModel;
            private set => SetProperty(ref _currentViewModel, value);
        }

        private bool _isBusy;

        /// <summary>
        /// IsBusy — коли true:
        /// - навігаційні кнопки відключені
        /// - може показуватися overlay/loader
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                // 1) Якщо значення змінилось — оновлюємо залежні властивості.
                if (SetProperty(ref _isBusy, value))
                {
                    // 2) Enabled-стани залежать від IsBusy.
                    RaiseNavStateProperties();

                    // 3) CanExecute навігаційних команд теж залежить від IsBusy.
                    RaiseNavCanExecute();
                }
            }
        }

        private string? _busyText;

        /// <summary>
        /// BusyText — текст “що зараз відбувається” (наприклад "Opening Shop...").
        /// </summary>
        public string? BusyText
        {
            get => _busyText;
            private set => SetProperty(ref _busyText, value);
        }

        // =========================================================
        // 5) “Enabled” властивості (UI навігації)
        // =========================================================

        /// <summary>
        /// Кнопка Employee доступна, якщо:
        /// - ми не busy
        /// - і ми вже не на Employee
        /// </summary>
        public bool IsHomeEnabled => !IsBusy && ActivePage != NavPage.Home;

        public bool IsEmployeeEnabled => !IsBusy && ActivePage != NavPage.Employee;

        public bool IsShopEnabled => !IsBusy && ActivePage != NavPage.Shop;

        public bool IsAvailabilityEnabled => !IsBusy && ActivePage != NavPage.Availability;

        public bool IsContainerEnabled => !IsBusy && ActivePage != NavPage.Container;

        public bool IsInformationEnabled => !IsBusy && ActivePage != NavPage.Information;

        public bool IsDatabaseEnabled => !IsBusy && ActivePage != NavPage.Database;

        // =========================================================
        // 6) Commands
        // =========================================================

        public AsyncRelayCommand ShowHomeCommand { get; }
        public AsyncRelayCommand ShowEmployeeCommand { get; }
        public AsyncRelayCommand ShowShopCommand { get; }
        public AsyncRelayCommand ShowAvailabilityCommand { get; }
        public AsyncRelayCommand ShowContainerCommand { get; }
        public AsyncRelayCommand ShowInformationCommand { get; }
        public AsyncRelayCommand ShowDatabaseCommand { get; }

        public AsyncRelayCommand CloseCommand { get; }
        public AsyncRelayCommand MinimizeCommand { get; }

        /// <summary>
        /// Масив навігаційних команд — щоб не дублювати RaiseCanExecuteChanged() 4 рази.
        /// </summary>
        private readonly AsyncRelayCommand[] _navCommands;

        // =========================================================
        // 7) Constructor
        // =========================================================

        public MainViewModel(IServiceProvider sp)
        {
            // 1) Захист від null DI контейнера.
            _sp = sp ?? throw new ArgumentNullException(nameof(sp));

            // 2) Створюємо навігаційні команди через одну фабрику, щоб не дублювати шаблон.
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

            // 3) Збираємо навігаційні команди в масив для швидкого оновлення CanExecute.
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

            // 4) CloseCommand — закриває застосунок.
            //    Робимо null-safe на Application.Current.
            CloseCommand = new AsyncRelayCommand(() =>
            {
                Application.Current?.Shutdown();
                return Task.CompletedTask;
            });

            // 5) MinimizeCommand — мінімізує main window.
            MinimizeCommand = new AsyncRelayCommand(() =>
            {
                var wnd = Application.Current?.MainWindow;
                if (wnd != null)
                    wnd.WindowState = WindowState.Minimized;

                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// CreateNavCommand — фабрика для команд навігації.
        ///
        /// Навіщо:
        /// - щоб не повторювати той самий шаблон 4 рази
        /// - щоб гарантовано всі команди мають однакову семантику (busy + cancel + ensure init)
        /// </summary>
        private AsyncRelayCommand CreateNavCommand(
            NavPage page,
            string busyText,
            Func<object> getOrCreateVm,
            Func<bool> isEnabled)
        {
            // 1) execute: перейти на page.
            // 2) canExecute: залежить від isEnabled (яке в свою чергу залежить від IsBusy та ActivePage).
            return new AsyncRelayCommand(
                execute: () => NavigateAsync(page, busyText, getOrCreateVm),
                canExecute: isEnabled);
        }

        // =========================================================
        // 8) Navigation core
        // =========================================================

        /// <summary>
        /// NavigateAsync — центральний метод навігації.
        ///
        /// Ключові гарантії:
        /// - переходи виконуються ПО ОДНОМУ (через _navGate)
        /// - новий перехід скасовує попередній (через _navOpCts)
        /// - ActivePage/CurrentViewModel встановлюються ТІЛЬКИ після успішного EnsureInitializedAsync
        /// </summary>
        private async Task NavigateAsync(NavPage page, string busyText, Func<object> getOrCreateVm)
        {
            // 1) Беремо lifetime token, щоб можна було зупинити все при Dispose().
            var lifetimeToken = _lifetimeCts.Token;

            // 2) Серіалізуємо навігацію: поки один перехід йде — інший не запускається.
            await _navGate.WaitAsync(lifetimeToken).ConfigureAwait(false);

            try
            {
                // 3) Скасовуємо попередню навігаційну операцію (якщо вона ще не завершилась).
                //    Це важливо, якщо (наприклад) EnsureInitializedAsync робить I/O і користувач перемкнувся.
                _navOpCts?.Cancel();
                _navOpCts?.Dispose();

                // 4) Створюємо CTS для нового переходу:
                //    - лінкуємо з lifetimeToken (щоб Dispose теж скасував)
                //    - але дозволяємо скасовувати “лише навігацію”, не знищуючи lifetime
                _navOpCts = CancellationTokenSource.CreateLinkedTokenSource(lifetimeToken);

                // 5) Токен цього переходу.
                var navToken = _navOpCts.Token;

                // 6) Виконуємо навігацію в режимі Busy.
                await RunBusyAsync(async () =>
                {
                    // 6.1) Якщо вже скасовано — кидаємо OCE (ми її нижче нормально обробимо).
                    navToken.ThrowIfCancellationRequested();

                    // 6.2) Дістаємо або створюємо VM модуля.
                    var viewModel = getOrCreateVm();

                    // 6.3) Ініціалізуємо модуль (якщо треба).
                    //      Важливо: робимо це ДО установки CurrentViewModel,
                    //      щоб UI не показував “напівпорожній” модуль при проблемі.
                    await EnsureInitializedIfNeededAsync(viewModel, navToken).ConfigureAwait(false);

                    // 6.4) Повторно перевіряємо cancel після init.
                    navToken.ThrowIfCancellationRequested();

                    // 6.5) Тепер, коли все готово — перемикаємо UI.
                    //      Порядок:
                    //      - ActivePage: впливає на enabled-стани кнопок
                    //      - CurrentViewModel: ContentControl покаже DataTemplate
                    ActivePage = page;
                    CurrentViewModel = viewModel;

                }, busyText, navToken).ConfigureAwait(false);
            }
            finally
            {
                // 7) Завжди відпускаємо gate.
                _navGate.Release();
            }
        }

        /// <summary>
        /// EnsureInitializedIfNeededAsync — централізована ініціалізація модулів.
        ///
        /// Навіщо:
        /// - щоб не дублювати switch(viewModel) у NavigateAsync
        /// - щоб легше розширювати (додаси новий модуль — додаси 1 кейс)
        /// </summary>
        private static Task EnsureInitializedIfNeededAsync(object viewModel, CancellationToken ct)
        {
            // Примітка: ми НЕ вводимо спільний інтерфейс, щоб не чіпати існуючі VM.
            // Але якщо захочеш — можна зробити IInitializableVm з EnsureInitializedAsync(ct).
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

        // =========================================================
        // 9) Busy helper
        // =========================================================

        /// <summary>
        /// RunBusyAsync — обгортка для Busy UI.
        ///
        /// Правила:
        /// - IsBusy=true і BusyText=... на початку
        /// - в finally гарантуємо IsBusy=false і BusyText=null
        /// - OperationCanceledException (коли ct.IsCancellationRequested) НЕ вважаємо помилкою
        /// </summary>
        private async Task RunBusyAsync(Func<Task> action, string? text, CancellationToken ct)
        {
            // 1) Увімкнути busy.
            IsBusy = true;

            // 2) Показати текст (може бути null).
            BusyText = text;

            try
            {
                // 3) Виконати дію.
                await action().ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // 4) Якщо це очікуване скасування (наприклад, користувач перемкнув сторінку) —
                //    НЕ викидаємо далі і НЕ показуємо error.
            }
            finally
            {
                // 5) Вимкнути busy (навіть якщо було exception/cancel).
                IsBusy = false;

                // 6) Очистити текст.
                BusyText = null;
            }
        }

        // =========================================================
        // 10) Helpers: Raise properties + CanExecute
        // =========================================================

        /// <summary>
        /// RaiseNavStateProperties — єдина точка, де ми піднімаємо PropertyChanged для Is*Enabled.
        /// Це прибирає дублювання в ActivePage setter та IsBusy setter.
        /// </summary>
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

        /// <summary>
        /// RaiseNavCanExecute — оновлює CanExecute для всіх навігаційних команд.
        /// Викликається коли змінюється IsBusy або ActivePage.
        /// </summary>
        private void RaiseNavCanExecute()
        {
            // Якщо конструктор ще не завершився — _navCommands може бути null, але в нас він ініціалізується в ctor.
            for (int i = 0; i < _navCommands.Length; i++)
                _navCommands[i].RaiseCanExecuteChanged();
        }

        // =========================================================
        // 11) Dispose
        // =========================================================

        public void Dispose()
        {
            // 1) Скасовуємо lifetime — це “приб’є” будь-які ongoing operations.
            try { _lifetimeCts.Cancel(); } catch { /* ignore */ }

            // 2) Скасовуємо поточну навігацію (якщо була).
            try { _navOpCts?.Cancel(); } catch { /* ignore */ }

            // 3) Dispose CTS.
            _navOpCts?.Dispose();
            _lifetimeCts.Dispose();

            // 4) Dispose gate.
            _navGate.Dispose();
        }
    }
}
