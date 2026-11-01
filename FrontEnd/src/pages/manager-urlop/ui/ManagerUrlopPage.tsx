import { useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { useParams } from "react-router-dom";
import { useAuth } from "@app/providers/AuthProvider";
import { useEmployeesListQuery } from "@entities/employees";
import { useShopByIdQuery } from "@entities/shops";
import { useManagerUrlopMonthQuery, useUrlopMutation, urlopApi, UrlopMonthCalendar, isoDate, occupiedDays, toggleDay, validSelection, warsawToday, type UrlopRequest } from "@entities/urlop";
import { dateTimeFormat, t } from "@shared/i18n";
import { ApiError, getErrorMessage } from "@shared/api/httpClient";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { PageHeader } from "@shared/ui/PageHeader";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import { ChevronLeftIcon, ChevronRightIcon, PlusIcon, ShopIcon, ScheduleIcon } from "@shared/ui/icons";
import { useUnsavedChangesPrompt } from "@shared/lib/useUnsavedChangesPrompt";
import { ManagerUrlopOverviewCalendar } from "./ManagerUrlopOverviewCalendar";
import { ManagerUrlopRequestsPanel } from "./ManagerUrlopRequestsPanel";
import styles from "./ManagerUrlopPage.module.css";

type Operation = { kind: "month"; isOpen: boolean } | { kind: "save" } | { kind: "approve" | "reject" | "remove"; id: string };
export function ManagerUrlopPage() {
  const { session } = useAuth();
  const { shopId: routeShopId } = useParams<{ shopId: string }>();
  const parsedShopId = Number(routeShopId);
  const shopId = Number.isSafeInteger(parsedShopId) && parsedShopId > 0 ? parsedShopId : 0;
  const shopQuery = useShopByIdQuery(shopId || null);
  const employees = useEmployeesListQuery();
  const [period, setPeriod] = useState(warsawToday().slice(0, 7));
  const [year, month] = period.split("-").map(Number);
  const query = useManagerUrlopMonthQuery(shopId, year, month);
  const data = !query.isError && query.data?.shopId === shopId && query.data.year === year && query.data.month === month ? query.data : undefined;
  const [employee, setEmployee] = useState(0);
  const employeeId = employee || employees.data?.[0]?.id || 0;
  const [selected, setSelected] = useState<number[]>([]);
  const [editing, setEditing] = useState<UrlopRequest | null>(null);
  const [editorContext, setEditorContext] = useState("");
  const editorOpen = editorContext === `${shopId}:${period}`;
  const [mutationError, setMutationError] = useState<unknown>(null);
  const [deleting, setDeleting] = useState<string | null>(null);
  const [editorShop, setEditorShop] = useState(shopId);
  if (editorShop !== shopId) {
    setEditorShop(shopId); setEditorContext(""); setEditing(null); setSelected([]); setDeleting(null); setMutationError(null);
  }
  const readOnly = session?.workspaceMode === "phone";
  const addButton = useRef<HTMLButtonElement>(null);
  const invoker = useRef<HTMLElement | null>(null);
  const layer = useRef<HTMLDivElement>(null);
  const dirty = [...selected].sort((a, b) => a - b).join(",") !== [...(editing?.days ?? [])].sort((a, b) => a - b).join(",");
  const reset = () => { setSelected([]); setEditing(null); setEditorContext(""); };
  const mutation = useUrlopMutation(async (op: Operation) => {
    if (readOnly || !shopId || !data) return;
    if (op.kind === "month") return urlopApi.setMonth({ shopId, year, month, isOpen: op.isOpen });
    if (op.kind === "approve") return urlopApi.approve(op.id);
    if (op.kind === "reject") return urlopApi.reject(op.id);
    if (op.kind === "remove") return urlopApi.remove(op.id);
    if (editing) return urlopApi.replace(editing.requestId, selected);
    return urlopApi.create({ shopId, employeeId, year, month, days: selected });
  });
  const mutate: typeof mutation.mutate = (operation, callbacks) => {
    setMutationError(null);
    mutation.mutate(operation, { ...callbacks, onError: error => { setMutationError(error); callbacks?.onError?.(error); } });
  };
  const { confirmIfNeeded, dialog } = useUnsavedChangesPrompt({ when: editorOpen && dirty && !mutation.isPending });
  const confirming = dialog.props.open;
  const blocked = occupiedDays(data?.requests.filter(x => x.employeeId === (editing?.employeeId ?? employeeId) && x.requestId !== editing?.requestId) ?? []);
  const error = mutationError || query.error || employees.error;
  const conflicts = error instanceof ApiError ? (error.details as { conflictingDates?: string[]; scheduleIds?: number[] } | undefined) : undefined;
  const errorBanner = error ? <ErrorBanner>{getErrorMessage(error)} {conflicts?.conflictingDates?.join(", ")} {conflicts?.scheduleIds?.length ? `${t("Schedule")}: ${conflicts.scheduleIds.join(", ")}` : ""}</ErrorBanner> : null;
  const busy = mutation.isPending || readOnly;
  const closeEditor = () => { if (!mutation.isPending) confirmIfNeeded(() => { reset(); setMutationError(null); }); };
  const openEditor = (request?: UrlopRequest) => {
    if (busy || !data || request?.status === "rejected") return;
    invoker.current = document.activeElement as HTMLElement;
    setMutationError(null); setEditing(request ?? null); setSelected([...(request?.days ?? [])]); setEditorContext(`${shopId}:${period}`);
  };
  const changePeriod = (nextYear: number, nextMonth: number) => {
    if (mutation.isPending || nextYear < 1 || nextYear > 9999) return;
    confirmIfNeeded(() => { reset(); setMutationError(null); setPeriod(isoDate(nextYear, nextMonth, 1).slice(0, 7)); });
  };
  useEffect(() => {
    if (!editorOpen) return;
    const previousOverflow = document.body.style.overflow;
    const fallbackFocus = addButton.current;
    document.body.style.overflow = "hidden";
    const siblings = [...document.body.children].filter((element): element is HTMLElement => element instanceof HTMLElement && element !== layer.current);
    const previousInert = siblings.map(element => element.inert);
    siblings.forEach(element => { element.inert = true; });
    return () => {
      document.body.style.overflow = previousOverflow;
      siblings.forEach((element, index) => { element.inert = previousInert[index]; });
      const target = invoker.current?.isConnected ? invoker.current : fallbackFocus;
      target?.focus();
    };
  }, [editorOpen]);
  useEffect(() => {
    if (!editorOpen) return;
    const dialogs = layer.current?.querySelectorAll<HTMLElement>('[role="dialog"]');
    const activeDialog = dialogs?.[dialogs.length - 1];
    activeDialog?.querySelector<HTMLElement>('select:not(:disabled), button:not(:disabled)')?.focus();
  }, [editorOpen, confirming]);
  const firstYear = Math.max(1, Math.min(9989, year - 5));
  const years = Array.from({ length: 11 }, (_, i) => firstYear + i);
  return <div className={styles.page}>
    <PageHeader title={t("Urlop")} backTo="/urlop" subtitle={t("Manage vacation requests for the selected shop.")} />
    {!shopId ? <ErrorBanner>{t("Shop not found.")}</ErrorBanner> : null}
    {!editorOpen ? errorBanner : null}
    {shopId > 0 ? <>
      {data ? <div className={styles.identity}><span aria-hidden="true"><ShopIcon size={25} style={{ transform: "scaleY(-1)" }} /></span><div><h2>{data.shopName}</h2><p>{shopQuery.data?.id === shopId && shopQuery.data.address ? shopQuery.data.address : t("Shop vacation management")}</p></div></div> : null}
      <div className={styles.hero}><div className={styles.intro}><h1>{t("Monthly vacation schedule")}</h1><p>{t("Overview of approved vacations and requests for this shop.")}</p></div>
        <div className={styles.toolbar}>
          <label><span className={styles.srOnly}>{t("Month")}</span><select aria-label={t("Month")} value={month} disabled={busy} onChange={e => changePeriod(year, Number(e.target.value))}>{Array.from({ length: 12 }, (_, i) => <option key={i} value={i + 1}>{dateTimeFormat("en-GB", { month: "long" }).format(new Date(2000, i, 1))}</option>)}</select></label>
          <label><span className={styles.srOnly}>{t("Year")}</span><select aria-label={t("Year")} value={year} disabled={busy} onChange={e => changePeriod(Number(e.target.value), month)}>{years.map(value => <option value={value} key={value}>{value}</option>)}</select></label>
          <button type="button" className={styles.arrow} aria-label={t("Previous month")} disabled={busy || year === 1 && month === 1} onClick={() => changePeriod(month === 1 ? year - 1 : year, month === 1 ? 12 : month - 1)}><ChevronLeftIcon size={20} /></button>
          <button type="button" className={styles.arrow} aria-label={t("Next month")} disabled={busy || year === 9999 && month === 12} onClick={() => changePeriod(month === 12 ? year + 1 : year, month === 12 ? 1 : month + 1)}><ChevronRightIcon size={20} /></button>
          <button ref={addButton} type="button" className={styles.add} disabled={busy || !data || employees.isLoading || !employees.data?.length} onClick={event => { event.currentTarget.focus(); openEditor(); }}><PlusIcon size={20} />{t("Add vacation")}</button>
        </div>
      </div>
      {query.isLoading ? <div className={styles.dashboard} role="status" aria-label={t("Loading")}><div className={styles.skeleton}>{t("Loading")}</div><div className={styles.skeleton} /></div> : null}
      {data ? <div className={styles.dashboard}>
        <ManagerUrlopOverviewCalendar key={`${shopId}:${period}`} year={year} month={month} approvedDays={data.approvedDays} />
        <ManagerUrlopRequestsPanel requests={data.requests} isOpen={data.isOpen} busy={mutation.isPending} readOnly={readOnly} onSetMonthOpen={isOpen => mutate({ kind: "month", isOpen })}
          onApprove={id => mutate({ kind: "approve", id })} onReject={id => mutate({ kind: "reject", id })} onEdit={openEditor} onDelete={setDeleting} />
      </div> : null}
    </> : null}
    {createPortal(<div ref={layer} onKeyDown={event => {
      if (!editorOpen) return;
      if (event.key === "Escape" && !confirming) { event.stopPropagation(); closeEditor(); }
      if (event.key !== "Tab") return;
      const dialogs = layer.current!.querySelectorAll<HTMLElement>('[role="dialog"]');
      const activeDialog = dialogs[dialogs.length - 1];
      const controls = [...activeDialog.querySelectorAll<HTMLElement>('button:not(:disabled), select:not(:disabled), [tabindex="0"]')];
      if (!controls.length) { event.preventDefault(); return; }
      const first = controls[0], last = controls[controls.length - 1];
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last?.focus(); }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus(); }
    }}>
      {editorOpen ? <div className={styles.overlay} inert={confirming} onMouseDown={event => { if (event.target === event.currentTarget) closeEditor(); }}>
        <section className={styles.editor} role="dialog" aria-modal="true" aria-labelledby="vacation-editor-title">
          <h2 id="vacation-editor-title">{t(editing ? "Edit dates" : "Add vacation")}</h2>
          <p className={styles.editorContext}><ScheduleIcon size={17} aria-hidden="true" />{dateTimeFormat("en-GB", { month: "long", year: "numeric" }).format(new Date(`${isoDate(year, month, 1)}T12:00:00`))}<span aria-hidden="true">·</span>{data?.shopName}</p>
          {errorBanner}
          <label className={styles.employee}>{t("Employee")}<select aria-label={t("Employee")} disabled={busy || !!editing} value={editing?.employeeId ?? employeeId} onChange={e => { const id = Number(e.target.value); confirmIfNeeded(() => { setEmployee(id); setSelected([]); }); }}>
            {editing && !employees.data?.some(x => x.id === editing.employeeId) ? <option value={editing.employeeId}>{editing.employeeName}</option> : null}
            {employees.data?.map(x => <option key={x.id} value={x.id}>{x.firstName} {x.lastName}</option>)}
          </select></label>
          <div className={styles.editorCalendar}><UrlopMonthCalendar year={year} month={month} approved={data?.approvedDays} selected={selected} readOnly={busy || !(editing?.employeeId ?? employeeId)} disabledDays={blocked} onToggle={day => setSelected(toggleDay(selected, day))} /></div>
          <div className={styles.editorActions}>
            <IosButton className={styles.editorSecondary} label={t("Clear selection")} variant="secondary" disabled={busy} onClick={() => setSelected([])} />
            <IosButton className={styles.editorSecondary} label={t("Cancel")} variant="secondary" disabled={mutation.isPending} onClick={closeEditor} />
            <IosButton className={styles.editorPrimary} label={t(editing ? "Save" : "Create approved vacation")} disabled={busy || !data || !(editing?.employeeId ?? employeeId) || !validSelection(year, month, selected, blocked)} onClick={() => mutate({ kind: "save" }, { onSuccess: () => { reset(); setMutationError(null); } })} />
          </div>
        </section>
      </div> : null}
      <ConfirmDialog open={!!deleting && !editorOpen} title={t("Delete vacation request?")} message={t("This deletes the request and all its vacation days.")}
        confirmDisabled={busy} cancelDisabled={mutation.isPending} onCancel={() => setDeleting(null)} onConfirm={() => {
          if (deleting) mutate({ kind: "remove", id: deleting }, { onSuccess: () => setDeleting(null) });
        }} />
      {dialog}
    </div>, document.body)}
  </div>;
}
