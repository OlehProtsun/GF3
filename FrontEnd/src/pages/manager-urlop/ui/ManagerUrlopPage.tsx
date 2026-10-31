import { useState } from "react";
import { useAuth } from "@app/providers/AuthProvider";
import { useShopsListQuery } from "@entities/shops";
import { useEmployeesListQuery } from "@entities/employees";
import { useManagerUrlopMonthQuery, useUrlopMutation, urlopApi, UrlopMonthCalendar, UrlopStatusLabel, isoDate, occupiedDays, toggleDay, validSelection, warsawToday, type UrlopRequest } from "@entities/urlop";
import { t } from "@shared/i18n";
import { ApiError, getErrorMessage } from "@shared/api/httpClient";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { PageHeader } from "@shared/ui/PageHeader";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import { useUnsavedChangesPrompt } from "@shared/lib/useUnsavedChangesPrompt";
import styles from "./ManagerUrlopPage.module.css";

type Operation = { kind: "month"; isOpen: boolean } | { kind: "save" } | { kind: "approve" | "reject" | "remove"; id: string };
export function ManagerUrlopPage() {
  const { session } = useAuth();
  const shops = useShopsListQuery();
  const employees = useEmployeesListQuery();
  const [shop, setShop] = useState(0);
  const [period, setPeriod] = useState(warsawToday().slice(0, 7));
  const [year, month] = period.split("-").map(Number);
  const shopId = shop || shops.data?.[0]?.id || 0;
  const query = useManagerUrlopMonthQuery(shopId, year, month);
  const [employee, setEmployee] = useState(0);
  const employeeId = employee || employees.data?.[0]?.id || 0;
  const [selected, setSelected] = useState<number[]>([]);
  const [editing, setEditing] = useState<UrlopRequest | null>(null);
  const [deleting, setDeleting] = useState<string | null>(null);
  const readOnly = session?.workspaceMode === "phone";
  const dirty = selected.join(",") !== (editing?.days ?? []).join(",");
  const { confirmIfNeeded, dialog } = useUnsavedChangesPrompt({ when: dirty });
  const reset = () => { setSelected([]); setEditing(null); };
  const mutation = useUrlopMutation(async (op: Operation) => {
    if (readOnly) return;
    if (op.kind === "month") return urlopApi.setMonth({ shopId, year, month, isOpen: op.isOpen });
    if (op.kind === "approve") return urlopApi.approve(op.id);
    if (op.kind === "reject") return urlopApi.reject(op.id);
    if (op.kind === "remove") return urlopApi.remove(op.id);
    if (editing) return urlopApi.replace(editing.requestId, selected);
    return urlopApi.create({ shopId, employeeId, year, month, days: selected });
  });
  const blocked = occupiedDays(query.data?.requests.filter(x => x.employeeId === (editing?.employeeId ?? employeeId) && x.requestId !== editing?.requestId) ?? []);
  const error = mutation.error || query.error || shops.error || employees.error;
  const conflicts = error instanceof ApiError ? (error.details as { conflictingDates?: string[]; scheduleIds?: number[] } | undefined) : undefined;
  const busy = mutation.isPending || readOnly;
  return <div className={styles.page}>
    <PageHeader title={t("Urlop")} />
    {error ? <ErrorBanner>{getErrorMessage(error)} {conflicts?.conflictingDates?.join(", ")} {conflicts?.scheduleIds?.length ? `${t("Schedule")}: ${conflicts.scheduleIds.join(", ")}` : ""}</ErrorBanner> : null}
    {shops.isLoading || employees.isLoading || query.isLoading ? <p>{t("Loading")}</p> : null}
    {!shops.isLoading && !shops.data?.length ? <p>{t("No shops available for vacation requests.")}</p> : null}
    <div className={styles.controls}>
      <label>{t("Shop")}<select aria-label={t("Shop")} value={shopId} disabled={busy} onChange={e => { const id = Number(e.target.value); confirmIfNeeded(() => { setShop(id); reset(); }); }}>
        {shops.data?.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}
      </select></label>
      <label>{t("Month")}<input type="month" aria-label={t("Month")} min="0001-01" max="9999-12" value={period} disabled={busy} onChange={e => { const value = e.target.value; if (value) confirmIfNeeded(() => { setPeriod(value); reset(); }); }} /></label>
      <IosButton label={t(query.data?.isOpen ? "Close requests" : "Open requests")} variant="secondary" disabled={busy || !query.data}
        onClick={() => mutation.mutate({ kind: "month", isOpen: !query.data?.isOpen })} />
    </div>
    {query.data ? <>
      <p role="status">{t(query.data.isOpen ? "Open for requests" : "Month closed by manager.")}</p>
      <section className={styles.panel}>
        <h2>{t(editing ? "Edit dates" : "Create approved vacation")}</h2>
        <label>{t("Employee")}<select aria-label={t("Employee")} disabled={busy || !!editing} value={editing?.employeeId ?? employeeId} onChange={e => { const id = Number(e.target.value); confirmIfNeeded(() => { setEmployee(id); reset(); }); }}>
          {employees.data?.map(x => <option key={x.id} value={x.id}>{x.firstName} {x.lastName}</option>)}
        </select></label>
        <UrlopMonthCalendar year={year} month={month} approved={query.data.approvedDays} selected={selected} readOnly={busy || !employeeId}
          disabledDays={blocked} onToggle={day => setSelected(toggleDay(selected, day))} />
        <div className={styles.controls}>
          <IosButton label={t("Clear selection")} variant="secondary" disabled={busy} onClick={() => setSelected([])} />
          {editing ? <IosButton label={t("Cancel")} variant="secondary" disabled={busy} onClick={() => confirmIfNeeded(reset)} /> : null}
          <IosButton label={t(editing ? "Save" : "Create approved vacation")} disabled={busy || !employeeId || !validSelection(year, month, selected, blocked)}
            onClick={() => mutation.mutate({ kind: "save" }, { onSuccess: reset })} />
        </div>
      </section>
      <h2>{t("Vacation requests")}</h2>
      {!query.data.requests.length ? <p>{t("No vacation requests.")}</p> : query.data.requests.map(x => <article className={styles.panel} key={x.requestId}>
        <strong>{x.employeeName}</strong> <UrlopStatusLabel status={x.status} />
        <p>{x.days.map(day => isoDate(x.year, x.month, day)).join(", ")}</p>
        <small>{t(x.createdByManagerId ? "Created by manager" : "Created by employee")} · {x.createdAtUtc}</small>
        <div className={styles.controls}>
          {x.status === "pending" ? <>
            <IosButton label={t("Approve")} disabled={busy} onClick={() => mutation.mutate({ kind: "approve", id: x.requestId })} />
            <IosButton label={t("Reject")} disabled={busy} variant="secondary" onClick={() => mutation.mutate({ kind: "reject", id: x.requestId })} />
          </> : null}
          {x.status !== "rejected" ? <IosButton label={t("Edit dates")} disabled={busy} variant="secondary" onClick={() => confirmIfNeeded(() => { setEditing(x); setSelected([...x.days]); })} /> : null}
          <IosButton label={t("Delete")} disabled={busy} variant="secondary" onClick={() => setDeleting(x.requestId)} />
        </div>
      </article>)}
    </> : null}
    <ConfirmDialog open={!!deleting} title={t("Delete vacation request?")} message={t("This deletes the request and all its vacation days.")}
      confirmDisabled={busy} cancelDisabled={mutation.isPending} onCancel={() => setDeleting(null)} onConfirm={() => {
        if (deleting) mutation.mutate({ kind: "remove", id: deleting }, { onSuccess: () => { if (editing?.requestId === deleting) reset(); setDeleting(null); } });
      }} />
    {dialog}
  </div>;
}
