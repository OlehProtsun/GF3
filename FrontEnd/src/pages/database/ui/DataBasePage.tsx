import { getLocale, t } from "@shared/i18n";
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
import { RegulationsAdminPanel } from "@entities/regulations/ui/RegulationsAdminPanel";

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

  return date.toLocaleString(getLocale());
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
      return t("Admin DB access is blocked by the current backend policy. Start GF3 through the launcher or enable remote admin access on the host machine.");
    }

    if (error.status === 404) {
      return t("Admin DB tools are disabled in the current Web API configuration.");
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
    t("Executable statements: {0}.", result.statementsExecuted),
    t("Applied: {0}.", result.statementsApplied),
    t("Already existed: {0}.", result.statementsAlreadyExisted),
    t("Service lines skipped: {0}.", result.serviceStatementsSkipped),
  ];

  if (result.failedStatementIndex == null) {
    return t("Import completed. {0}", parts.join(" "));
  }

  const reason = result.failureReason ? t(" Reason: {0}.", result.failureReason) : "";
  return t("Import rolled back at statement #{0}. {1}{2}", result.failedStatementIndex + 1, parts.join(" "), reason);
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
    return t("Query completed. No rows returned.");
  }

  return t("Query completed. Rows: {0}.", result.rowCount);
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
      return t("Auto backup");
    case "manualCopy":
      return t("Manual copy");
    default:
      return t("Database");
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
            <span className={styles.accessEyebrow}>{t("Developer access")}</span>
            <h2 id={titleId}>{t("Unlock Database")}</h2>
            <p id={descriptionId}>{t("Enter the developer password configured on the GF3 host.")}</p>
          </div>
        </div>

        <label className={styles.accessField} htmlFor="database-developer-password">
          <span>{t("Developer password")}</span>
          <input
            id="database-developer-password"
            type="password"
            value={password}
            autoComplete="off"
            autoFocus
            placeholder={t("Enter password")}
            disabled={isSubmitting}
            onChange={event => onPasswordChange(event.target.value)}
          />
        </label>

        {error ? <div className={styles.accessError} role="alert">{error}</div> : null}

        <div className={styles.accessActions}>
          <IosButton
            label={t("Go back")}
            type="button"
            variant="secondary"
            customColor="#e5e7eb"
            customBorderColor="#d1d5db"
            disabled={isSubmitting}
            onClick={onCancel}
          />
          <IosButton
            label={isSubmitting ? t("Checking...") : t("Unlock database")}
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
      setAccessError(error instanceof ApiError ? error.message : t("Could not verify the developer password."));
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
  const [executorMessage, setExecutorMessage] = useState(t("Ready."));
  const [executorTone, setExecutorTone] = useState<ResultTone>("neutral");
  const [queryResult, setQueryResult] = useState<AdminDbQueryResponse | null>(null);
  const [importFile, setImportFile] = useState<ImportFileState | null>(null);
  const [importScript, setImportScript] = useState("");
  const [importMessage, setImportMessage] = useState(t("Ready. Load a file or paste a SQL script to begin."));
  const [importTone, setImportTone] = useState<ResultTone>("neutral");
  const [databaseActionMessage, setDatabaseActionMessage] = useState(
    t("AutoBackup creates a fresh SQLite snapshot every hour while the application stays open. The last selected database is restored on the next launch."),
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
    ? describeApiError(accessError, t("Could not access admin database tools."))
    : metadata
      ? t("Live metadata is connected directly to the admin database endpoints exposed by the current GF3 host.")
      : t("Connecting to admin database endpoints and loading live metadata.");
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
    ? t("Select a table or view to preview live records.")
    : !selectedObjectPreviewable
      ? t("Live row preview is available for tables and views. Indexes still show their SQL definition below.")
      : viewerPreviewQuery.isLoading
        ? t("Loading preview for {0}...", selectedObject.name)
        : viewerPreviewQuery.isError
          ? describeApiError(viewerPreviewQuery.error, t("Could not load preview rows for {0}.", selectedObject.name))
          : t("Showing {0} row{1} from {2}.", viewerResult?.rowCount ?? 0, viewerResult?.rowCount === 1 ? "" : "s", selectedObject.name);
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
    setExecutorMessage(t("Output cleared."));
  };

  const handleCreateManualCopy = () => {
    manualCopyMutation.mutate(undefined, {
      onSuccess: createdCopy => {
        setDatabaseActionTone("success");
        setDatabaseActionMessage(t("Manual copy created: {0}.", createdCopy.name));
        invalidateAdminQueries();
      },
      onError: error => {
        setDatabaseActionTone("error");
        setDatabaseActionMessage(describeApiError(error, t("Could not create a manual database copy.")));
      },
    });
  };

  const handleSelectDatabase = () => {
    const databasePath = selectedDatabasePath.trim();

    if (!databasePath) {
      setDatabaseActionTone("error");
      setDatabaseActionMessage(t("Choose a database file before switching."));
      return;
    }

    if (databasePath === metadata?.databasePath) {
      setDatabaseActionTone("neutral");
      setDatabaseActionMessage(t("This database is already active."));
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
          setDatabaseActionMessage(t("Active database switched to {0}. This choice will be restored after restart.", selectedDatabase.name));
          invalidateAllDatabaseQueries();
        },
        onError: error => {
          setDatabaseActionTone("error");
          setDatabaseActionMessage(describeApiError(error, t("Could not switch the active database.")));
        },
      },
    );
  };

  const handleExecuteSql = () => {
    const sql = executorSql.trim();

    if (!sql) {
      setExecutorTone("error");
      setExecutorMessage(t("SQL command is required."));
      return;
    }

    if (sqlOverLimit) {
      setExecutorTone("error");
      setExecutorMessage(t("SQL exceeds the current limit of {0} characters.", metadata?.maxSqlLength ?? 0));
      return;
    }

    if (sqlMode === "unknown") {
      setExecutorTone("error");
      setExecutorMessage(t("Start the command with SELECT, PRAGMA, WITH, INSERT, UPDATE, or DELETE."));
      return;
    }

    if (sqlMode === "write" && metadata && !metadata.allowWriteSql) {
      setExecutorTone("error");
      setExecutorMessage(t("Write SQL operations are disabled by the backend configuration."));
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
            setExecutorMessage(describeApiError(error, t("Query execution failed.")));
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
          setExecutorMessage(t("Command executed. Affected rows: {0}.", result.affectedRows));
          invalidateAdminQueries();
        },
        onError: error => {
          setQueryResult(null);
          setExecutorTone("error");
          setExecutorMessage(describeApiError(error, t("Write execution failed.")));
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
      setImportMessage(t("File loaded. Review or adjust the script, then run the import."));
    } catch (error) {
      setImportTone("error");
      setImportMessage(describeApiError(error, t("Could not read the selected SQL file.")));
    }
  };

  const handleRunImport = () => {
    const normalizedScript = importScript.trim();

    if (!normalizedScript) {
      setImportTone("error");
      setImportMessage(t("Import script is empty."));
      return;
    }

    if (importOverLimit) {
      setImportTone("error");
      setImportMessage(t("Import payload exceeds the current limit of {0}.", formatBytes(metadata?.maxImportBytes ?? 0)));
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
        setImportMessage(describeApiError(error, t("Import execution failed.")));
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
    metadata ? `SQLite ${metadata.sqliteVersion}` : accessError ? t("SQLite: unavailable") : t("SQLite: loading"),
    metadata ? t("{0} tables", tables.length) : t("Tables: -"),
    metadata ? t("{0} objects", objectList.length) : t("Objects: -"),
    metadata?.allowWriteSql ? t("Write SQL enabled") : metadata ? t("Write SQL locked") : t("Write SQL: -"),
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
        title={t("Database")}
        subtitle={t("Switch databases, manage copies, run SQL, and inspect live metadata")}
        backTo="/"
        gutter={8}
      />

      {accessError ? <ErrorBanner>{describeApiError(accessError, t("Could not access admin database tools."))}</ErrorBanner> : null}

      <RegulationsAdminPanel />

      <div className={styles.workspaceGrid}>
        <CardSection
          elementRef={overviewCardRef}
          className={`${styles.card} ${styles.overviewCard}`}
          icon={<DatabaseIcon size={24} />}
          title={t("Database Overview")}
        >
          <div className={styles.cardBody}>
            <div className={styles.overviewHero}>
              <div className={styles.overviewHeroRow}>
                <span className={accessStateClassName}>
                  {accessStateReady ? <CheckIcon size={16} /> : <WarnIcon size={16} />}
                  <span>{accessStateReady ? t("Live access active") : accessError ? t("Access blocked") : t("Connecting")}</span>
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
                {t("Local workspace for live GF3 database metadata, record preview, SQL execution, and portable import/export scripts.")}</p>

              <p className={styles.accessNote}>{accessStateMessage}</p>
            </div>

            {metadata ? (
              <div className={styles.overviewPanels}>
                <section className={styles.overviewPanel}>
                  <p className={styles.sectionCaption}>{t("Connection")}</p>
                  <div className={styles.overviewMiniGrid}>
                    <div className={styles.overviewMiniTile}>
                      <span className={styles.detailLabel}>{t("Access scope")}</span>
                      <span className={styles.detailValue}>{t("Loopback only")}</span>
                    </div>
                    <div className={styles.overviewMiniTile}>
                      <span className={styles.detailLabel}>{t("API state")}</span>
                      <span className={styles.detailValue}>{accessError ? t("Unavailable") : metadata ? t("Connected") : t("Loading")}</span>
                    </div>
                    <div className={styles.overviewMiniTile}>
                      <span className={styles.detailLabel}>{t("Write SQL")}</span>
                      <span className={styles.detailValue}>{metadata.allowWriteSql ? t("Enabled") : t("Disabled")}</span>
                    </div>
                    <div className={styles.overviewMiniTile}>
                      <span className={styles.detailLabel}>{t("Schema objects")}</span>
                      <span className={styles.detailValue}>{objectList.length}</span>
                    </div>
                  </div>
                </section>

                <section className={styles.overviewPanel}>
                  <p className={styles.sectionCaption}>{t("Database Control")}</p>

                  <div className={styles.controlStack}>
                    <div className={styles.controlHeader}>
                      <label className={styles.fieldLabel} htmlFor="active-database-select">
                        {t("Working database")}</label>

                      <div className={styles.pillRow}>
                        <span className={styles.infoPill}>{t("AutoBackup hourly")}</span>
                        <span className={styles.infoPill}>{t("Keep")} {storageWorkspace?.automaticBackupRetentionLimit ?? 10}  {t("copies")}</span>
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
                        label={t("Use Selected DB")}
                        onClick={handleSelectDatabase}
                        disabled={isSelectingDatabase || !selectedDatabasePath || selectedDatabasePath === metadata.databasePath}
                      />
                      <IosButton
                        label={t("Create Manual Copy")}
                        variant="secondary"
                        onClick={handleCreateManualCopy}
                        disabled={isCreatingManualCopy}
                      />
                    </div>

                    <div className={`${databaseActionResultClassName} ${styles.controlMessage}`}>{databaseActionMessage}</div>
                  </div>

                  <div className={`${styles.overviewMiniGrid} ${styles.controlMetricsGrid}`}>
                    <div className={`${styles.overviewMiniTile} ${styles.overviewMiniTileWide}`}>
                      <span className={styles.detailLabel}>{t("Workspace root")}</span>
                      <span className={styles.detailValueHash}>{storageWorkspace?.workspaceRootPath || "-"}</span>
                    </div>
                    <div className={`${styles.overviewMiniTile} ${styles.overviewMiniTileWide}`}>
                      <span className={styles.detailLabel}>{t("Auto backup folder")}</span>
                      <span className={styles.detailValueHash}>{storageWorkspace?.automaticBackupDirectoryPath || "-"}</span>
                    </div>
                    <div className={`${styles.overviewMiniTile} ${styles.overviewMiniTileWide}`}>
                      <span className={styles.detailLabel}>{t("Manual copy folder")}</span>
                      <span className={styles.detailValueHash}>{storageWorkspace?.manualCopyDirectoryPath || "-"}</span>
                    </div>
                    <div className={styles.overviewMiniTile}>
                      <span className={styles.detailLabel}>{t("Auto backups")}</span>
                      <span className={styles.detailValue}>{automaticBackups.length}</span>
                    </div>
                    <div className={styles.overviewMiniTile}>
                      <span className={styles.detailLabel}>{t("Manual copies")}</span>
                      <span className={styles.detailValue}>{manualCopies.length}</span>
                    </div>
                  </div>

                  <div className={styles.fileCollections}>
                    <div className={styles.fileCollection}>
                      <p className={styles.fileCollectionTitle}>{t("Recent Auto Backups")}</p>
                      {recentAutomaticBackups.length > 0 ? (
                        <div className={styles.fileStack}>
                          {recentAutomaticBackups.map(entry => (
                            <div key={entry.path} className={styles.fileRow}>
                              <div className={styles.fileRowMain}>
                                <span className={styles.fileName}>{entry.name}</span>
                                {entry.isActive ? <span className={styles.fileBadge}>{t("Active")}</span> : null}
                              </div>
                              <span className={styles.fileMeta}>
                                {formatBytes(entry.fileSizeBytes)} • {formatDateTime(entry.lastModifiedUtc)}
                              </span>
                            </div>
                          ))}
                          {autoBackupsOverflowCount > 0 ? (
                            <span className={styles.emptyInline}>+{autoBackupsOverflowCount}  {t("more auto backups")}</span>
                          ) : null}
                        </div>
                      ) : (
                        <span className={styles.emptyInline}>{t("The first automatic backup will appear after the app has been running for one hour.")}</span>
                      )}
                    </div>

                    <div className={styles.fileCollection}>
                      <p className={styles.fileCollectionTitle}>{t("Recent Manual Copies")}</p>
                      {recentManualCopies.length > 0 ? (
                        <div className={styles.fileStack}>
                          {recentManualCopies.map(entry => (
                            <div key={entry.path} className={styles.fileRow}>
                              <div className={styles.fileRowMain}>
                                <span className={styles.fileName}>{entry.name}</span>
                                {entry.isActive ? <span className={styles.fileBadge}>{t("Active")}</span> : null}
                              </div>
                              <span className={styles.fileMeta}>
                                {formatBytes(entry.fileSizeBytes)} • {formatDateTime(entry.lastModifiedUtc)}
                              </span>
                            </div>
                          ))}
                          {manualCopiesOverflowCount > 0 ? (
                            <span className={styles.emptyInline}>+{manualCopiesOverflowCount}  {t("more manual copies")}</span>
                          ) : null}
                        </div>
                      ) : (
                        <span className={styles.emptyInline}>{t("Manual copies appear here after the first Create Manual Copy action.")}</span>
                      )}
                    </div>
                  </div>
                </section>

                <section className={styles.overviewPanel}>
                  <p className={styles.sectionCaption}>{t("Storage")}</p>
                  <div className={styles.overviewMiniGrid}>
                    <div className={`${styles.overviewMiniTile} ${styles.overviewMiniTileWide}`}>
                      <span className={styles.detailLabel}>{t("Database path")}</span>
                      <span className={styles.detailValueHash}>{metadata.databasePath || "-"}</span>
                    </div>
                    <div className={styles.overviewMiniTile}>
                      <span className={styles.detailLabel}>{t("Size")}</span>
                      <span className={styles.detailValue}>{formatBytes(metadata.fileSizeBytes)}</span>
                    </div>
                    <div className={styles.overviewMiniTile}>
                      <span className={styles.detailLabel}>{t("Last modified")}</span>
                      <span className={styles.detailValue}>{formatDateTime(metadata.lastModifiedUtc)}</span>
                    </div>
                    <div className={styles.overviewMiniTile}>
                      <span className={styles.detailLabel}>{t("User version")}</span>
                      <span className={styles.detailValue}>{metadata.userVersion}</span>
                    </div>
                    <div className={`${styles.overviewMiniTile} ${styles.overviewMiniTileWide}`}>
                      <span className={styles.detailLabel}>{t("Database hash")}</span>
                      <span className={styles.detailValueHash}>{databaseHash ?? t("Loading hash...")}</span>
                    </div>
                  </div>
                </section>

                <section className={styles.overviewPanel}>
                  <p className={styles.sectionCaption}>{t("Limits")}</p>
                  <div className={styles.pillRow}>
                    <span className={styles.infoPill}>{t("SQL limit:")} {metadata.maxSqlLength}  {t("chars")}</span>
                    <span className={styles.infoPill}>{t("Import limit:")} {formatBytes(metadata.maxImportBytes)}</span>
                  </div>
                </section>

                <section className={styles.overviewPanel}>
                  <p className={styles.sectionCaption}>{t("Tables")}</p>
                  <div className={styles.pillRow}>
                    {visibleTables.length > 0 ? (
                      <>
                        {visibleTables.map(table => (
                          <span key={table} className={styles.infoPill}>
                            {table}
                          </span>
                        ))}
                        {hiddenTablesCount > 0 ? <span className={styles.infoPill}>+{hiddenTablesCount}  {t("more")}</span> : null}
                      </>
                    ) : (
                      <span className={styles.emptyInline}>{t("No user tables found.")}</span>
                    )}
                  </div>
                </section>
              </div>
            ) : (
              <div className={styles.emptyState}>
                {accessError
                  ? t("Database information is unavailable while admin DB tools are unreachable.")
                  : t("Loading live database information...")}
              </div>
            )}
          </div>
        </CardSection>

        <CardSection
          style={schemaCardStyle}
          className={`${styles.card} ${styles.schemaCard}`}
          icon={<SearchIcon size={18} />}
          title={t("Database Viewer")}
        >
          <div className={`${styles.cardBody} ${styles.schemaCardBody}`}>
            <div className={styles.searchShell}>
              <SearchIcon size={16} className={styles.searchIcon} />
              <input
                className={styles.searchInput}
                value={schemaSearch}
                onChange={event => setSchemaSearch(event.target.value)}
                placeholder={t("Search tables, views, indexes, SQL...")}
                aria-label={t("Search schema objects")}
              />
            </div>

            {metadata ? (
              <>
                <div className={styles.pillRow}>
                  <span className={styles.infoPill}>{getObjectCount(objectList, "table")}  {t("tables")}</span>
                  <span className={styles.infoPill}>{getObjectCount(objectList, "view")}  {t("views")}</span>
                  <span className={styles.infoPill}>{getObjectCount(objectList, "index")}  {t("indexes")}</span>
                  <span className={styles.infoPill}>{filteredObjects.length}  {t("visible")}</span>
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
                      <div className={styles.emptyState}>{t("No schema objects match the current search.")}</div>
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
                            <span className={styles.detailLabel}>{t("Object type")}</span>
                            <span className={styles.detailValue}>{selectedObject.type}</span>
                          </div>
                          <div className={styles.detailTile}>
                            <span className={styles.detailLabel}>{t("Preview mode")}</span>
                            <span className={styles.detailValue}>{selectedObjectPreviewable ? t("Rows available") : t("Definition only")}</span>
                          </div>
                          <div className={styles.detailTile}>
                            <span className={styles.detailLabel}>{t("Columns shown")}</span>
                            <span className={styles.detailValue}>{viewerResult?.columns.length ?? "-"}</span>
                          </div>
                          <div className={styles.detailTile}>
                            <span className={styles.detailLabel}>{t("Rows shown")}</span>
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
                              {limit}  {t("rows")}</button>
                          ))}
                          {selectedObjectPreviewable ? (
                            <IosButton
                              label={t("Use In Executor")}
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
                                          {t("No rows found for this object.")}</td>
                                      </tr>
                                    )}
                                  </tbody>
                                </table>
                              ) : (
                                <div className={styles.emptyState}>
                                  {viewerPreviewQuery.isLoading || viewerPreviewQuery.isFetching
                                    ? t("Loading live rows for {0}...", selectedObject.name)
                                    : viewerTone === "error"
                                      ? t("Preview could not be loaded for this object.")
                                      : t("Preparing preview for {0}...", selectedObject.name)}
                                </div>
                              )}
                            </div>
                          </div>
                        ) : null}

                        <div className={styles.viewerSqlSection}>
                          <p className={styles.sectionCaption}>{t("SQL definition")}</p>
                          <pre className={styles.sqlPreview}>{selectedObject.sql || "-- No SQL definition available for this object."}</pre>
                        </div>
                      </>
                    ) : (
                      <div className={styles.emptyState}>{t("Select an object to inspect its records and SQL definition.")}</div>
                    )}
                  </div>
                </div>
              </>
            ) : (
              <div className={styles.emptyState}>
                {accessError ? t("Schema metadata is unavailable while admin DB tools are unreachable.") : t("Loading schema metadata...")}
              </div>
            )}
          </div>
        </CardSection>

        <CardSection
          className={`${styles.card} ${styles.executorCard}`}
          icon={<CodeIcon size={20} />}
          title={t("Executor")}
          headerRightSlot={
            <div className={styles.buttonRowCompact}>
              <IosButton label={t("Execute")} onClick={handleExecuteSql} disabled={isExecuting} />
              <IosButton label={t("Clear Output")} variant="secondary" onClick={handleClearExecutorOutput} />
            </div>
          }
        >
          <div className={styles.cardBody}>
            <div className={styles.inlineMeta}>
              <span className={styles.metaBadge}>{t("Mode:")} {sqlMode === "unknown" ? "unknown" : sqlMode}</span>
              <span className={`${styles.metaBadge} ${sqlOverLimit ? styles.metaBadgeError : ""}`}>
                {sqlLength}/{metadata?.maxSqlLength ?? "?"}  {t("chars")}</span>
            </div>

            <div className={styles.presetRow}>
              <button type="button" className={styles.presetButton} onClick={() => setExecutorSql(DEFAULT_EXECUTOR_SQL)}>
                {t("List tables")}</button>
              <button
                type="button"
                className={styles.presetButton}
                onClick={() => setExecutorSql("SELECT type, name FROM sqlite_master WHERE type IN ('table','view','index') ORDER BY type, name;")}
              >
                {t("List objects")}</button>
              <button type="button" className={styles.presetButton} onClick={() => setExecutorSql("PRAGMA user_version;")}>
                PRAGMA user_version
              </button>
            </div>

            <label className={styles.fieldLabel} htmlFor="executor-sql">
              {t("SQL command")}</label>
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
                            {t("Query returned no rows.")}</td>
                        </tr>
                      )}
                    </tbody>
                  </table>
                </div>
              ) : (
                <div className={styles.emptyState}>{t("Result grid appears here after a read query.")}</div>
              )}
            </div>
          </div>
        </CardSection>

        <CardSection
          className={`${styles.card} ${styles.importCard}`}
          icon={<ImportIcon size={20} />}
          title={t("Importer")}
          headerRightSlot={
            <div className={styles.buttonRowCompact}>
              <IosButton label={t("Choose File")} variant="secondary" onClick={handleChooseFile} />
              <IosButton label={t("Run Import")} onClick={handleRunImport} disabled={isImporting} />
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
              {t("`Export to Code` files from graph and container pages are supported here. Missing container, shop, employee, availability, and schedule records are inserted automatically; existing rows are skipped and reported in the result summary.")}</div>

            <div className={styles.detailsGrid}>
              <div className={styles.detailTile}>
                <span className={styles.detailLabel}>{t("Source")}</span>
                <span className={styles.detailValue}>{importFile?.fileName ?? t("Manual script editor")}</span>
              </div>
              <div className={styles.detailTile}>
                <span className={styles.detailLabel}>{t("Size")}</span>
                <span className={styles.detailValue}>{formatBytes(importFile?.size ?? importPayloadSize)}</span>
              </div>
              <div className={styles.detailTile}>
                <span className={styles.detailLabel}>{t("Last modified")}</span>
                <span className={styles.detailValue}>{formatDateTime(importFile?.lastModified ?? null)}</span>
              </div>
              <div className={styles.detailTile}>
                <span className={styles.detailLabel}>SHA-256</span>
                <span className={styles.detailValueHash}>{importFile?.hash ?? t("Available after loading a file")}</span>
              </div>
              <div className={styles.detailTile}>
                <span className={styles.detailLabel}>{t("Script status")}</span>
                <span className={styles.detailValue}>
                  {importFile ? (hasImportEdits ? t("Edited after load") : t("Matches selected file")) : importScript.trim() ? t("Manual script ready") : t("No script selected")}
                </span>
              </div>
              <div className={styles.detailTile}>
                <span className={styles.detailLabel}>{t("Limit")}</span>
                <span className={`${styles.detailValue} ${importOverLimit ? styles.limitError : ""}`}>
                  {formatBytes(importPayloadSize)} / {metadata ? formatBytes(metadata.maxImportBytes) : "-"}
                </span>
              </div>
            </div>

            <label className={styles.fieldLabel} htmlFor="import-script">
              {t("Import script")}</label>
            <textarea
              id="import-script"
              className={`${styles.codeArea} ${styles.importArea}`}
              value={importScript}
              onChange={event => setImportScript(event.target.value)}
              placeholder={t("Choose a .sql file or paste an import script here.")}
              spellCheck={false}
            />

            <div className={importResultClassName}>{importMessage}</div>
          </div>
        </CardSection>
      </div>
    </div>
  );
}
