export type AdminDbSqlRequest = { sql: string };

export type AdminDbObject = {
  type: string;
  name: string;
  sql: string;
};

export type AdminDbFileEntry = {
  name: string;
  path: string;
  category: string;
  fileSizeBytes: number;
  lastModifiedUtc: string | null;
  isActive: boolean;
};

export type AdminDbStorageWorkspace = {
  workspaceRootPath: string;
  automaticBackupDirectoryPath: string;
  manualCopyDirectoryPath: string;
  automaticBackupRetentionLimit: number;
  availableDatabases: AdminDbFileEntry[];
  automaticBackups: AdminDbFileEntry[];
  manualCopies: AdminDbFileEntry[];
};

export type AdminDbMetadataResponse = {
  sqliteVersion: string;
  databasePath: string;
  fileSizeBytes: number;
  lastModifiedUtc: string | null;
  userVersion: number;
  tables: string[];
  objects: AdminDbObject[];
  storageWorkspace: AdminDbStorageWorkspace;
  allowWriteSql: boolean;
  maxSqlLength: number;
  maxImportBytes: number;
};

export type AdminDbHashResponse = {
  hash: string;
};

export type AdminDbSelectDatabaseRequest = {
  databasePath: string;
};

export type AdminDbExecuteResponse = {
  affectedRows: number;
};

export type AdminDbQueryResponse = {
  columns: string[];
  rows: unknown[][];
  rowCount: number;
};

export type AdminDbImportResponse = {
  statementsExecuted: number;
  statementsApplied: number;
  statementsAlreadyExisted: number;
  serviceStatementsSkipped: number;
  failedStatementIndex?: number | null;
  failureReason?: string | null;
};
