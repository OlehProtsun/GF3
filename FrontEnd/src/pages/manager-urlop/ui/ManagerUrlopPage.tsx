import { useEffect, useLayoutEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { useLocation, useParams } from "react-router-dom";
import { useAuth } from "@app/providers/AuthProvider";
import { useEmployeesListQuery } from "@entities/employees";
import { useShopByIdQuery } from "@entities/shops";
import { useManagerUrlopMonthQuery, useUrlopMutation, urlopApi, UrlopMonthCalendar, formatUrlopConflictDates, isoDate, occupiedDays, toggleDay, validSelection, warsawToday, type UrlopRequest } from "@entities/urlop";
import { dateTimeFormat, t } from "@shared/i18n";
import { ApiError, getErrorMessage } from "@shared/api/httpClient";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { PageHeader } from "@shared/ui/PageHeader";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import { ChevronLeftIcon, ChevronRightIcon, PlusIcon, ShopIcon, ScheduleIcon } from "@shared/ui/icons";
import { SearchableSelect, type SearchableSelectOption } from "@shared/ui/components/SearchableSelect";
import { useLanguageRevision } from "@shared/i18n/useLanguageRevision";
import { usePageScrollbarHidden } from "@shared/lib/usePageScrollbarHidden";
import { useUnsavedChangesPrompt } from "@shared/lib/useUnsavedChangesPrompt";
import { ManagerUrlopOverviewCalendar } from "./ManagerUrlopOverviewCalendar";
import { ManagerUrlopRequestsPanel } from "./ManagerUrlopRequestsPanel";
import styles from "./ManagerUrlopPage.module.css";

type Operation = { kind: "month"; isOpen: boolean } | { kind: "save" } | { kind: "approve" | "reject" | "remove"; id: string };
export function ManagerUrlopPage() {
  useLanguageRevision();
  usePageScrollbarHidden();
  const { session } = useAuth();
  const { shopId: routeShopId } = useParams<{ shopId: string }>();
  const parsedShopId = Number(routeShopId);
  const shopId = Number.isSafeInteger(parsedShopId) && parsedShopId > 0 ? parsedShopId : 0;
  const shopQuery = useShopByIdQuery(shopId || null);
  const employees = useEmployeesListQuery();
  const location = useLocation();
  const [period, setPeriod] = useState(() => {
    const value = new URLSearchParams(location.search).get("period");
    return value && /^(?!0000)\d{4}-(0[1-9]|1[0-2])$/.test(value) ? value : warsawToday().slice(0, 7);
  });
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
  const scrollPosition = useRef<number | null>(null);
  const toolbar = useRef<HTMLDivElement>(null);
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
  const details = error instanceof ApiError ? error.details : null;
  const shiftConflict = details !== null && typeof details === "object" && "code" in details && details.code === "urlop_shift_conflict";
  const conflictDates = shiftConflict && "conflictingDates" in details && Array.isArray(details.conflictingDates)
    ? formatUrlopConflictDates(details.conflictingDates.filter((value): value is string => typeof value === "string")) : "";
  const errorBanner = error ? <ErrorBanner>{shiftConflict ? <>
    <span className={styles.errorLine}>{t("Vacation cannot be approved because the employee has a scheduled shift.")}</span>
    {conflictDates ? <><span className={styles.errorLine}>{t("Conflicting dates: {0}", conflictDates)}</span><span className={styles.errorLine}>{t("Change or remove the conflicting shift in the work schedule, then try again.")}</span></> : <span className={styles.errorLine}>{t("The schedule conflict could not be resolved. Check the employee's work schedule and try again.")}</span>}
  </> : getErrorMessage(error)}</ErrorBanner> : null;
  const busy = mutation.isPending || readOnly;
  const closeEditor = () => { if (!mutation.isPending) confirmIfNeeded(() => { reset(); setMutationError(null); }); };
  const openEditor = (request?: UrlopRequest) => {
    if (busy || !data || request?.status === "rejected") return;
    invoker.current = document.activeElement as HTMLElement;
    setMutationError(null); setEditing(request ?? null); setSelected([...(request?.days ?? [])]); setEditorContext(`${shopId}:${period}`);
  };
  const changePeriod = (nextYear: number, nextMonth: number) => {
    if (mutation.isPending || !Number.isInteger(nextYear) || !Number.isInteger(nextMonth) || nextYear < 1 || nextYear > 9999 || nextMonth < 1 || nextMonth > 12) return;
    confirmIfNeeded(() => { scrollPosition.current = window.scrollY; reset(); setMutationError(null); setPeriod(isoDate(nextYear, nextMonth, 1).slice(0, 7)); });
  };
  useLayoutEffect(() => {
    if (scrollPosition.current === null) return;
    window.scrollTo({ top: Math.min(scrollPosition.current, Math.max(0, document.documentElement.scrollHeight - window.innerHeight)), behavior: "instant" });
    if (data || query.isError) scrollPosition.current = null;
  }, [period, data, query.isError]);
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
  const monthOptions: SearchableSelectOption[] = Array.from({ length: 12 }, (_, i) => ({ value: String(i + 1), label: dateTimeFormat("en-GB", { month: "long" }).format(new Date(2000, i, 1)) }));
  const employeeOptions: SearchableSelectOption[] = (employees.data ?? []).map(x => ({ value: String(x.id), label: `${x.firstName} ${x.lastName}` }));
  if (editing && !employeeOptions.some(x => x.value === String(editing.employeeId))) employeeOptions.unshift({ value: String(editing.employeeId), label: editing.employeeName });
  return <div className={styles.page}>
    <PageHeader title={t("Urlop")} backTo="/urlop" subtitle={t("Manage vacation requests for the selected shop.")} />
    <section className={styles.workspace} aria-label={t("Shop vacation management")}>
    {!shopId ? <ErrorBanner>{t("Shop not found.")}</ErrorBanner> : null}
    {!editorOpen ? errorBanner : null}
    {shopId > 0 ? <>
      <div className={styles.identity}><span aria-hidden="true"><ShopIcon size={25} style={{ transform: "scaleY(-1)" }} /></span><div><h2>{data?.shopName ?? shopQuery.data?.name ?? t("Shop vacation management")}</h2><p>{shopQuery.data?.id === shopId && shopQuery.data.address ? shopQuery.data.address : t("Shop vacation management")}</p></div></div>
      <div className={styles.hero}><div className={styles.intro}><h1>{t("Monthly vacation schedule")}</h1><p>{t("Overview of approved vacations and requests for this shop.")}</p></div>
        <div ref={toolbar} className={styles.toolbar} onKeyDown={event => {
          const trigger = toolbar.current?.querySelector<HTMLButtonElement>('button[aria-expanded="true"]');
          if (!trigger) return;
          if (event.key === "Escape") { event.preventDefault(); event.stopPropagation(); trigger.click(); trigger.focus({ preventScroll: true }); }
          if (event.key === "ArrowDown" || event.key === "ArrowUp" || event.key === "Tab" && event.target === trigger) {
            const options = [...document.querySelectorAll<HTMLButtonElement>('[role="listbox"] [role="option"]')];
            if (!options.length) return;
            event.preventDefault();
            const index = options.indexOf(document.activeElement as HTMLButtonElement);
            options[(index + (event.key === "ArrowUp" || event.shiftKey ? -1 : 1) + options.length) % options.length]?.focus();
          }
        }}>
          <SearchableSelect className={styles.monthSelect} value={String(month)} options={monthOptions} placeholder={t("Month")} dropdownTitle={t("Month")} ariaLabel={t("Month")} size="compact" searchEnabled={false} showSelectedHint={false} disabled={busy} onChange={value => { changePeriod(year, Number(value)); toolbar.current?.querySelector<HTMLButtonElement>('[aria-label="' + t("Month") + '"]')?.focus({ preventScroll: true }); }} />
          <SearchableSelect className={styles.yearSelect} value={String(year)} options={years.map(value => ({ value: String(value), label: String(value) }))} placeholder={t("Year")} dropdownTitle={t("Year")} ariaLabel={t("Year")} size="compact" searchEnabled={false} showSelectedHint={false} disabled={busy} onChange={value => { changePeriod(Number(value), month); toolbar.current?.querySelector<HTMLButtonElement>('[aria-label="' + t("Year") + '"]')?.focus({ preventScroll: true }); }} />
          <button type="button" className={styles.arrow} aria-label={t("Previous month")} disabled={busy || year === 1 && month === 1} onClick={event => { event.currentTarget.focus({ preventScroll: true }); changePeriod(month === 1 ? year - 1 : year, month === 1 ? 12 : month - 1); }}><ChevronLeftIcon size={20} /></button>
          <button type="button" className={styles.arrow} aria-label={t("Next month")} disabled={busy || year === 9999 && month === 12} onClick={event => { event.currentTarget.focus({ preventScroll: true }); changePeriod(month === 12 ? year + 1 : year, month === 12 ? 1 : month + 1); }}><ChevronRightIcon size={20} /></button>
          <button ref={addButton} type="button" className={styles.add} disabled={busy || !data || employees.isLoading || !employees.data?.length} onClick={event => { event.currentTarget.focus(); openEditor(); }}><PlusIcon size={20} />{t("Add vacation")}</button>
        </div>
      </div>
      <div className={styles.dashboard} aria-busy={!data && !query.isError}>
        <ManagerUrlopOverviewCalendar year={year} month={month} approvedDays={data?.approvedDays ?? []} loading={!data && !query.isError} />
        <ManagerUrlopRequestsPanel requests={data?.requests ?? []} isOpen={data?.isOpen ?? false} loading={!data && !query.isError} busy={mutation.isPending || !data} readOnly={readOnly} onSetMonthOpen={isOpen => mutate({ kind: "month", isOpen })}
          onApprove={id => mutate({ kind: "approve", id })} onReject={id => mutate({ kind: "reject", id })} onEdit={openEditor} onDelete={setDeleting} />
      </div>
    </> : null}
    </section>
    {createPortal(<div ref={layer} onKeyDown={event => {
      if (!editorOpen) return;
      const employeeTrigger = document.getElementById("urlop-employee");
      const popup = document.querySelector<HTMLElement>('[role="listbox"][aria-label="' + t("Employee") + '"]')?.parentElement?.parentElement;
      if (employeeTrigger?.getAttribute("aria-expanded") === "true" && popup && !confirming) {
        if (event.key === "Escape") { event.preventDefault(); event.stopPropagation(); employeeTrigger.click(); employeeTrigger.focus(); return; }
        if (event.key === "Tab") {
          const controls = [...popup.querySelectorAll<HTMLElement>('input, button:not(:disabled)')];
          const index = controls.indexOf(document.activeElement as HTMLElement);
          event.preventDefault(); controls[(index + (event.shiftKey ? -1 : 1) + controls.length) % controls.length]?.focus(); return;
        }
      }
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
          <div className={styles.employee}><label htmlFor="urlop-employee">{t("Employee")}</label><SearchableSelect id="urlop-employee" value={String(editing?.employeeId ?? employeeId)} options={employeeOptions} placeholder={t("Employee")} dropdownTitle={t("Employee")} ariaLabel={t("Employee")} size="field" searchEnabled showSelectedHint={false} disabled={busy || !!editing} onChange={value => {
            const id = Number(value);
            if (!Number.isSafeInteger(id) || id <= 0) return;
            confirmIfNeeded(() => { setEmployee(id); setSelected([]); });
            document.getElementById("urlop-employee")?.focus();
          }} /></div>
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
