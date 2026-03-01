export type AdminDbSqlRequest = { sql: string };

export type AdminDbHashResponse = { hash: string };
export type AdminDbExecuteResponse = { affectedRows: number };

export type AdminDbMetadataResponse = Record<string, unknown>;
export type AdminDbQueryResponse = Record<string, unknown>;
export type AdminDbImportResponse = Record<string, unknown>;
