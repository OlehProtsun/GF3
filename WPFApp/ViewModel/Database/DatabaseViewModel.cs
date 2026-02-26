/*
  Опис файлу: цей модуль містить реалізацію компонента DatabaseViewModel у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using Microsoft.Win32;
using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WPFApp.Applications.Notifications;
using WPFApp.MVVM.Commands;
using WPFApp.MVVM.Core;
using BusinessLogicLayer.Services.Abstractions;

namespace WPFApp.ViewModel.Database
{
    /// <summary>
    /// Визначає публічний елемент `public sealed class DatabaseViewModel : ViewModelBase` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class DatabaseViewModel : ViewModelBase
    {
        private readonly ISqliteAdminFacade _sqliteAdminService;
        private readonly IDatabaseChangeNotifier _databaseChangeNotifier;
                private bool _autoQueryExecuted;

        private string _executorSql = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";
        private DataView? _queryResult;
        private string _executorOutput = "Ready.";
        private bool _hasQueryResult;
        private bool _isExecutorError;

        private string _importFilePath = "No file selected.";
        private string _importFileSize = "-";
        private string _importFileModified = "-";
        private string _importFileHash = "-";
        private string _importScript = "-- Choose a .sql file to import, then review/adjust script and execute.";
        private string _importOutput = "Ready.";
        private bool _isImportError;

        private string _databasePath = string.Empty;
        private string _databaseSize = "0 bytes";
        private string _databaseLastUpdated = "-";
        private string _schemaVersion = "0";
        private string _tablesSummary = "No tables.";

        /// <summary>
        /// Визначає публічний елемент `public DatabaseViewModel(` та контракт його використання у шарі WPFApp.
        /// </summary>
        public DatabaseViewModel(
            ISqliteAdminFacade sqliteAdminService,
            IDatabaseChangeNotifier databaseChangeNotifier
            )
        {
            _sqliteAdminService = sqliteAdminService;
            _databaseChangeNotifier = databaseChangeNotifier;

            ExecuteSqlCommand = new AsyncRelayCommand(ExecuteSqlAsync);
            ClearExecutorOutputCommand = new RelayCommand(ClearExecutorOutput);
            ChooseImportFileCommand = new AsyncRelayCommand(ChooseImportFileAsync);
            ExecuteImportScriptCommand = new AsyncRelayCommand(ExecuteImportScriptAsync);
            RefreshInfoCommand = new AsyncRelayCommand(RefreshDatabaseInfoAsync);

            _ = InitializeOnEnterAsync();
        }

        private async Task InitializeOnEnterAsync()
        {
            if (_autoQueryExecuted) return;
            _autoQueryExecuted = true;

            
            await RefreshDatabaseInfoAsync(CancellationToken.None);

            
            await ExecuteSqlAsync(CancellationToken.None);
        }

        /// <summary>
        /// Визначає публічний елемент `public string ExecutorSql` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ExecutorSql
        {
            get => _executorSql;
            set => SetProperty(ref _executorSql, value);
        }

        /// <summary>
        /// Визначає публічний елемент `public DataView? QueryResult` та контракт його використання у шарі WPFApp.
        /// </summary>
        public DataView? QueryResult
        {
            get => _queryResult;
            private set => SetProperty(ref _queryResult, value);
        }

        /// <summary>
        /// Визначає публічний елемент `public string ExecutorOutput` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ExecutorOutput
        {
            get => _executorOutput;
            private set => SetProperty(ref _executorOutput, value);
        }

        /// <summary>
        /// Визначає публічний елемент `public bool HasQueryResult` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool HasQueryResult
        {
            get => _hasQueryResult;
            private set => SetProperty(ref _hasQueryResult, value);
        }

        /// <summary>
        /// Визначає публічний елемент `public bool IsExecutorError` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool IsExecutorError
        {
            get => _isExecutorError;
            private set => SetProperty(ref _isExecutorError, value);
        }

        /// <summary>
        /// Визначає публічний елемент `public string ImportFilePath` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ImportFilePath
        {
            get => _importFilePath;
            private set => SetProperty(ref _importFilePath, value);
        }

        /// <summary>
        /// Визначає публічний елемент `public string ImportFileSize` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ImportFileSize
        {
            get => _importFileSize;
            private set => SetProperty(ref _importFileSize, value);
        }

        /// <summary>
        /// Визначає публічний елемент `public string ImportFileModified` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ImportFileModified
        {
            get => _importFileModified;
            private set => SetProperty(ref _importFileModified, value);
        }

        /// <summary>
        /// Визначає публічний елемент `public string ImportFileHash` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ImportFileHash
        {
            get => _importFileHash;
            private set => SetProperty(ref _importFileHash, value);
        }

        /// <summary>
        /// Визначає публічний елемент `public string ImportScript` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ImportScript
        {
            get => _importScript;
            set => SetProperty(ref _importScript, value);
        }

        /// <summary>
        /// Визначає публічний елемент `public string ImportOutput` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ImportOutput
        {
            get => _importOutput;
            private set => SetProperty(ref _importOutput, value);
        }

        /// <summary>
        /// Визначає публічний елемент `public bool IsImportError` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool IsImportError
        {
            get => _isImportError;
            private set => SetProperty(ref _isImportError, value);
        }

        /// <summary>
        /// Визначає публічний елемент `public string DatabasePath` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string DatabasePath
        {
            get => _databasePath;
            private set => SetProperty(ref _databasePath, value);
        }

        /// <summary>
        /// Визначає публічний елемент `public string DatabaseSize` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string DatabaseSize
        {
            get => _databaseSize;
            private set => SetProperty(ref _databaseSize, value);
        }

        /// <summary>
        /// Визначає публічний елемент `public string DatabaseLastUpdated` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string DatabaseLastUpdated
        {
            get => _databaseLastUpdated;
            private set => SetProperty(ref _databaseLastUpdated, value);
        }

        /// <summary>
        /// Визначає публічний елемент `public string SchemaVersion` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string SchemaVersion
        {
            get => _schemaVersion;
            private set => SetProperty(ref _schemaVersion, value);
        }

        /// <summary>
        /// Визначає публічний елемент `public string TablesSummary` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string TablesSummary
        {
            get => _tablesSummary;
            private set => SetProperty(ref _tablesSummary, value);
        }

        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand ExecuteSqlCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand ExecuteSqlCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public RelayCommand ClearExecutorOutputCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public RelayCommand ClearExecutorOutputCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand ChooseImportFileCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand ChooseImportFileCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand ExecuteImportScriptCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand ExecuteImportScriptCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand RefreshInfoCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand RefreshInfoCommand { get; }

        private async Task ExecuteSqlAsync(CancellationToken ct)
        {
            try
            {
                var result = await _sqliteAdminService.ExecuteSqlAsync(ExecutorSql, ct);
                IsExecutorError = false;
                ExecutorOutput = result.Message;

                if (result.IsSelect && result.ResultTable != null)
                {
                    QueryResult = result.ResultTable.DefaultView;
                    HasQueryResult = true;
                }
                else
                {
                    QueryResult = null;
                    HasQueryResult = false;
                }

                await RefreshDatabaseInfoAsync(ct);

                if (!result.IsSelect)
                {
                    _databaseChangeNotifier.NotifyDatabaseChanged("DatabaseView.ExecuteSql");
                }

            }
            catch (Exception ex)
            {
                QueryResult = null;
                HasQueryResult = false;
                IsExecutorError = true;
                ExecutorOutput = ex.Message;
            }
        }

        private void ClearExecutorOutput()
        {
            QueryResult = null;
            HasQueryResult = false;
            IsExecutorError = false;
            ExecutorOutput = "Output cleared.";
        }

        private async Task ChooseImportFileAsync(CancellationToken ct)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Choose SQL file",
                Filter = "SQL files (*.sql)|*.sql|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true)
                return;

            var filePath = dialog.FileName;
            var fi = new FileInfo(filePath);

            ImportFilePath = fi.FullName;
            ImportFileSize = FormatBytes(fi.Length);
            ImportFileModified = fi.LastWriteTime.ToString("g", CultureInfo.CurrentCulture);
            ImportFileHash = await _sqliteAdminService.ComputeFileHashAsync(filePath, ct);

            ImportScript = await File.ReadAllTextAsync(filePath, ct);
            IsImportError = false;
            ImportOutput = "File loaded. Review script and execute import.";
        }

        private async Task ExecuteImportScriptAsync(CancellationToken ct)
        {
            try
            {
                await _sqliteAdminService.ImportSqlScriptAsync(ImportScript, ct);
                _databaseChangeNotifier.NotifyDatabaseChanged("DatabaseView.ImportSqlScript");
                                IsImportError = false;
                ImportOutput = "Import script executed successfully against the current application database.";
                await RefreshDatabaseInfoAsync(ct);

            }
            catch (Exception ex)
            {
                IsImportError = true;
                ImportOutput = ex.Message;
            }
        }

        private async Task RefreshDatabaseInfoAsync(CancellationToken ct)
        {
            var info = await _sqliteAdminService.GetDatabaseInfoAsync(ct);

            DatabasePath = info.DatabasePath;
            DatabaseSize = FormatBytes(info.FileSizeBytes);
            DatabaseLastUpdated = info.LastModifiedUtc == DateTime.MinValue
                ? "-"
                : info.LastModifiedUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
            SchemaVersion = info.UserVersion.ToString(CultureInfo.InvariantCulture);
            TablesSummary = info.Tables.Count == 0
                ? "No user tables found."
                : string.Join(Environment.NewLine, info.Tables);
        }

        private static string FormatBytes(long bytes)
        {
            string[] units = ["B", "KB", "MB", "GB"];
            double value = bytes;
            var idx = 0;

            while (value >= 1024 && idx < units.Length - 1)
            {
                value /= 1024;
                idx++;
            }

            return $"{value:0.##} {units[idx]}";
        }
    }
}
