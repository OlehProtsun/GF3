import { PageHeader } from "@shared/ui/PageHeader";
import { useWorkflowLogsQuery } from "@entities/workflow-logs";
import { getErrorMessage } from "@shared/api/httpClient";
import { InformationIcon } from "@shared/ui/icons";
import styles from "./InformationPage.module.css";

const logDateFormatter = new Intl.DateTimeFormat("uk-UA", {
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
  second: "2-digit",
});

function formatWorkflowLogDate(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return logDateFormatter.format(date);
}

export function InformationPage() {
  const logsQuery = useWorkflowLogsQuery();
  const logs = logsQuery.data ?? [];

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
          <div className={styles.logList}>
            {logs.map(log => (
              <div key={log.id} className={styles.logRow}>
                <span>{`[${formatWorkflowLogDate(log.occurredAtUtc)}]-[${log.actorName}]-{${log.action}}`}</span>
              </div>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
