using System;
using System.IO;

namespace WPFApp.Service
{
    public interface IDatabasePathProvider
    {
        string RootDirectory { get; }
        string DatabaseFilePath { get; }
        string ConnectionString { get; }
    }

    public sealed class DatabasePathProvider : IDatabasePathProvider
    {
        public string RootDirectory { get; }
        public string DatabaseFilePath { get; }
        public string ConnectionString { get; }

        public DatabasePathProvider()
        {
            RootDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GF3");

            Directory.CreateDirectory(RootDirectory);

            DatabaseFilePath = Path.Combine(RootDirectory, "SQLite.db");
            ConnectionString = $"Data Source={DatabaseFilePath}";
        }
    }
}
