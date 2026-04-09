export type AdminDbSqlRequest = { sql: string };

export type AdminDbObject = {
  type: string;
  name: string;
  sql: string;
};

export type AdminDbMetadataResponse = {
  sqliteVersion: string;
  databasePath: string;
  fileSizeBytes: number;
  lastModifiedUtc: string | null;
  userVersion: number;
  tables: string[];
  objects: AdminDbObject[];
  allowWriteSql: boolean;
  maxSqlLength: number;
  maxImportBytes: number;
};

export type AdminDbHashResponse = {
  hash: string;
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
