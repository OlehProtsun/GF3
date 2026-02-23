using Microsoft.Win32;
using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WPFApp.Infrastructure;
using WPFApp.Service;

namespace WPFApp.ViewModel.Database
{
    public sealed class DatabaseViewModel : ViewModelBase
    {
        private readonly ISqliteAdminService _sqliteAdminService;
        private readonly IDatabaseChangeNotifier _databaseChangeNotifier;
        private readonly ILoggerService _logger;
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

        public DatabaseViewModel(
            ISqliteAdminService sqliteAdminService,
            IDatabaseChangeNotifier databaseChangeNotifier,
            ILoggerService logger)
        {
            _sqliteAdminService = sqliteAdminService;
            _databaseChangeNotifier = databaseChangeNotifier;
            _logger = logger;

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

            // якщо хочеш — спочатку онови інфу
            await RefreshDatabaseInfoAsync(CancellationToken.None);

            // виконається запит з ExecutorSql (він у тебе вже заданий дефолтом)
            await ExecuteSqlAsync(CancellationToken.None);
        }

        public string ExecutorSql
        {
            get => _executorSql;
            set => SetProperty(ref _executorSql, value);
        }

        public DataView? QueryResult
        {
            get => _queryResult;
            private set => SetProperty(ref _queryResult, value);
        }

        public string ExecutorOutput
        {
            get => _executorOutput;
            private set => SetProperty(ref _executorOutput, value);
        }

        public bool HasQueryResult
        {
            get => _hasQueryResult;
            private set => SetProperty(ref _hasQueryResult, value);
        }

        public bool IsExecutorError
        {
            get => _isExecutorError;
            private set => SetProperty(ref _isExecutorError, value);
        }

        public string ImportFilePath
        {
            get => _importFilePath;
            private set => SetProperty(ref _importFilePath, value);
        }

        public string ImportFileSize
        {
            get => _importFileSize;
            private set => SetProperty(ref _importFileSize, value);
        }

        public string ImportFileModified
        {
            get => _importFileModified;
            private set => SetProperty(ref _importFileModified, value);
        }

        public string ImportFileHash
        {
            get => _importFileHash;
            private set => SetProperty(ref _importFileHash, value);
        }

        public string ImportScript
        {
            get => _importScript;
            set => SetProperty(ref _importScript, value);
        }

        public string ImportOutput
        {
            get => _importOutput;
            private set => SetProperty(ref _importOutput, value);
        }

        public bool IsImportError
        {
            get => _isImportError;
            private set => SetProperty(ref _isImportError, value);
        }

        public string DatabasePath
        {
            get => _databasePath;
            private set => SetProperty(ref _databasePath, value);
        }

        public string DatabaseSize
        {
            get => _databaseSize;
            private set => SetProperty(ref _databaseSize, value);
        }

        public string DatabaseLastUpdated
        {
            get => _databaseLastUpdated;
            private set => SetProperty(ref _databaseLastUpdated, value);
        }

        public string SchemaVersion
        {
            get => _schemaVersion;
            private set => SetProperty(ref _schemaVersion, value);
        }

        public string TablesSummary
        {
            get => _tablesSummary;
            private set => SetProperty(ref _tablesSummary, value);
        }

        public AsyncRelayCommand ExecuteSqlCommand { get; }
        public RelayCommand ClearExecutorOutputCommand { get; }
        public AsyncRelayCommand ChooseImportFileCommand { get; }
        public AsyncRelayCommand ExecuteImportScriptCommand { get; }
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
                _logger.Log("[DB-CHANGE] Import SQL completed; change event emitted.");
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
