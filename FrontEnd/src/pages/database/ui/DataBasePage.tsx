import { useDeferredValue, useEffect, useId, useLayoutEffect, useMemo, useRef, useState } from "react";
import type { ChangeEvent, FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import {
  type AdminDbFileEntry,
  type AdminDbImportResponse,
  type AdminDbObject,
  type AdminDbQueryResponse,
  adminDbApi,
  useAdminDbExecuteMutation,
  useAdminDbHashQuery,
  useAdminDbImportMutation,
  useAdminDbManualCopyMutation,
  useAdminDbMetadataQuery,
  useAdminDbQueryMutation,
  useAdminDbSelectDatabaseMutation,
} from "@entities/admin-db";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ApiError } from "@shared/api/httpClient";
import { queryKeys } from "@shared/api/queryKeys";
import { usePageScrollbarHidden } from "@shared/lib/usePageScrollbarHidden";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import { CheckIcon, CodeIcon, DatabaseIcon, ImportIcon, SearchIcon, WarnIcon } from "@shared/ui/icons";
import { PageHeader } from "@shared/ui/PageHeader";
import { CardSection } from "@shared/ui/sections/CardSection";
import styles from "./DataBasePage.module.css";

const DEFAULT_EXECUTOR_SQL = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";
const READ_PREFIXES = ["SELECT", "PRAGMA", "WITH"];
const WRITE_PREFIXES = ["INSERT", "UPDATE", "DELETE"];
const VIEWER_LIMIT_OPTIONS = [25, 100, 250];
const VIEWER_QUERY_KEY = ["admin-db-viewer-preview"] as const;
const WIDE_VIEWER_ROW_MEDIA_QUERY = "(min-width: 1181px)";

type ResultTone = "neutral" | "success" | "error";

type ImportFileState = {
  fileName: string;
  originalText: string;
  size: number;
  lastModified: number;
  hash: string | null;
};

function formatBytes(bytes: number) {
  if (!Number.isFinite(bytes) || bytes <= 0) {
    return "0 B";
  }

  const units = ["B", "KB", "MB", "GB", "TB"];
  let value = bytes;
  let unitIndex = 0;

  while (value >= 1024 && unitIndex < units.length - 1) {
    value /= 1024;
    unitIndex += 1;
  }

  return `${value.toFixed(value >= 100 || unitIndex === 0 ? 0 : value >= 10 ? 1 : 2)} ${units[unitIndex]}`;
}

function formatDateTime(value: string | number | null | undefined) {
  if (value == null) {
    return "-";
  }

  const date = typeof value === "number" ? new Date(value) : new Date(value);
  if (Number.isNaN(date.valueOf())) {
    return "-";
  }

  return date.toLocaleString();
}

function formatCellValue(value: unknown) {
  if (value == null) {
    return "NULL";
  }

  if (typeof value === "object") {
    try {
      return JSON.stringify(value);
    } catch {
      return String(value);
    }
  }

  return String(value);
}

function detectSqlMode(sql: string): "read" | "write" | "unknown" {
  const trimmed = sql.trimStart().toUpperCase();

  if (READ_PREFIXES.some(prefix => trimmed.startsWith(prefix))) {
    return "read";
  }

  if (WRITE_PREFIXES.some(prefix => trimmed.startsWith(prefix))) {
    return "write";
  }

  return "unknown";
}

function getObjectKey(item: AdminDbObject) {
  return `${item.type}:${item.name}`;
}

function getObjectCount(objects: AdminDbObject[], type: string) {
  return objects.filter(item => item.type === type).length;
}

function describeApiError(error: unknown, fallbackMessage: string) {
  if (error instanceof ApiError) {
    if (error.status === 403) {
      return "Admin DB access is blocked by the current backend policy. Start GF3 through the launcher or enable remote admin access on the host machine.";
    }

    if (error.status === 404) {
      return "Admin DB tools are disabled in the current Web API configuration.";
    }

    return error.message || fallbackMessage;
  }

  if (error instanceof Error && error.message) {
    return error.message;
  }

  return fallbackMessage;
}

function buildImportResultMessage(result: AdminDbImportResponse) {
  const parts = [
    `Executable statements: ${result.statementsExecuted}.`,
    `Applied: ${result.statementsApplied}.`,
    `Already existed: ${result.statementsAlreadyExisted}.`,
    `Service lines skipped: ${result.serviceStatementsSkipped}.`,
  ];

  if (result.failedStatementIndex == null) {
    return `Import completed. ${parts.join(" ")}`;
  }

  const reason = result.failureReason ? ` Reason: ${result.failureReason}.` : "";
  return `Import rolled back at statement #${result.failedStatementIndex + 1}. ${parts.join(" ")}${reason}`;
}

async function computeFileHash(file: File) {
  if (typeof window === "undefined" || typeof window.crypto === "undefined" || !window.crypto.subtle) {
    return null;
  }

  try {
    const buffer = await file.arrayBuffer();
    const digest = await window.crypto.subtle.digest("SHA-256", buffer);
    return Array.from(new Uint8Array(digest))
      .map(chunk => chunk.toString(16).padStart(2, "0"))
      .join("");
  } catch {
    return null;
  }
}

function buildQueryMessage(result: AdminDbQueryResponse) {
  if (result.rowCount === 0) {
    return "Query completed. No rows returned.";
  }

  return `Query completed. Rows: ${result.rowCount}.`;
}

function quoteSqlIdentifier(identifier: string) {
  return `"${identifier.replaceAll('"', '""')}"`;
}

function isPreviewableObjectType(type: string) {
  const normalizedType = type.trim().toLowerCase();
  return normalizedType === "table" || normalizedType === "view";
}

function buildPreviewSql(item: AdminDbObject, rowLimit: number) {
  return `SELECT * FROM ${quoteSqlIdentifier(item.name)} LIMIT ${rowLimit};`;
}

function getDefaultObjectKey(objects: AdminDbObject[]) {
  const preferredObject = objects.find(item => isPreviewableObjectType(item.type)) ?? objects[0] ?? null;
  return preferredObject ? getObjectKey(preferredObject) : "";
}

