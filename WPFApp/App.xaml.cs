using BusinessLogicLayer;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Windows;
using WPFApp.View;
using WPFApp.View.Availability;
using WPFApp.View.Employee;
using WPFApp.View.Shop;
using WPFApp.View.Home;
using WPFApp.View.Information;
using WPFApp.ViewModel.Availability.Main;
using WPFApp.ViewModel.Container.Edit;
using WPFApp.ViewModel.Employee;
using WPFApp.ViewModel.Main;
using WPFApp.ViewModel.Shop;
using WPFApp.ViewModel.Database;
using WPFApp.ViewModel.Home;
using WPFApp.ViewModel.Information;
using WPFApp.UI.Dialogs;
using WPFApp.Applications.Configuration;
using WPFApp.Applications.Notifications;
using WPFApp.Applications.Diagnostics;
using WPFApp.Applications.Export;


namespace WPFApp
{
    public partial class App : Application
    {
        private IHost? _host;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(services);
                })
                .Build();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // 1) DAL/BLL як у WinForms
            var dbPathProvider = new DatabasePathProvider();
            services.AddSingleton<IDatabasePathProvider>(dbPathProvider);
            services.AddDataAccess(dbPathProvider.ConnectionString);

            services.AddBusinessLogicLayer();

            // Те, що ти реєстрував вручну
            services.AddSingleton<BusinessLogicLayer.Generators.IScheduleGenerator,
                                  BusinessLogicLayer.Generators.ScheduleGenerator>();

            // 2) Реєстрація ViewModels
            services.AddSingleton<MainViewModel>();
            services.AddTransient<HomeViewModel>();
            services.AddTransient<EmployeeViewModel>();
            services.AddTransient<AvailabilityViewModel>();
            services.AddTransient<ShopViewModel>();
            services.AddTransient<ContainerViewModel>();
            services.AddTransient<InformationViewModel>();
            services.AddTransient<DatabaseViewModel>();

            // 3) Реєстрація Views (Windows/UserControls)
            services.AddSingleton<MainWindow>();

            services.AddSingleton<IColorPickerService, ColorPickerService>();
            services.AddSingleton<ISqliteAdminService, SqliteAdminService>();
            services.AddSingleton<ILoggerService>(_ => LoggerService.Instance);
            services.AddSingleton<IScheduleExportService, ScheduleExportService>();
            services.AddSingleton<IDatabaseChangeNotifier, DatabaseChangeNotifier>();

            // Якщо робиш навігацію через UserControl-и:
            services.AddTransient<HomeView>();
            services.AddTransient<EmployeeView>();
            services.AddTransient<AvailabilityView>();
            services.AddTransient<ShopView>();
            services.AddTransient<ContainerView>();
            services.AddTransient<InformationView>();
            services.AddTransient<DatabaseView>();

            // 4) Навігація / фабрики (заміна твого MdiViewFactory)
            //services.AddSingleton<Service.NavigationService>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                _host!.StartAsync().GetAwaiter().GetResult();

                // Відкриваємо головне вікно через DI
                var mainWindow = _host.Services.GetRequiredService<View.MainWindow>();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Startup error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(-1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_host != null)
            {
                try
                {
                    _host.StopAsync().GetAwaiter().GetResult();
                }
                finally
                {
                    _host.Dispose();
                }
            }

            base.OnExit(e);
        }
    }
}
