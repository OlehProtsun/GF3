/*
  Опис файлу: цей модуль містить реалізацію компонента App у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer;
using BusinessLogicLayer.Services.Abstractions;
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
using WPFApp.Applications.Export;


namespace WPFApp
{
    /// <summary>
    /// Визначає публічний елемент `public partial class App : Application` та контракт його використання у шарі WPFApp.
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;

        /// <summary>
        /// Визначає публічний елемент `public App()` та контракт його використання у шарі WPFApp.
        /// </summary>
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
            
            var dbPathProvider = new DatabasePathProvider();
            services.AddSingleton<IDatabasePathProvider>(dbPathProvider);
            services.AddBusinessLogicStack(dbPathProvider.ConnectionString, dbPathProvider.DatabaseFilePath);

            
            services.AddSingleton<BusinessLogicLayer.Generators.IScheduleGenerator,
                                  BusinessLogicLayer.Generators.ScheduleGenerator>();

            
            services.AddSingleton<MainViewModel>();
            services.AddTransient<HomeViewModel>();
            services.AddTransient<EmployeeViewModel>();
            services.AddTransient<AvailabilityViewModel>();
            services.AddTransient<ShopViewModel>();
            services.AddTransient<ContainerViewModel>();
            services.AddTransient<InformationViewModel>();
            services.AddTransient<DatabaseViewModel>();

            
            services.AddSingleton<MainWindow>();

            services.AddSingleton<IColorPickerService, ColorPickerService>();
            services.AddSingleton<IScheduleExportService, ScheduleExportService>();
            services.AddSingleton<IDatabaseChangeNotifier, DatabaseChangeNotifier>();

            
            services.AddTransient<HomeView>();
            services.AddTransient<EmployeeView>();
            services.AddTransient<AvailabilityView>();
            services.AddTransient<ShopView>();
            services.AddTransient<ContainerView>();
            services.AddTransient<InformationView>();
            services.AddTransient<DatabaseView>();

            
            
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                _host!.StartAsync().GetAwaiter().GetResult();

                
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
