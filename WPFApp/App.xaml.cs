using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using BusinessLogicLayer;
using DataAccessLayer.Models.DataBaseContext;
using WPFApp.View;
using WPFApp.View.Availability;
using WPFApp.View.Employee;
using WPFApp.View.Shop;
using WPFApp.ViewModel.Availability;
using WPFApp.ViewModel.Container;
using WPFApp.ViewModel.Employee;
using WPFApp.ViewModel.Shop;

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
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GF3");

            Directory.CreateDirectory(root);

            var dbPath = Path.Combine(root, "SQLite.db");
            services.AddDataAccess($"Data Source={dbPath}");

            services.AddBusinessLogicLayer();

            // Те, що ти реєстрував вручну
            services.AddSingleton<BusinessLogicLayer.Generators.IScheduleGenerator,
                                  BusinessLogicLayer.Generators.ScheduleGenerator>();

            // 2) Реєстрація ViewModels
            services.AddSingleton<WPFApp.ViewModel.MainViewModel>();
            services.AddTransient<EmployeeViewModel>();
            services.AddTransient<AvailabilityViewModel>();
            services.AddTransient<ShopViewModel>();
            services.AddTransient<ContainerViewModel>();

            // 3) Реєстрація Views (Windows/UserControls)
            services.AddSingleton<MainWindow>();

            services.AddSingleton<WPFApp.Service.IColorPickerService, WPFApp.Service.ColorPickerService>();

            // Якщо робиш навігацію через UserControl-и:
            services.AddTransient<EmployeeView>();
            services.AddTransient<AvailabilityView>();
            services.AddTransient<ShopView>();
            services.AddTransient<ContainerView>();

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
