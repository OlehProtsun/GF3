import { t, translateServerMessage } from "@shared/i18n";
import { useMemo, useState } from "react";
import {
  useBulkDeleteWorkflowLogsMutation,
  useDeleteWorkflowLogMutation,
  useUpdateWorkflowLogSettingsMutation,
  useWorkflowLogSettingsQuery,
  useWorkflowLogsQuery,
  type WorkflowLog,
  type WorkflowLogAudience,
  type WorkflowLogBulkDeleteRequest,
} from "@entities/workflow-logs";
import { AvailabilityDateTimeField } from "@entities/availability-groups";
import { getErrorMessage } from "@shared/api/httpClient";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { PageHeader } from "@shared/ui/PageHeader";
import { CloseIcon, InformationIcon, SearchIcon } from "@shared/ui/icons";
import {
  formatWorkflowLogTime,
  getRoleLabel,
  getWorkflowLogDayBounds,
  getWorkflowLogRangeBounds,
  groupWorkflowLogsByDay,
} from "../model/workflowLogDays";
import styles from "./InformationPage.module.css";

const emptyLogs: WorkflowLog[] = [];
const audienceOptions: Array<{ value: WorkflowLogAudience; label: string }> = [
  { value: "all", get label() { return t("Everyone"); } },
  { value: "managers", get label() { return t("Managers"); } },
  { value: "employees", get label() { return t("Employees"); } },
];

type DeleteIntent =
  | { kind: "entry"; id: number; title: string; message: string; confirmText: string }
  | { kind: "bulk"; payload: WorkflowLogBulkDeleteRequest; title: string; message: string; confirmText: string };

