import { useMemo, useState } from "react";
import { PageHeader } from "@shared/ui/PageHeader";
import { useWorkflowLogsQuery, type WorkflowLog } from "@entities/workflow-logs";
import { getErrorMessage } from "@shared/api/httpClient";
import { InformationIcon } from "@shared/ui/icons";
import styles from "./InformationPage.module.css";

type WorkflowLogDay = {
  key: string;
  label: string;
  fullLabel: string;
  logs: WorkflowLog[];
  sortValue: number;
};

const dayTabFormatter = new Intl.DateTimeFormat("en-US", {
  day: "2-digit",
  month: "short",
});
const fullDayFormatter = new Intl.DateTimeFormat("en-US", {
  weekday: "long",
  day: "2-digit",
  month: "long",
  year: "numeric",
});
const logTimeFormatter = new Intl.DateTimeFormat("en-US", {
  hour: "2-digit",
  minute: "2-digit",
  second: "2-digit",
  hour12: false,
});

function parseWorkflowLogDate(value: string) {
  const date = new Date(value);

  return Number.isNaN(date.getTime()) ? null : date;
}

function getWorkflowDayKey(date: Date | null) {
  if (!date) {
    return "unknown";
  }

  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");

  return `${year}-${month}-${day}`;
}

function formatWorkflowLogTime(value: string) {
  const date = parseWorkflowLogDate(value);
  if (!date) {
    return value;
  }

  return logTimeFormatter.format(date);
}

function getRoleLabel(role: string) {
  return role.toLowerCase() === "employee" ? "Employee" : "Manager";
}

function groupWorkflowLogsByDay(logs: WorkflowLog[]) {
  const groups = new Map<string, WorkflowLogDay>();

  logs.forEach(log => {
    const date = parseWorkflowLogDate(log.occurredAtUtc);
    const key = getWorkflowDayKey(date);
    const existing = groups.get(key);

    if (existing) {
      existing.logs.push(log);
      return;
    }

    groups.set(key, {
      key,
      label: date ? dayTabFormatter.format(date) : "Unknown",
      fullLabel: date ? fullDayFormatter.format(date) : "Unknown date",
      logs: [log],
      sortValue: date ? new Date(date.getFullYear(), date.getMonth(), date.getDate()).getTime() : 0,
    });
  });

  return [...groups.values()]
    .map(group => ({
      ...group,
      logs: [...group.logs].sort(
        (left, right) =>
          (parseWorkflowLogDate(right.occurredAtUtc)?.getTime() ?? 0) -
          (parseWorkflowLogDate(left.occurredAtUtc)?.getTime() ?? 0),
      ),
    }))
    .sort((left, right) => right.sortValue - left.sortValue);
}

export function InformationPage() {
  const logsQuery = useWorkflowLogsQuery();
  const logs = logsQuery.data ?? [];
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
