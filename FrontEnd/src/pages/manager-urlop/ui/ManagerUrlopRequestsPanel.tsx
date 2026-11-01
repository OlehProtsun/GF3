import { useState } from "react";
import { formatUrlopDayRanges, UrlopStatusLabel, type UrlopRequest } from "@entities/urlop";
import { t } from "@shared/i18n";
import { CheckIcon, CloseIcon, ClockIcon, InboxIcon, ScheduleIcon } from "@shared/ui/icons";
import styles from "./ManagerUrlopRequestsPanel.module.css";

type Props = {
  loading?: boolean; requests: readonly UrlopRequest[]; isOpen: boolean; busy: boolean; readOnly: boolean;
  onSetMonthOpen: (nextIsOpen: boolean) => void; onApprove: (requestId: string) => void;
  onReject: (requestId: string) => void; onEdit: (request: UrlopRequest) => void; onDelete: (requestId: string) => void;
};
export function ManagerUrlopRequestsPanel({ requests, isOpen, loading = false, busy, readOnly, onSetMonthOpen, onApprove, onReject, onEdit, onDelete }: Props) {
  const [all, setAll] = useState(false);
  const unique = [...new Map(requests.map(request => [request.requestId, request])).values()];
  const pendingCount = unique.filter(request => request.status === "pending").length;
  const visible = unique.filter(request => all || request.status === "pending").sort((a, b) => (all ? -1 : 1) * a.createdAtUtc.localeCompare(b.createdAtUtc) || a.requestId.localeCompare(b.requestId));
  const disabled = busy || readOnly || loading;
  return <section className={styles.panel} aria-label={t("Vacation requests")} aria-busy={loading}>
    <header className={styles.header}><span className={styles.icon} aria-hidden="true"><ClockIcon size={23} /></span><div><h2>{t("Vacation requests")}</h2><p>{t("Pending requests for this month.")}</p></div><span className={styles.count} aria-label={`${t("Pending")}: ${pendingCount}`}>{pendingCount}</span></header>
    <div className={styles.filters} role="group" aria-label={t("Vacation requests")}><button type="button" aria-pressed={!all} onClick={() => setAll(false)}>{t("Pending")}</button><button type="button" aria-pressed={all} onClick={() => setAll(true)}>{t("All")}</button></div>
    <div className={styles.status}><span role="status" data-open={isOpen}>{t(isOpen ? "Open for requests" : "Month closed by manager.")}</span><button type="button" disabled={disabled} onClick={() => onSetMonthOpen(!isOpen)}>{t(isOpen ? "Close requests" : "Open requests")}</button></div>
    <div className={styles.list}>
      {loading ? <div className={styles.empty} role="status">{t("Loading")}</div> : null}
      {!loading && !visible.length ? <div className={styles.empty}><span aria-hidden="true"><InboxIcon size={27} /></span><p>{t(all ? "No vacation requests." : "No pending vacation requests.")}</p></div> : visible.map(request => {
        const days = [...new Set(request.days)].sort((a, b) => a - b);
        return <article className={styles.request} key={request.requestId}>
          <span className={styles.avatar} data-tone={request.employeeId % 4} aria-hidden="true">{request.employeeName.split(/\s+/).filter(Boolean).slice(0, 2).map(part => part[0]).join("")}</span>
          <div className={styles.copy}><strong>{request.employeeName}</strong><UrlopStatusLabel status={request.status} />
            <p className={styles.dates}><ScheduleIcon size={14} aria-hidden="true" /><span>{formatUrlopDayRanges(request.year, request.month, days)}</span></p>
            <small>{t(days.length === 1 ? "1 day" : "{0} days", days.length)} · {t(request.createdByManagerId ? "Created by manager" : "Created by employee")}</small>
          </div>
          <div className={styles.footer}>
            <div className={styles.secondaryActions}>{request.status !== "rejected" ? <button type="button" disabled={disabled} onClick={event => { event.currentTarget.focus(); onEdit(request); }}>{t("Edit dates")}</button> : null}<button type="button" disabled={disabled} onClick={() => onDelete(request.requestId)}>{t("Delete")}</button></div>
            {request.status === "pending" ? <div className={styles.primaryActions}><button className={styles.approve} type="button" aria-label={t("Approve")} title={t("Approve")} disabled={disabled} onClick={() => onApprove(request.requestId)}><CheckIcon size={18} aria-hidden="true" />{t("Approve")}</button><button className={styles.reject} type="button" aria-label={t("Reject")} title={t("Reject")} disabled={disabled} onClick={() => onReject(request.requestId)}><CloseIcon size={18} aria-hidden="true" />{t("Reject")}</button></div> : null}
          </div>
        </article>;
      })}
    </div>
  </section>;
}
