import { useMemo, useState } from "react";
import { PageHeader } from "@shared/ui/PageHeader";
import { useWorkflowLogsQuery, type WorkflowLog } from "@entities/workflow-logs";
import { getErrorMessage } from "@shared/api/httpClient";
import { InformationIcon } from "@shared/ui/icons";
import { formatWorkflowLogTime, getRoleLabel, groupWorkflowLogsByDay } from "../model/workflowLogDays";
import styles from "./InformationPage.module.css";

const emptyLogs: WorkflowLog[] = [];

export function InformationPage() {
  const logsQuery = useWorkflowLogsQuery();
  const logs = logsQuery.data ?? emptyLogs;
  const [selectedDayKey, setSelectedDayKey] = useState<string | null>(null);
  const logDays = useMemo(() => groupWorkflowLogsByDay(logs), [logs]);
  const selectedDay = logDays.find(day => day.key === selectedDayKey) ?? logDays[0] ?? null;

  return (
    <div className={styles.page}>
      <PageHeader title="Information" subtitle="Employee and manager workflow activity" backTo={-1} />

      <section className={styles.panel}>
        <div className={styles.panelHeader}>
          <div className={styles.panelTitle}>
            <span className={styles.panelIcon} aria-hidden="true">
              <InformationIcon size={18} />
            </span>
            <div>
              <span className={styles.eyebrow}>Workflow logs</span>
              <strong>Live activity</strong>
            </div>
          </div>

          <div className={styles.headerPills}>
            <span className={styles.livePill}>{logsQuery.isFetching ? "Syncing" : "Live"}</span>
            <span className={styles.countPill}>{logs.length}</span>
          </div>
        </div>

        {logsQuery.isLoading ? (
          <div className={styles.stateText}>Loading logs...</div>
        ) : logsQuery.error ? (
          <div className={styles.stateText}>{getErrorMessage(logsQuery.error, "Could not load workflow logs.")}</div>
        ) : logs.length === 0 ? (
          <div className={styles.stateText}>No workflow activity yet.</div>
        ) : (
          <div className={styles.activityLayout}>
            <div className={styles.dayRail} aria-label="Workflow log days">
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

            <div className={styles.timelinePanel}>
              <div className={styles.timelineHeader}>
                <div>
                  <span className={styles.eyebrow}>Selected day</span>
                  <strong>{selectedDay?.fullLabel ?? "No day selected"}</strong>
                </div>
                <span className={styles.countPill}>{selectedDay?.logs.length ?? 0}</span>
              </div>

              <div className={styles.timelineList}>
                {selectedDay?.logs.map(log => (
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
                      <p>{log.action}</p>
                    </div>
                  </article>
                ))}
              </div>
            </div>
          </div>
        )}
      </section>
    </div>
  );
}
