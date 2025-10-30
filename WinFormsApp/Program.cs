using DataAccessLayer.Models.DataBaseContext;
using Microsoft.Extensions.DependencyInjection;

namespace WinFormsApp
{
    internal static class Program
    {
        public static IServiceProvider Services { get; private set; } = default!;

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

            // форму реєструємо в DI
            services.AddTransient<Form1>();

            Services = services.BuildServiceProvider();



            Application.Run(Services.GetRequiredService<Form1>());
        }
    }
}