export function InformationPage() {
  const logsQuery = useWorkflowLogsQuery();
  const settingsQuery = useWorkflowLogSettingsQuery();
  const updateSettingsMutation = useUpdateWorkflowLogSettingsMutation();
  const deleteLogMutation = useDeleteWorkflowLogMutation();
  const bulkDeleteMutation = useBulkDeleteWorkflowLogsMutation();
  const logs = logsQuery.data ?? emptyLogs;
  const settings = settingsQuery.data;
  const [selectedDayKey, setSelectedDayKey] = useState<string | null>(null);
  const [rangeFrom, setRangeFrom] = useState("");
  const [rangeTo, setRangeTo] = useState("");
  const [logSearch, setLogSearch] = useState("");
  const [deleteIntent, setDeleteIntent] = useState<DeleteIntent | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const logDays = useMemo(() => groupWorkflowLogsByDay(logs), [logs]);
  const selectedDay = logDays.find(day => day.key === selectedDayKey) ?? logDays[0] ?? null;
  const filteredSelectedDayLogs = useMemo(() => {
    const query = logSearch.trim().toLocaleLowerCase();
    if (!query) {
      return selectedDay?.logs ?? [];
    }

    const compactQuery = query.replace(/\s|:/g, "");
    return (selectedDay?.logs ?? []).filter(log => {
      const time = formatWorkflowLogTime(log.occurredAtUtc).toLocaleLowerCase();
      return log.actorName.toLocaleLowerCase().includes(query) || time.includes(query) || time.replace(/\s|:/g, "").includes(compactQuery);
    });
  }, [logSearch, selectedDay]);
  const deletePending = deleteLogMutation.isPending || bulkDeleteMutation.isPending;

  const updateSettings = (isEnabled: boolean, audience: WorkflowLogAudience) => {
    setActionError(null);
    updateSettingsMutation.mutate(
      { isEnabled, audience },
      { onError: error => setActionError(getErrorMessage(error, t("Could not update logging settings."))) },
    );
  };

  const requestDeleteDay = () => {
    if (!selectedDay) {
      return;
    }

    const bounds = getWorkflowLogDayBounds(selectedDay.key);
    if (!bounds) {
      setActionError(t("This day cannot be deleted because its date is unknown."));
      return;
    }

    setActionError(null);
    setDeleteIntent({
      kind: "bulk",
      payload: { deleteAll: false, ...bounds },
      title: t("Delete this day"),
      message: t("Delete all {0} log entries from {1}?", selectedDay.logs.length, selectedDay.fullLabel),
      confirmText: t("Delete day"),
    });
  };

  const requestDeleteRange = () => {
    const bounds = getWorkflowLogRangeBounds(rangeFrom, rangeTo);
    if (!bounds) {
      setActionError(t("Choose a valid From and Through date."));
      return;
    }

    setActionError(null);
    setDeleteIntent({
      kind: "bulk",
      payload: { deleteAll: false, ...bounds },
      title: t("Delete date range"),
      message: t("Delete all workflow logs from {0} through {1}, inclusive?", rangeFrom, rangeTo),
      confirmText: t("Delete range"),
    });
  };

  const confirmDelete = () => {
    if (!deleteIntent) {
      return;
    }

    setActionError(null);
    const options = {
      onSuccess: () => setDeleteIntent(null),
      onError: (error: unknown) => {
        setActionError(getErrorMessage(error, t("Could not delete workflow logs.")));
        setDeleteIntent(null);
      },
    };

    if (deleteIntent.kind === "entry") {
      deleteLogMutation.mutate(deleteIntent.id, options);
      return;
    }

    bulkDeleteMutation.mutate(deleteIntent.payload, options);
  };

  return (
    <div className={styles.page}>
      <PageHeader title={t("Information")} subtitle={t("Employee and manager workflow activity")} backTo={-1} />

      <section className={styles.panel}>
        <div className={styles.panelHeader}>
          <div className={styles.panelTitle}>
            <span className={styles.panelIcon} aria-hidden="true">
              <InformationIcon size={18} />
            </span>
            <div>
              <span className={styles.eyebrow}>{t("Workflow logs")}</span>
              <strong>{t("Live activity")}</strong>
            </div>
          </div>

          <div className={styles.headerPills}>
            <span className={settings?.isEnabled === false ? styles.pausedPill : styles.livePill}>
              {logsQuery.isFetching ? t("Syncing") : settings?.isEnabled === false ? t("Paused") : t("Live")}
            </span>
            <span className={styles.countPill}>{logs.length}</span>
          </div>
        </div>

        <div className={styles.managementGrid}>
          <div className={styles.managementCard}>
            <div className={styles.managementHeading}>
              <div>
                <span className={styles.eyebrow}>{t("Recording")}</span>
                <strong>{t("Logging control")}</strong>
              </div>
              <button
                type="button"
                className={[styles.toggleButton, settings?.isEnabled ? styles.toggleButtonActive : ""].filter(Boolean).join(" ")}
                aria-pressed={settings?.isEnabled ?? false}
                disabled={!settings || updateSettingsMutation.isPending}
                onClick={() => settings && updateSettings(!settings.isEnabled, settings.audience)}
              >
                {settings?.isEnabled ? t("Logging on") : t("Logging off")}
              </button>
            </div>

            <div className={styles.audienceGroup} role="group" aria-label={t("Choose who is logged")}>
              {audienceOptions.map(option => (
                <button
                  key={option.value}
                  type="button"
                  className={settings?.audience === option.value ? styles.audienceButtonActive : styles.audienceButton}
                  aria-pressed={settings?.audience === option.value}
                  disabled={!settings || updateSettingsMutation.isPending}
                  onClick={() => settings && updateSettings(settings.isEnabled, option.value)}
                >
                  {option.label}
                </button>
              ))}
            </div>
            <p className={styles.managementHint}>
              {t("Turning logging off keeps existing history. Audience changes apply only to new activity.")}</p>
          </div>

          <div className={styles.managementCard}>
            <div className={styles.managementHeading}>
              <div>
                <span className={styles.eyebrow}>{t("History")}</span>
                <strong>{t("Delete by date")}</strong>
              </div>
              <button
                type="button"
                className={styles.dangerButton}
                disabled={logs.length === 0 || deletePending}
                onClick={() => {
                  setActionError(null);
                  setDeleteIntent({
                    kind: "bulk",
                    payload: { deleteAll: true },
                    title: t("Delete all history"),
                    message: t("Delete every workflow log entry? This cannot be undone."),
                    confirmText: t("Delete all"),
                  });
                }}
              >
                {t("Delete all")}</button>
            </div>

            <div className={styles.rangeControls}>
              <AvailabilityDateTimeField
                id="workflow-log-range-from"
                label={t("From")}
                value={rangeFrom}
                defaultTime="00:00"
                dateOnly
                onChange={setRangeFrom}
              />
              <AvailabilityDateTimeField
                id="workflow-log-range-through"
                label={t("Through")}
                value={rangeTo}
                defaultTime="00:00"
                dateOnly
                onChange={setRangeTo}
              />
              <button type="button" className={styles.rangeDeleteButton} disabled={deletePending} onClick={requestDeleteRange}>
                {t("Delete range")}</button>
            </div>
            <p className={styles.managementHint}>{t("Dates use your local timezone and include the entire Through day.")}</p>
          </div>
        </div>

        {actionError ? <div className={styles.errorText}>{actionError}</div> : null}
        {settingsQuery.error ? (
          <div className={styles.errorText}>{getErrorMessage(settingsQuery.error, t("Could not load logging settings."))}</div>
        ) : null}

        {logsQuery.isLoading ? (
          <div className={styles.stateText}>{t("Loading logs...")}</div>
        ) : logsQuery.error ? (
          <div className={styles.stateText}>{getErrorMessage(logsQuery.error, t("Could not load workflow logs."))}</div>
        ) : logs.length === 0 ? (
          <div className={styles.stateText}>{t("No workflow activity yet.")}</div>
        ) : (
          <div className={styles.activityLayout}>
            <div className={styles.dayRailShell}>
              <div className={styles.dayRail} aria-label={t("Workflow log days")}>
                {logDays.map(day => {
                  const isSelected = day.key === selectedDay?.key;

                  return (
                    <button
                      key={day.key}
                      type="button"
                      className={[styles.dayButton, isSelected ? styles.dayButtonActive : ""].filter(Boolean).join(" ")}
                      aria-pressed={isSelected}
                      onClick={() => setSelectedDayKey(day.key)}
                    >
                      <span>{day.label}</span>
                      <strong>{day.logs.length}</strong>
                    </button>
                  );
                })}
              </div>
            </div>

            <div className={styles.timelinePanel}>
              <div className={styles.timelineHeader}>
                <div>
                  <span className={styles.eyebrow}>{t("Selected day")}</span>
                  <strong>{selectedDay?.fullLabel ?? t("No day selected")}</strong>
                </div>
                <div className={styles.timelineHeaderActions}>
                  <span className={styles.countPill}>{filteredSelectedDayLogs.length}</span>
                  <button type="button" className={styles.dangerButton} disabled={!selectedDay || deletePending} onClick={requestDeleteDay}>
                    {t("Delete day")}</button>
                </div>
              </div>

              <label className={styles.logSearch}>
                <SearchIcon size={17} />
                <input
                  type="search"
                  value={logSearch}
                  placeholder={t("Search by name or time")}
                  aria-label={t("Search logs by actor name or time")}
                  onChange={event => setLogSearch(event.target.value)}
                />
              </label>

              <div className={styles.timelineList}>
                {filteredSelectedDayLogs.map(log => (
                  <article key={log.id} className={styles.timelineItem}>
                    <time className={styles.timelineTime} dateTime={log.occurredAtUtc}>
                      {formatWorkflowLogTime(log.occurredAtUtc)}
                    </time>
                    <span className={styles.timelineDot} aria-hidden="true" />
                    <div className={styles.timelineContent}>
                      <div className={styles.actorLine}>
                        <strong>{log.actorName}</strong>
                        <span
                          className={[
                            styles.roleBadge,
                            log.actorRole.toLowerCase() === "employee" ? styles.roleBadgeEmployee : styles.roleBadgeManager,
                          ].filter(Boolean).join(" ")}
                        >
                          {getRoleLabel(log.actorRole)}
                        </span>
                      </div>
                      <p>{translateServerMessage(log.action)}</p>
                    </div>
                    <button
                      type="button"
                      className={styles.logDeleteButton}
                      disabled={deletePending}
                      aria-label={t("Delete log from {0} at {1}", log.actorName, formatWorkflowLogTime(log.occurredAtUtc))}
                      onClick={() => {
                        setActionError(null);
                        setDeleteIntent({
                          kind: "entry",
                          id: log.id,
                          title: t("Delete log entry"),
                          message: t("Delete this {0} log from {1}?", getRoleLabel(log.actorRole).toLowerCase(), log.actorName),
                          confirmText: t("Delete entry"),
                        });
                      }}
                    >
                      <CloseIcon size={15} />
                    </button>
                  </article>
                ))}
                {selectedDay && filteredSelectedDayLogs.length === 0 ? (
                  <div className={styles.emptySearch}>{t("No logs match this search.")}</div>
                ) : null}
              </div>
            </div>
          </div>
        )}
      </section>

      <ConfirmDialog
        open={deleteIntent !== null}
        title={deleteIntent?.title ?? t("Delete workflow logs")}
        message={deleteIntent?.message ?? t("Delete the selected workflow logs?")}
        confirmText={deletePending ? t("Deleting...") : deleteIntent?.confirmText ?? t("Delete")}
        confirmDisabled={deletePending}
        cancelDisabled={deletePending}
        onCancel={() => setDeleteIntent(null)}
        onConfirm={confirmDelete}
      />
    </div>
  );
}
