using BusinessLogicLayer;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.Extensions.DependencyInjection;
using WinFormsApp.Presenter;
using WinFormsApp.Presenter.Availability;
using WinFormsApp.Presenter.Container;
using WinFormsApp.Presenter.Employee;
using WinFormsApp.View.Availability;
using WinFormsApp.View.Container;
using WinFormsApp.View.Employee;
using WinFormsApp.View.Main;

namespace WinFormsApp
{
    internal static class Program
    {

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            
            ApplicationConfiguration.Initialize();

            var services = new ServiceCollection();

            var dbPath = Path.Combine(AppContext.BaseDirectory, "DB", "SQLite.db");
            services.AddDataAccess($"Data Source={dbPath}");
            services.AddBusinessLogicLayer();
            services.AddSingleton<BusinessLogicLayer.Generators.IScheduleGenerator, BusinessLogicLayer.Generators.ScheduleGenerator>();

            // Main
            services.AddTransient<MainPresenter>();
            services.AddTransient<MainView>(sp =>
            {
                var view = new MainView();
                // Прив’язуємо презентер до конкретного інстанса View через DI
                _ = ActivatorUtilities.CreateInstance<MainPresenter>(sp, view);
                return view;
            });

            // Employee
            services.AddTransient<EmployeePresenter>();
            services.AddTransient<EmployeeView>(sp =>
            {
                var view = new EmployeeView();
                var presenter = ActivatorUtilities.CreateInstance<EmployeePresenter>(sp, view);
                // Стартове завантаження асинхронно (без блокування UI)
                _ = presenter.InitializeAsync();
                return view;
            });

            // Availability
            services.AddTransient<AvailabilityPresenter>();
            services.AddTransient<AvailabilityView>(sp =>
            {
                var view = new AvailabilityView();
                var presenter = ActivatorUtilities.CreateInstance<AvailabilityPresenter>(sp, view);
                // Стартове завантаження асинхронно (без блокування UI)
                _ = presenter.InitializeAsync();
                return view;
            });

            // Container
            services.AddTransient<ContainerPresenter>();
            services.AddTransient<ContainerView>(sp =>
            {
                var view = new ContainerView();
                var presenter = ActivatorUtilities.CreateInstance<ContainerPresenter>(sp, view);
                _ = presenter.InitializeAsync();
                return view;
            });

            using var provider = services.BuildServiceProvider();
            Application.Run(provider.GetRequiredService<MainView>());
            
        }
    }
}