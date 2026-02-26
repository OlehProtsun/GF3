/*
  Опис файлу: цей модуль містить реалізацію компонента DatabasePathProvider у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.IO;

namespace WPFApp.Applications.Configuration
{
    /// <summary>
    /// Визначає публічний елемент `public interface IDatabasePathProvider` та контракт його використання у шарі WPFApp.
    /// </summary>
    public interface IDatabasePathProvider
    {
        string RootDirectory { get; }
        string DatabaseFilePath { get; }
        string ConnectionString { get; }
    }

    /// <summary>
    /// Визначає публічний елемент `public sealed class DatabasePathProvider : IDatabasePathProvider` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class DatabasePathProvider : IDatabasePathProvider
    {
        /// <summary>
        /// Визначає публічний елемент `public string RootDirectory { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string RootDirectory { get; }
        /// <summary>
        /// Визначає публічний елемент `public string DatabaseFilePath { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string DatabaseFilePath { get; }
        /// <summary>
        /// Визначає публічний елемент `public string ConnectionString { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ConnectionString { get; }

        /// <summary>
        /// Визначає публічний елемент `public DatabasePathProvider()` та контракт його використання у шарі WPFApp.
        /// </summary>
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