function getDatabaseCategoryLabel(category: string) {
  switch (category) {
    case "backup":
      return "Auto backup";
    case "manualCopy":
      return "Manual copy";
    default:
      return "Database";
  }
}

function buildDatabaseOptionLabel(entry: AdminDbFileEntry) {
  const parts = [entry.name, getDatabaseCategoryLabel(entry.category)];

  if (entry.lastModifiedUtc) {
    parts.push(formatDateTime(entry.lastModifiedUtc));
  }

  return parts.join(" • ");
}

function DeveloperAccessDialog({
  password,
  error,
  isSubmitting,
  onPasswordChange,
  onSubmit,
  onCancel,
}: {
  password: string;
  error: string | null;
  isSubmitting: boolean;
  onPasswordChange: (value: string) => void;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  onCancel: () => void;
}) {
  const titleId = useId();
  const descriptionId = useId();

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape" && !isSubmitting) {
        onCancel();
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [isSubmitting, onCancel]);

  return (
    <div className={styles.accessOverlay} role="dialog" aria-modal="true" aria-labelledby={titleId} aria-describedby={descriptionId}>
      <form className={styles.accessDialog} onSubmit={onSubmit}>
        <div className={styles.accessHero}>
          <span className={styles.accessIcon} aria-hidden="true">
            <DatabaseIcon size={24} />
          </span>
          <div className={styles.accessCopy}>
            <span className={styles.accessEyebrow}>Developer access</span>
            <h2 id={titleId}>Unlock Database</h2>
            <p id={descriptionId}>Enter the developer password configured on the GF3 host.</p>
          </div>
        </div>

        <label className={styles.accessField} htmlFor="database-developer-password">
          <span>Developer password</span>
          <input
            id="database-developer-password"
            type="password"
            value={password}
            autoComplete="off"
            autoFocus
            placeholder="Enter password"
            disabled={isSubmitting}
            onChange={event => onPasswordChange(event.target.value)}
          />
        </label>

        {error ? <div className={styles.accessError} role="alert">{error}</div> : null}

        <div className={styles.accessActions}>
          <IosButton
            label="Go back"
            type="button"
            variant="secondary"
            customColor="#e5e7eb"
            customBorderColor="#d1d5db"
            disabled={isSubmitting}
            onClick={onCancel}
          />
          <IosButton
            label={isSubmitting ? "Checking..." : "Unlock database"}
            type="submit"
            icon={<CheckIcon size={16} />}
            disabled={isSubmitting || !password.trim()}
          />
        </div>
      </form>
    </div>
  );
}

export function DataBasePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [developerPassword, setDeveloperPassword] = useState("");
  const [accessError, setAccessError] = useState<string | null>(null);
  const [isCheckingAccess, setIsCheckingAccess] = useState(false);
  const [isUnlocked, setIsUnlocked] = useState(false);

  const handleUnlock = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const password = developerPassword.trim();
    if (!password || isCheckingAccess) {
      return;
    }

    setAccessError(null);
    setIsCheckingAccess(true);

    try {
      const metadata = await adminDbApi.unlock(password);
      queryClient.setQueryData(queryKeys.adminDb.metadata(), metadata);
      setDeveloperPassword("");
      setIsUnlocked(true);
    } catch (error) {
      setAccessError(error instanceof ApiError ? error.message : "Could not verify the developer password.");
    } finally {
      setIsCheckingAccess(false);
    }
  };

  if (!isUnlocked) {
    return (
      <DeveloperAccessDialog
        password={developerPassword}
        error={accessError}
        isSubmitting={isCheckingAccess}
        onPasswordChange={value => {
          setDeveloperPassword(value);
          setAccessError(null);
        }}
        onSubmit={handleUnlock}
        onCancel={() => navigate(-1)}
      />
    );
  }

  return <DataBaseWorkspace />;
}

