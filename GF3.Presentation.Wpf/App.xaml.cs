using System;
using System.IO;
using System.Windows;
using BusinessLogicLayer;
using DataAccessLayer.Models.DataBaseContext;
using GF3.Presentation.Wpf.Services;
using GF3.Presentation.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GF3.Presentation.Wpf
{
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var dbPath = Path.Combine(AppContext.BaseDirectory, "DB", "SQLite.db");
            services.AddDataAccess($"Data Source={dbPath}");
            services.AddBusinessLogicLayer();
            services.AddSingleton<BusinessLogicLayer.Generators.IScheduleGenerator, BusinessLogicLayer.Generators.ScheduleGenerator>();

            services.AddSingleton<IMessageDialogService, MessageDialogService>();
            services.AddSingleton<MainWindow>();
            services.AddSingleton<MainWindowViewModel>();

            services.AddTransient<EmployeePageViewModel>();
            services.AddTransient<ShopPageViewModel>();
            services.AddTransient<AvailabilityPageViewModel>();
            services.AddTransient<ContainerPageViewModel>();
        }
    }
}
