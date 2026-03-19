using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccessLayer.Models.DataBaseContext
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("GF3_CONNECTION_STRING");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                var root = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "GF3");

                Directory.CreateDirectory(root);

                var dbPath = Path.Combine(root, "SQLite.db");
                connectionString = $"Data Source={dbPath}";
            }

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlite(connectionString);

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
