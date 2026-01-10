using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using BusinessLogicLayer;
using DataAccessLayer.Models.DataBaseContext;
using WPFApp.View;
using WPFApp.View.Availability;
using WPFApp.ViewModel.Availability;

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
            var dbPath = Path.Combine(AppContext.BaseDirectory, "DB", "SQLite.db");
            services.AddDataAccess($"Data Source={dbPath}");
            services.AddBusinessLogicLayer();

            // Те, що ти реєстрував вручну
            services.AddSingleton<BusinessLogicLayer.Generators.IScheduleGenerator,
                                  BusinessLogicLayer.Generators.ScheduleGenerator>();

            // 2) Реєстрація ViewModels
            services.AddSingleton<WPFApp.ViewModel.MainViewModel>();
            services.AddTransient<WPFApp.ViewModel.EmployeeViewModel>();
            services.AddTransient<AvailabilityViewModel>();
            services.AddTransient<WPFApp.ViewModel.ShopViewModel>();
            services.AddTransient<WPFApp.ViewModel.ContainerViewModel>();

            // 3) Реєстрація Views (Windows/UserControls)
            services.AddSingleton<MainWindow>();

            // Якщо робиш навігацію через UserControl-и:
            services.AddTransient<View.EmployeeView>();
            services.AddTransient<AvailabilityView>();
            services.AddTransient<ShopView>();
            services.AddTransient<ContainerView>();

            // 4) Навігація / фабрики (заміна твого MdiViewFactory)
            //services.AddSingleton<Service.NavigationService>();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            await _host!.StartAsync();

            // Відкриваємо головне вікно через DI
            var mainWindow = _host.Services.GetRequiredService<View.MainWindow>();
            mainWindow.Show();

            // Якщо треба — можна викликати async init у VM
            // (приклад нижче, як це робити красиво)
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }

            base.OnExit(e);
        }
    }
}