function DataBaseWorkspace() {
  usePageScrollbarHidden();

  const queryClient = useQueryClient();
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const overviewCardRef = useRef<HTMLElement | null>(null);
  const [executorSql, setExecutorSql] = useState(DEFAULT_EXECUTOR_SQL);
  const [executorMessage, setExecutorMessage] = useState("Ready.");
  const [executorTone, setExecutorTone] = useState<ResultTone>("neutral");
  const [queryResult, setQueryResult] = useState<AdminDbQueryResponse | null>(null);
  const [importFile, setImportFile] = useState<ImportFileState | null>(null);
  const [importScript, setImportScript] = useState("");
  const [importMessage, setImportMessage] = useState("Ready. Load a file or paste a SQL script to begin.");
  const [importTone, setImportTone] = useState<ResultTone>("neutral");
  const [databaseActionMessage, setDatabaseActionMessage] = useState(
    "AutoBackup creates a fresh SQLite snapshot every hour while the application stays open. The last selected database is restored on the next launch.",
  );
  const [databaseActionTone, setDatabaseActionTone] = useState<ResultTone>("neutral");
  const [schemaSearch, setSchemaSearch] = useState("");
  const deferredSchemaSearch = useDeferredValue(schemaSearch);
  const [selectedObjectKey, setSelectedObjectKey] = useState("");
  const [selectedDatabasePath, setSelectedDatabasePath] = useState("");
  const [viewerLimit, setViewerLimit] = useState(100);
  const [isWideViewerRow, setIsWideViewerRow] = useState(() => {
    if (typeof window === "undefined") {
      return true;
    }

    return window.matchMedia(WIDE_VIEWER_ROW_MEDIA_QUERY).matches;
  });
  const [overviewCardHeight, setOverviewCardHeight] = useState<number | null>(null);

  const metadataQuery = useAdminDbMetadataQuery();
  const hashQuery = useAdminDbHashQuery();
  const queryMutation = useAdminDbQueryMutation();
  const executeMutation = useAdminDbExecuteMutation();
  const importMutation = useAdminDbImportMutation();
  const manualCopyMutation = useAdminDbManualCopyMutation();
  const selectDatabaseMutation = useAdminDbSelectDatabaseMutation();

  const metadata = metadataQuery.data ?? null;
  const databaseHash = hashQuery.data?.hash ?? null;
  const accessError = metadataQuery.error ?? hashQuery.error;
  const storageWorkspace = metadata?.storageWorkspace ?? null;
  const availableDatabases = useMemo(() => storageWorkspace?.availableDatabases ?? [], [storageWorkspace?.availableDatabases]);
  const automaticBackups = useMemo(() => storageWorkspace?.automaticBackups ?? [], [storageWorkspace?.automaticBackups]);
  const manualCopies = useMemo(() => storageWorkspace?.manualCopies ?? [], [storageWorkspace?.manualCopies]);
  const objectList = useMemo(() => metadata?.objects ?? [], [metadata?.objects]);
  const tables = useMemo(() => metadata?.tables ?? [], [metadata?.tables]);
  const normalizedSchemaSearch = deferredSchemaSearch.trim().toLowerCase();
  const filteredObjects = useMemo(
    () =>
      objectList.filter(item => {
        if (!normalizedSchemaSearch) {
          return true;
        }

        const searchableText = `${item.type} ${item.name} ${item.sql}`.toLowerCase();
        return searchableText.includes(normalizedSchemaSearch);
      }),
    [normalizedSchemaSearch, objectList],
  );

  const resolvedSelectedObjectKey = useMemo(() => {
    if (filteredObjects.length === 0) {
      return "";
    }

    if (filteredObjects.some(item => getObjectKey(item) === selectedObjectKey)) {
      return selectedObjectKey;
    }

    return getDefaultObjectKey(filteredObjects);
  }, [filteredObjects, selectedObjectKey]);

  const selectedObject = useMemo(
    () => filteredObjects.find(item => getObjectKey(item) === resolvedSelectedObjectKey) ?? null,
    [filteredObjects, resolvedSelectedObjectKey],
  );
  const selectedObjectPreviewable = selectedObject ? isPreviewableObjectType(selectedObject.type) : false;
  const selectedObjectPreviewSql = useMemo(
    () => (selectedObject && selectedObjectPreviewable ? buildPreviewSql(selectedObject, viewerLimit) : ""),
    [selectedObject, selectedObjectPreviewable, viewerLimit],
  );
  const viewerQueryEnabled = Boolean(selectedObject && selectedObjectPreviewable && selectedObjectPreviewSql);
  const viewerPreviewQuery = useQuery({
    queryKey: [...VIEWER_QUERY_KEY, selectedObject?.type ?? "", selectedObject?.name ?? "", viewerLimit],
    enabled: viewerQueryEnabled,
    cancelOnUnmount: true,
    staleTime: 30_000,
    queryFn: ({ signal }) => adminDbApi.query({ sql: selectedObjectPreviewSql }, { signal }),
  });

  const sqlMode = detectSqlMode(executorSql);
  const sqlLength = executorSql.length;
  const sqlOverLimit = metadata ? sqlLength > metadata.maxSqlLength : false;
  const importPayloadSize = useMemo(() => new Blob([importScript]).size, [importScript]);
  const importOverLimit = metadata ? importPayloadSize > metadata.maxImportBytes : false;
  const hasImportEdits = importFile ? importFile.originalText !== importScript : importScript.trim().length > 0;
  const isExecuting = queryMutation.isPending || executeMutation.isPending;
  const isImporting = importMutation.isPending;
  const isCreatingManualCopy = manualCopyMutation.isPending;
  const isSelectingDatabase = selectDatabaseMutation.isPending;
  const accessStateReady = Boolean(metadata) && !accessError;
  const accessStateMessage = accessError
    ? describeApiError(accessError, "Could not access admin database tools.")
    : metadata
      ? "Live metadata is connected directly to the admin database endpoints exposed by the current GF3 host."
      : "Connecting to admin database endpoints and loading live metadata.";
  const viewerResult = viewerPreviewQuery.data ?? null;
  const viewerTone: ResultTone =
    !selectedObject || !selectedObjectPreviewable
      ? "neutral"
      : viewerPreviewQuery.isError
        ? "error"
        : viewerResult
          ? "success"
          : "neutral";
  const viewerMessage = !selectedObject
    ? "Select a table or view to preview live records."
    : !selectedObjectPreviewable
      ? "Live row preview is available for tables and views. Indexes still show their SQL definition below."
      : viewerPreviewQuery.isLoading
        ? `Loading preview for ${selectedObject.name}...`
        : viewerPreviewQuery.isError
          ? describeApiError(viewerPreviewQuery.error, `Could not load preview rows for ${selectedObject.name}.`)
          : `Showing ${viewerResult?.rowCount ?? 0} row${viewerResult?.rowCount === 1 ? "" : "s"} from ${selectedObject.name}.`;
  const recentAutomaticBackups = automaticBackups.slice(0, 6);
  const recentManualCopies = manualCopies.slice(0, 6);
  const autoBackupsOverflowCount = Math.max(automaticBackups.length - recentAutomaticBackups.length, 0);
  const manualCopiesOverflowCount = Math.max(manualCopies.length - recentManualCopies.length, 0);

  const invalidateAdminQueries = () => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.adminDb.metadata() });
    void queryClient.invalidateQueries({ queryKey: queryKeys.adminDb.hash() });
    void queryClient.invalidateQueries({ queryKey: VIEWER_QUERY_KEY });
  };

  const invalidateAllDatabaseQueries = () => {
    void queryClient.invalidateQueries({});
  };

  const handleClearExecutorOutput = () => {
    setQueryResult(null);
    setExecutorTone("neutral");
    setExecutorMessage("Output cleared.");
  };

  const handleCreateManualCopy = () => {
    manualCopyMutation.mutate(undefined, {
      onSuccess: createdCopy => {
        setDatabaseActionTone("success");
        setDatabaseActionMessage(`Manual copy created: ${createdCopy.name}.`);
        invalidateAdminQueries();
      },
      onError: error => {
        setDatabaseActionTone("error");
        setDatabaseActionMessage(describeApiError(error, "Could not create a manual database copy."));
      },
    });
  };

  const handleSelectDatabase = () => {
    const databasePath = selectedDatabasePath.trim();

    if (!databasePath) {
      setDatabaseActionTone("error");
      setDatabaseActionMessage("Choose a database file before switching.");
      return;
    }

    if (databasePath === metadata?.databasePath) {
      setDatabaseActionTone("neutral");
      setDatabaseActionMessage("This database is already active.");
      return;
    }

    selectDatabaseMutation.mutate(
      { databasePath },
      {
        onSuccess: selectedDatabase => {
          setSelectedDatabasePath(selectedDatabase.path);
          setSelectedObjectKey("");
          setQueryResult(null);
          setSchemaSearch("");
          setDatabaseActionTone("success");
          setDatabaseActionMessage(`Active database switched to ${selectedDatabase.name}. This choice will be restored after restart.`);
          invalidateAllDatabaseQueries();
        },
        onError: error => {
          setDatabaseActionTone("error");
          setDatabaseActionMessage(describeApiError(error, "Could not switch the active database."));
        },
      },
    );
  };

  const handleExecuteSql = () => {
    const sql = executorSql.trim();

    if (!sql) {
      setExecutorTone("error");
      setExecutorMessage("SQL command is required.");
      return;
    }

    if (sqlOverLimit) {
      setExecutorTone("error");
      setExecutorMessage(`SQL exceeds the current limit of ${metadata?.maxSqlLength ?? 0} characters.`);
      return;
    }

    if (sqlMode === "unknown") {
      setExecutorTone("error");
      setExecutorMessage("Start the command with SELECT, PRAGMA, WITH, INSERT, UPDATE, or DELETE.");
      return;
    }

    if (sqlMode === "write" && metadata && !metadata.allowWriteSql) {
      setExecutorTone("error");
      setExecutorMessage("Write SQL operations are disabled by the backend configuration.");
      return;
    }

    if (sqlMode === "read") {
      queryMutation.mutate(
        { sql },
        {
          onSuccess: result => {
            setQueryResult(result);
            setExecutorTone("success");
            setExecutorMessage(buildQueryMessage(result));
          },
          onError: error => {
            setQueryResult(null);
            setExecutorTone("error");
            setExecutorMessage(describeApiError(error, "Query execution failed."));
          },
        },
      );
      return;
    }

    executeMutation.mutate(
      { sql },
      {
        onSuccess: result => {
          setQueryResult(null);
          setExecutorTone("success");
          setExecutorMessage(`Command executed. Affected rows: ${result.affectedRows}.`);
          invalidateAdminQueries();
        },
        onError: error => {
          setQueryResult(null);
          setExecutorTone("error");
          setExecutorMessage(describeApiError(error, "Write execution failed."));
        },
      },
    );
  };

  const handleChooseFile = () => {
    fileInputRef.current?.click();
  };

  const handleImportFileChange = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    event.target.value = "";

    if (!file) {
      return;
    }

    try {
      const [text, hash] = await Promise.all([file.text(), computeFileHash(file)]);
      setImportFile({
        fileName: file.name,
        originalText: text,
        size: file.size,
        lastModified: file.lastModified,
        hash,
      });
      setImportScript(text);
      setImportTone("success");
      setImportMessage("File loaded. Review or adjust the script, then run the import.");
    } catch (error) {
      setImportTone("error");
      setImportMessage(describeApiError(error, "Could not read the selected SQL file."));
    }
  };

  const handleRunImport = () => {
    const normalizedScript = importScript.trim();

    if (!normalizedScript) {
      setImportTone("error");
      setImportMessage("Import script is empty.");
      return;
    }

    if (importOverLimit) {
      setImportTone("error");
      setImportMessage(`Import payload exceeds the current limit of ${formatBytes(metadata?.maxImportBytes ?? 0)}.`);
      return;
    }

    const payloadFile = new File([importScript], importFile?.fileName ?? "manual-import.sql", {
      type: "application/sql",
      lastModified: Date.now(),
    });

    importMutation.mutate(payloadFile, {
      onSuccess: result => {
        const hasFailure = result.failedStatementIndex != null;
        setImportTone(hasFailure ? "error" : "success");
        setImportMessage(buildImportResultMessage(result));

        if (!hasFailure) {
          invalidateAdminQueries();
        }
      },
      onError: error => {
        setImportTone("error");
        setImportMessage(describeApiError(error, "Import execution failed."));
      },
    });
  };
  const visibleTables = tables.slice(0, 12);
  const hiddenTablesCount = Math.max(tables.length - visibleTables.length, 0);
  const schemaCardStyle = isWideViewerRow && overviewCardHeight ? { height: `${overviewCardHeight}px` } : undefined;

  useEffect(() => {
    if (typeof window === "undefined") {
      return;
    }

    const mediaQuery = window.matchMedia(WIDE_VIEWER_ROW_MEDIA_QUERY);
    const handleChange = () => {
      setIsWideViewerRow(mediaQuery.matches);
    };

    handleChange();

    if (typeof mediaQuery.addEventListener === "function") {
      mediaQuery.addEventListener("change", handleChange);
      return () => mediaQuery.removeEventListener("change", handleChange);
    }

    mediaQuery.addListener(handleChange);
    return () => mediaQuery.removeListener(handleChange);
  }, []);

  useEffect(() => {
    if (!metadata?.databasePath) {
      return;
    }

    setSelectedDatabasePath(metadata.databasePath);
  }, [metadata?.databasePath]);

  useLayoutEffect(() => {
    if (!isWideViewerRow) {
      return;
    }

    const overviewElement = overviewCardRef.current;
    if (!overviewElement) {
      return;
    }

    const updateHeight = () => {
      const nextHeight = Math.ceil(overviewElement.getBoundingClientRect().height);
      setOverviewCardHeight(previousHeight => (previousHeight !== nextHeight ? nextHeight : previousHeight));
    };

    updateHeight();

    const observer = new ResizeObserver(updateHeight);
    observer.observe(overviewElement);

    return () => {
      observer.disconnect();
    };
  }, [isWideViewerRow]);

  const summaryPills = [
    metadata ? `SQLite ${metadata.sqliteVersion}` : accessError ? "SQLite: unavailable" : "SQLite: loading",
    metadata ? `${tables.length} tables` : "Tables: -",
    metadata ? `${objectList.length} objects` : "Objects: -",
    metadata?.allowWriteSql ? "Write SQL enabled" : metadata ? "Write SQL locked" : "Write SQL: -",
  ];

  const accessStateClassName = [
    styles.statusBadge,
    accessStateReady ? styles.statusBadgeSuccess : styles.statusBadgeWarning,
  ].join(" ");

  const executorResultClassName = [
    styles.resultBox,
    executorTone === "success" ? styles.resultBoxSuccess : "",
    executorTone === "error" ? styles.resultBoxError : "",
  ]
    .filter(Boolean)
    .join(" ");

  const importResultClassName = [
    styles.resultBox,
    importTone === "success" ? styles.resultBoxSuccess : "",
    importTone === "error" ? styles.resultBoxError : "",
  ]
    .filter(Boolean)
    .join(" ");
  const databaseActionResultClassName = [
    styles.resultBox,
    databaseActionTone === "success" ? styles.resultBoxSuccess : "",
    databaseActionTone === "error" ? styles.resultBoxError : "",
  ]
    .filter(Boolean)
    .join(" ");
  const viewerResultClassName = [
    styles.resultBox,
    viewerTone === "success" ? styles.resultBoxSuccess : "",
    viewerTone === "error" ? styles.resultBoxError : "",
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <div className={styles.page}>
      <PageHeader
        title="Database"
        subtitle="Switch databases, manage copies, run SQL, and inspect live metadata"
        backTo="/"
        gutter={8}
      />

      {accessError ? <ErrorBanner>{describeApiError(accessError, "Could not access admin database tools.")}</ErrorBanner> : null}

      <div className={styles.workspaceGrid}>
        <CardSection
          elementRef={overviewCardRef}
          className={`${styles.card} ${styles.overviewCard}`}
          icon={<DatabaseIcon size={24} />}
          title="Database Overview"
        >
          <div className={styles.cardBody}>
            <div className={styles.overviewHero}>
              <div className={styles.overviewHeroRow}>
                <span className={accessStateClassName}>
                  {accessStateReady ? <CheckIcon size={16} /> : <WarnIcon size={16} />}
                  <span>{accessStateReady ? "Live access active" : accessError ? "Access blocked" : "Connecting"}</span>
                </span>

                <div className={styles.pillRow}>
                  {summaryPills.map(pill => (
                    <span key={pill} className={styles.infoPill}>
                      {pill}
                    </span>
                  ))}
                </div>
              </div>

              <p className={styles.accessCopy}>
                Local workspace for live GF3 database metadata, record preview, SQL execution, and portable import/export scripts.
              </p>

              <p className={styles.accessNote}>{accessStateMessage}</p>
            </div>

            {metadata ? (
              <div className={styles.overviewPanels}>
                <section className={styles.overviewPanel}>
                  <p className={styles.sectionCaption}>Connection</p>
                  <div className={styles.overviewMiniGrid}>
                    <div className={styles.overviewMiniTile}>
                      <span className={styles.detailLabel}>Access scope</span>
                      <span className={styles.detailValue}>Loopback only</span>
                    </div>
                    <div className={styles.overviewMiniTile}>
                      <span className={styles.detailLabel}>API state</span>
                      <span className={styles.detailValue}>{accessError ? "Unavailable" : metadata ? "Connected" : "Loading"}</span>
                    </div>
                    <div className={styles.overviewMiniTile}>
                      <span className={styles.detailLabel}>Write SQL</span>
                      <span className={styles.detailValue}>{metadata.allowWriteSql ? "Enabled" : "Disabled"}</span>
                    </div>
                    <div className={styles.overviewMiniTile}>
                      <span className={styles.detailLabel}>Schema objects</span>
                      <span className={styles.detailValue}>{objectList.length}</span>
                    </div>
                  </div>
                </section>

                <section className={styles.overviewPanel}>
                  <p className={styles.sectionCaption}>Database Control</p>

                  <div className={styles.controlStack}>
                    <div className={styles.controlHeader}>
                      <label className={styles.fieldLabel} htmlFor="active-database-select">
                        Working database
                      </label>

                      <div className={styles.pillRow}>
                        <span className={styles.infoPill}>AutoBackup hourly</span>
                        <span className={styles.infoPill}>Keep {storageWorkspace?.automaticBackupRetentionLimit ?? 10} copies</span>
                      </div>
                    </div>

                    <div className={styles.databaseSelectRow}>
                      <div className={styles.selectShell}>
                        <select
                          id="active-database-select"
                          className={styles.selectInput}
                          value={selectedDatabasePath}
                          onChange={event => setSelectedDatabasePath(event.target.value)}
                        >
                          {availableDatabases.length > 0 ? (
                            availableDatabases.map(entry => (
                              <option key={entry.path} value={entry.path}>
                                {buildDatabaseOptionLabel(entry)}
                              </option>
                            ))
                          ) : (
                            <option value={metadata.databasePath}>{metadata.databasePath}</option>
                          )}
                        </select>
                      </div>

                      <IosButton
                        label="Use Selected DB"
                        onClick={handleSelectDatabase}
                        disabled={isSelectingDatabase || !selectedDatabasePath || selectedDatabasePath === metadata.databasePath}
                      />
                      <IosButton
                        label="Create Manual Copy"
                        variant="secondary"
                        onClick={handleCreateManualCopy}
                        disabled={isCreatingManualCopy}
                      />
                    </div>

                    <div className={`${databaseActionResultClassName} ${styles.controlMessage}`}>{databaseActionMessage}</div>
                  </div>

                  <div className={`${styles.overviewMiniGrid} ${styles.controlMetricsGrid}`}>
                    <div className={`${styles.overviewMiniTile} ${styles.overviewMiniTileWide}`}>
                      <span className={styles.detailLabel}>Workspace root</span>
                      <span className={styles.detailValueHash}>{storageWorkspace?.workspaceRootPath || "-"}</span>
                    </div>
                    <div className={`${styles.overviewMiniTile} ${styles.overviewMiniTileWide}`}>
                      <span className={styles.detailLabel}>Auto backup folder</span>
                      <span className={styles.detailValueHash}>{storageWorkspace?.automaticBackupDirectoryPath || "-"}</span>
                    </div>
                    <div className={`${styles.overviewMiniTile} ${styles.overviewMiniTileWide}`}>
                      <span className={styles.detailLabel}>Manual copy folder</span>
                      <span className={styles.detailValueHash}>{storageWorkspace?.manualCopyDirectoryPath || "-"}</span>
                    </div>
                    <div className={styles.overviewMiniTile}>
                      <span className={styles.detailLabel}>Auto backups</span>
                      <span className={styles.detailValue}>{automaticBackups.length}</span>
                    </div>
                    <div className={styles.overviewMiniTile}>
                      <span className={styles.detailLabel}>Manual copies</span>
                      <span className={styles.detailValue}>{manualCopies.length}</span>
                    </div>
                  </div>

                  <div className={styles.fileCollections}>
                    <div className={styles.fileCollection}>
                      <p className={styles.fileCollectionTitle}>Recent Auto Backups</p>
                      {recentAutomaticBackups.length > 0 ? (
                        <div className={styles.fileStack}>
                          {recentAutomaticBackups.map(entry => (
                            <div key={entry.path} className={styles.fileRow}>
                              <div className={styles.fileRowMain}>
                                <span className={styles.fileName}>{entry.name}</span>
                                {entry.isActive ? <span className={styles.fileBadge}>Active</span> : null}
                              </div>
                              <span className={styles.fileMeta}>
                                {formatBytes(entry.fileSizeBytes)} • {formatDateTime(entry.lastModifiedUtc)}
                              </span>
                            </div>
                          ))}
                          {autoBackupsOverflowCount > 0 ? (
                            <span className={styles.emptyInline}>+{autoBackupsOverflowCount} more auto backups</span>
                          ) : null}
                        </div>
                      ) : (
                        <span className={styles.emptyInline}>The first automatic backup will appear after the app has been running for one hour.</span>
                      )}
                    </div>

                    <div className={styles.fileCollection}>
                      <p className={styles.fileCollectionTitle}>Recent Manual Copies</p>
                      {recentManualCopies.length > 0 ? (
                        <div className={styles.fileStack}>
                          {recentManualCopies.map(entry => (
                            <div key={entry.path} className={styles.fileRow}>
                              <div className={styles.fileRowMain}>
                                <span className={styles.fileName}>{entry.name}</span>
                                {entry.isActive ? <span className={styles.fileBadge}>Active</span> : null}
                              </div>
                              <span className={styles.fileMeta}>
                                {formatBytes(entry.fileSizeBytes)} • {formatDateTime(entry.lastModifiedUtc)}
                              </span>
                            </div>
                          ))}
                          {manualCopiesOverflowCount > 0 ? (
                            <span className={styles.emptyInline}>+{manualCopiesOverflowCount} more manual copies</span>
                          ) : null}
                        </div>
                      ) : (
                        <span className={styles.emptyInline}>Manual copies appear here after the first Create Manual Copy action.</span>
                      )}
                    </div>
                  </div>
                </section>

                <section className={styles.overviewPanel}>
                  <p className={styles.sectionCaption}>Storage</p>
                  <div className={styles.overviewMiniGrid}>
                    <div className={`${styles.overviewMiniTile} ${styles.overviewMiniTileWide}`}>
                      <span className={styles.detailLabel}>Database path</span>
                      <span className={styles.detailValueHash}>{metadata.databasePath || "-"}</span>
                    </div>
                    <div className={styles.overviewMiniTile}>
                      <span className={styles.detailLabel}>Size</span>
                      <span className={styles.detailValue}>{formatBytes(metadata.fileSizeBytes)}</span>
                    </div>
                    <div className={styles.overviewMiniTile}>
                      <span className={styles.detailLabel}>Last modified</span>
                      <span className={styles.detailValue}>{formatDateTime(metadata.lastModifiedUtc)}</span>
                    </div>
                    <div className={styles.overviewMiniTile}>
                      <span className={styles.detailLabel}>User version</span>
                      <span className={styles.detailValue}>{metadata.userVersion}</span>
                    </div>
                    <div className={`${styles.overviewMiniTile} ${styles.overviewMiniTileWide}`}>
                      <span className={styles.detailLabel}>Database hash</span>
                      <span className={styles.detailValueHash}>{databaseHash ?? "Loading hash..."}</span>
                    </div>
                  </div>
                </section>

                <section className={styles.overviewPanel}>
                  <p className={styles.sectionCaption}>Limits</p>
                  <div className={styles.pillRow}>
                    <span className={styles.infoPill}>SQL limit: {metadata.maxSqlLength} chars</span>
                    <span className={styles.infoPill}>Import limit: {formatBytes(metadata.maxImportBytes)}</span>
                  </div>
                </section>

                <section className={styles.overviewPanel}>
                  <p className={styles.sectionCaption}>Tables</p>
                  <div className={styles.pillRow}>
                    {visibleTables.length > 0 ? (
                      <>
                        {visibleTables.map(table => (
                          <span key={table} className={styles.infoPill}>
                            {table}
                          </span>
                        ))}
                        {hiddenTablesCount > 0 ? <span className={styles.infoPill}>+{hiddenTablesCount} more</span> : null}
                      </>
                    ) : (
                      <span className={styles.emptyInline}>No user tables found.</span>
                    )}
                  </div>
                </section>
              </div>
            ) : (
              <div className={styles.emptyState}>
                {accessError
                  ? "Database information is unavailable while admin DB tools are unreachable."
                  : "Loading live database information..."}
              </div>
            )}
          </div>
        </CardSection>

        <CardSection
          style={schemaCardStyle}
          className={`${styles.card} ${styles.schemaCard}`}
          icon={<SearchIcon size={18} />}
          title="Database Viewer"
        >
          <div className={`${styles.cardBody} ${styles.schemaCardBody}`}>
            <div className={styles.searchShell}>
              <SearchIcon size={16} className={styles.searchIcon} />
              <input
                className={styles.searchInput}
                value={schemaSearch}
                onChange={event => setSchemaSearch(event.target.value)}
                placeholder="Search tables, views, indexes, SQL..."
                aria-label="Search schema objects"
              />
            </div>

            {metadata ? (
              <>
                <div className={styles.pillRow}>
                  <span className={styles.infoPill}>{getObjectCount(objectList, "table")} tables</span>
                  <span className={styles.infoPill}>{getObjectCount(objectList, "view")} views</span>
                  <span className={styles.infoPill}>{getObjectCount(objectList, "index")} indexes</span>
                  <span className={styles.infoPill}>{filteredObjects.length} visible</span>
                </div>

                <div className={styles.viewerLayout}>
                  <div className={styles.schemaList}>
                    {filteredObjects.length > 0 ? (
                      filteredObjects.map(item => {
                        const itemKey = getObjectKey(item);
                        const isActive = itemKey === resolvedSelectedObjectKey;

                        return (
                          <button
                            key={itemKey}
                            type="button"
                            className={`${styles.schemaItem} ${isActive ? styles.schemaItemActive : ""}`}
                            onClick={() => setSelectedObjectKey(itemKey)}
                          >
                            <span className={styles.schemaType}>{item.type}</span>
                            <span className={styles.schemaName}>{item.name}</span>
                          </button>
                        );
                      })
                    ) : (
                      <div className={styles.emptyState}>No schema objects match the current search.</div>
                    )}
                  </div>

                  <div className={styles.viewerPanel}>
                    {selectedObject ? (
                      <>
                        <div className={styles.previewHeader}>
                          <span className={styles.previewBadge}>{selectedObject.type}</span>
                          <h2 className={styles.previewTitle}>{selectedObject.name}</h2>
                        </div>

                        <div className={styles.viewerSummaryGrid}>
                          <div className={styles.detailTile}>
                            <span className={styles.detailLabel}>Object type</span>
                            <span className={styles.detailValue}>{selectedObject.type}</span>
                          </div>
                          <div className={styles.detailTile}>
                            <span className={styles.detailLabel}>Preview mode</span>
                            <span className={styles.detailValue}>{selectedObjectPreviewable ? "Rows available" : "Definition only"}</span>
                          </div>
                          <div className={styles.detailTile}>
                            <span className={styles.detailLabel}>Columns shown</span>
                            <span className={styles.detailValue}>{viewerResult?.columns.length ?? "-"}</span>
                          </div>
                          <div className={styles.detailTile}>
                            <span className={styles.detailLabel}>Rows shown</span>
                            <span className={styles.detailValue}>{viewerResult?.rowCount ?? "-"}</span>
                          </div>
                        </div>

                        <div className={styles.viewerActions}>
                          {VIEWER_LIMIT_OPTIONS.map(limit => (
                            <button
                              key={limit}
                              type="button"
                              className={`${styles.presetButton} ${viewerLimit === limit ? styles.viewerLimitButtonActive : ""}`}
                              onClick={() => setViewerLimit(limit)}
                            >
                              {limit} rows
                            </button>
                          ))}
                          {selectedObjectPreviewable ? (
                            <IosButton
                              label="Use In Executor"
                              variant="secondary"
                              onClick={() => setExecutorSql(selectedObjectPreviewSql)}
                            />
                          ) : null}
                        </div>

                        <div className={viewerResultClassName}>{viewerMessage}</div>

                        {selectedObjectPreviewable ? (
                          <div className={`${styles.tableShell} ${styles.viewerTableShell}`}>
                            <div className={`${styles.tableScroll} ${styles.viewerTableScroll}`}>
                              {viewerResult ? (
                                <table className={styles.resultTable}>
                                  <thead>
                                    <tr>
                                      {viewerResult.columns.map(column => (
                                        <th key={column}>{column}</th>
                                      ))}
                                    </tr>
                                  </thead>
                                  <tbody>
                                    {viewerResult.rows.length > 0 ? (
                                      viewerResult.rows.map((row, rowIndex) => (
                                        <tr key={rowIndex}>
                                          {row.map((cell, cellIndex) => (
                                            <td key={`${rowIndex}-${cellIndex}`}>{formatCellValue(cell)}</td>
                                          ))}
                                        </tr>
                                      ))
                                    ) : (
                                      <tr>
                                        <td colSpan={Math.max(viewerResult.columns.length, 1)} className={styles.emptyTableCell}>
                                          No rows found for this object.
                                        </td>
                                      </tr>
                                    )}
                                  </tbody>
                                </table>
                              ) : (
                                <div className={styles.emptyState}>
                                  {viewerPreviewQuery.isLoading || viewerPreviewQuery.isFetching
                                    ? `Loading live rows for ${selectedObject.name}...`
                                    : viewerTone === "error"
                                      ? "Preview could not be loaded for this object."
                                      : `Preparing preview for ${selectedObject.name}...`}
                                </div>
                              )}
                            </div>
                          </div>
                        ) : null}

                        <div className={styles.viewerSqlSection}>
                          <p className={styles.sectionCaption}>SQL definition</p>
                          <pre className={styles.sqlPreview}>{selectedObject.sql || "-- No SQL definition available for this object."}</pre>
                        </div>
                      </>
                    ) : (
                      <div className={styles.emptyState}>Select an object to inspect its records and SQL definition.</div>
                    )}
                  </div>
                </div>
              </>
            ) : (
              <div className={styles.emptyState}>
                {accessError ? "Schema metadata is unavailable while admin DB tools are unreachable." : "Loading schema metadata..."}
              </div>
            )}
          </div>
        </CardSection>

        <CardSection
          className={`${styles.card} ${styles.executorCard}`}
          icon={<CodeIcon size={20} />}
          title="Executor"
          headerRightSlot={
            <div className={styles.buttonRowCompact}>
              <IosButton label="Execute" onClick={handleExecuteSql} disabled={isExecuting} />
              <IosButton label="Clear Output" variant="secondary" onClick={handleClearExecutorOutput} />
            </div>
          }
        >
          <div className={styles.cardBody}>
            <div className={styles.inlineMeta}>
              <span className={styles.metaBadge}>Mode: {sqlMode === "unknown" ? "unknown" : sqlMode}</span>
              <span className={`${styles.metaBadge} ${sqlOverLimit ? styles.metaBadgeError : ""}`}>
                {sqlLength}/{metadata?.maxSqlLength ?? "?"} chars
              </span>
            </div>

            <div className={styles.presetRow}>
              <button type="button" className={styles.presetButton} onClick={() => setExecutorSql(DEFAULT_EXECUTOR_SQL)}>
                List tables
              </button>
              <button
                type="button"
                className={styles.presetButton}
                onClick={() => setExecutorSql("SELECT type, name FROM sqlite_master WHERE type IN ('table','view','index') ORDER BY type, name;")}
              >
                List objects
              </button>
              <button type="button" className={styles.presetButton} onClick={() => setExecutorSql("PRAGMA user_version;")}>
                PRAGMA user_version
              </button>
            </div>

            <label className={styles.fieldLabel} htmlFor="executor-sql">
              SQL command
            </label>
            <textarea
              id="executor-sql"
              className={styles.codeArea}
              value={executorSql}
              onChange={event => setExecutorSql(event.target.value)}
              spellCheck={false}
            />

            <div className={executorResultClassName}>{executorMessage}</div>

            <div className={styles.tableShell}>
              {queryResult ? (
                <div className={styles.tableScroll}>
                  <table className={styles.resultTable}>
                    <thead>
                      <tr>
                        {queryResult.columns.map(column => (
                          <th key={column}>{column}</th>
                        ))}
                      </tr>
                    </thead>
                    <tbody>
                      {queryResult.rows.length > 0 ? (
                        queryResult.rows.map((row, rowIndex) => (
                          <tr key={rowIndex}>
                            {row.map((cell, cellIndex) => (
                              <td key={`${rowIndex}-${cellIndex}`}>{formatCellValue(cell)}</td>
                            ))}
                          </tr>
                        ))
                      ) : (
                        <tr>
                          <td colSpan={Math.max(queryResult.columns.length, 1)} className={styles.emptyTableCell}>
                            Query returned no rows.
                          </td>
                        </tr>
                      )}
                    </tbody>
                  </table>
                </div>
              ) : (
                <div className={styles.emptyState}>Result grid appears here after a read query.</div>
              )}
            </div>
          </div>
        </CardSection>

        <CardSection
          className={`${styles.card} ${styles.importCard}`}
          icon={<ImportIcon size={20} />}
          title="Importer"
          headerRightSlot={
            <div className={styles.buttonRowCompact}>
              <IosButton label="Choose File" variant="secondary" onClick={handleChooseFile} />
              <IosButton label="Run Import" onClick={handleRunImport} disabled={isImporting} />
            </div>
          }
        >
          <input
            ref={fileInputRef}
            className={styles.hiddenInput}
            type="file"
            accept=".sql,text/plain,application/sql"
            onChange={handleImportFileChange}
          />

          <div className={styles.cardBody}>
            <div className={styles.importHint}>
              `Export to Code` files from graph and container pages are supported here. Missing container, shop, employee, availability,
              and schedule records are inserted automatically; existing rows are skipped and reported in the result summary.
            </div>

            <div className={styles.detailsGrid}>
              <div className={styles.detailTile}>
                <span className={styles.detailLabel}>Source</span>
                <span className={styles.detailValue}>{importFile?.fileName ?? "Manual script editor"}</span>
              </div>
              <div className={styles.detailTile}>
                <span className={styles.detailLabel}>Size</span>
                <span className={styles.detailValue}>{formatBytes(importFile?.size ?? importPayloadSize)}</span>
              </div>
              <div className={styles.detailTile}>
                <span className={styles.detailLabel}>Last modified</span>
                <span className={styles.detailValue}>{formatDateTime(importFile?.lastModified ?? null)}</span>
              </div>
              <div className={styles.detailTile}>
                <span className={styles.detailLabel}>SHA-256</span>
                <span className={styles.detailValueHash}>{importFile?.hash ?? "Available after loading a file"}</span>
              </div>
              <div className={styles.detailTile}>
                <span className={styles.detailLabel}>Script status</span>
                <span className={styles.detailValue}>
                  {importFile ? (hasImportEdits ? "Edited after load" : "Matches selected file") : importScript.trim() ? "Manual script ready" : "No script selected"}
                </span>
              </div>
              <div className={styles.detailTile}>
                <span className={styles.detailLabel}>Limit</span>
                <span className={`${styles.detailValue} ${importOverLimit ? styles.limitError : ""}`}>
                  {formatBytes(importPayloadSize)} / {metadata ? formatBytes(metadata.maxImportBytes) : "-"}
                </span>
              </div>
            </div>

            <label className={styles.fieldLabel} htmlFor="import-script">
              Import script
            </label>
            <textarea
              id="import-script"
              className={`${styles.codeArea} ${styles.importArea}`}
              value={importScript}
              onChange={event => setImportScript(event.target.value)}
              placeholder="Choose a .sql file or paste an import script here."
              spellCheck={false}
            />

            <div className={importResultClassName}>{importMessage}</div>
          </div>
        </CardSection>
      </div>
    </div>
  );
}
