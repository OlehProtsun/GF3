import { useEffect, useMemo, useRef, useState, type RefObject } from "react";
import { createPortal } from "react-dom";
import { useQueryClient } from "@tanstack/react-query";
import type { EmployeeSchedule } from "@entities/employee-schedule";
import { useShiftSwapPreviewQuery, useShiftSwapReturnCandidatesQuery, type ShiftSwap, type ShiftSwapEmployee, type SwapWizardRequest } from "@entities/shift-swaps";
import { queryKeys } from "@shared/api/queryKeys";
import { getErrorMessage } from "@shared/api/httpClient";
import { getLocale, t } from "@shared/i18n";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { SwapShiftPickerDialog, swapMinutes, swapShiftDate, type SwapShiftSelection } from "./SwapShiftPickerDialog";
import { SwapRequestTypeDialog } from "./SwapRequestTypeDialog";
import styles from "./SwapCreateWizard.module.css";
type Recipient = { mode: "everyone" } | { mode: "specific"; employee: ShiftSwapEmployee; kind: "give" }
  | { mode: "specific"; employee: ShiftSwapEmployee; kind: "exchange"; selection: SwapShiftSelection };
type View = "wizard" | "source" | "type" | "return";
export function SwapCreateWizard({ schedules, employees, employeeId, swaps, busy, error, onClose, onSubmit, returnFocusRef }: {
  schedules: EmployeeSchedule[]; employees: ShiftSwapEmployee[]; employeeId: number; swaps: ShiftSwap[];
  busy: boolean; error?: string | null; onClose: () => void; onSubmit: (payload: SwapWizardRequest) => void;
  returnFocusRef?: RefObject<HTMLElement | null>;
}) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const [step, setStep] = useState<1 | 2 | 3>(1);
  const [view, setView] = useState<View>("wizard");
  const [sourceScheduleId, setSourceScheduleId] = useState<number | null>(null);
  const [source, setSource] = useState<SwapShiftSelection | null>(null);
  const [recipientMode, setRecipientMode] = useState<"everyone" | "specific">("everyone");
  const [recipient, setRecipient] = useState<Recipient>({ mode: "everyone" });
  const [typeEmployee, setTypeEmployee] = useState<ShiftSwapEmployee | null>(null);
  const [requestKind, setRequestKind] = useState<"give" | "exchange">("give");
  const [returnSelection, setReturnSelection] = useState<SwapShiftSelection | null>(null);
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const submitGuard = useRef(false);
  const client = useQueryClient();
  const back = () => {
    if (busy) return;
    if (view === "return") setView("type");
    else if (view !== "wizard") setView("wizard");
    else if (step > 1) setStep((step - 1) as 1 | 2);
    else onClose();
  };
  useEffect(() => {
    const dialog = dialogRef.current;
    const previousFocus = returnFocusRef?.current ?? document.activeElement as HTMLElement | null;
    const overflow = document.body.style.overflow;
    dialog?.showModal();
    document.body.style.overflow = "hidden";
    return () => { dialog?.close(); document.body.style.overflow = overflow; previousFocus?.focus({ preventScroll: true }); };
  }, [returnFocusRef]);
  useEffect(() => {
    dialogRef.current?.querySelector<HTMLElement>("[data-subview-heading]")?.focus({ preventScroll: true });
  }, [view, step]);
  useEffect(() => { if (!busy) submitGuard.current = false; }, [busy, error]);
  useEffect(() => {
    if (view === "wizard" && step === 1 && source) {
      dialogRef.current?.querySelector<HTMLElement>("[data-selected='true']")?.scrollIntoView?.({ block: "start" });
    }
  }, [view, step, source]);
  const payload = useMemo<SwapWizardRequest | null>(() => {
    if (!source || (recipientMode === "specific" && recipient.mode !== "specific")) return null;
    const base = { scheduleId: source.shift.scheduleId, scheduleSlotId: source.shift.slotId,
      fromTime: source.period.fromTime, toTime: source.period.toTime };
    if (recipientMode === "specific" && recipient.mode === "specific") {
      if (recipient.kind === "exchange") return { ...base, requestKind: "exchange", targetEmployeeId: recipient.employee.id,
        returnScheduleId: recipient.selection.shift.scheduleId, returnScheduleSlotId: recipient.selection.shift.slotId,
        returnFromTime: recipient.selection.period.fromTime, returnToTime: recipient.selection.period.toTime };
      return { ...base, requestKind: "give", targetEmployeeId: recipient.employee.id };
    }
    return { ...base, requestKind: "give", targetEmployeeId: null };
  }, [source, recipient, recipientMode]);
  const previewQuery = useShiftSwapPreviewQuery(step === 3 && view === "wizard" ? payload : null);
  const candidatesQuery = useShiftSwapReturnCandidatesQuery(view === "return" && source && typeEmployee ? {
    sourceScheduleId: source.shift.scheduleId, sourceSlotId: source.shift.slotId,
    fromTime: source.period.fromTime, toTime: source.period.toTime, targetEmployeeId: typeEmployee.id, page,
  } : null);
  const candidates = candidatesQuery.data?.items ?? [];
  const schedule = schedules.find(item => item.id === sourceScheduleId);
  const ownShifts = schedule?.slots.filter(slot => slot.employeeId === employeeId && slot.status === "ASSIGNED")
    .sort((a, b) => a.dayOfMonth - b.dayOfMonth || a.fromTime.localeCompare(b.fromTime) || a.id - b.id).map(slot => {
      const reserved = swaps.some(swap => swap.status === "open" && (swap.scheduleSlotId === slot.id || swap.returnScheduleSlotId === slot.id));
      return { scheduleId: schedule.id, scheduleName: schedule.name, shopName: schedule.shopName,
        year: schedule.year, month: schedule.month, dayOfMonth: slot.dayOfMonth, slotId: slot.id,
        fromTime: slot.fromTime, toTime: slot.toTime, durationMinutes: swapMinutes(slot.toTime) - swapMinutes(slot.fromTime),
        canSelect: !reserved, unavailableReason: reserved ? "This shift already has an open swap offer." : null };
    }) ?? [];
  const selectEmployee = (employee: ShiftSwapEmployee) => {
    setTypeEmployee(employee);
    const selected = recipient.mode === "specific" && recipient.employee.id === employee.id ? recipient : null;
    setRequestKind(selected?.kind ?? "give");
    setReturnSelection(selected?.kind === "exchange" ? selected.selection : null);
    if (selected?.kind !== "exchange") setPage(1);
    setView("type");
  };
  const preview = previewQuery.data;
  const previewMatches = Boolean(preview && payload && preview.source.scheduleId === payload.scheduleId &&
    preview.source.slotId === payload.scheduleSlotId && preview.source.fromTime === payload.fromTime && preview.source.toTime === payload.toTime &&
    preview.targetEmployeeId === payload.targetEmployeeId && preview.requestKind === payload.requestKind &&
    (payload.requestKind === "give" || (preview.return?.slotId === payload.returnScheduleSlotId &&
      preview.return?.scheduleId === payload.returnScheduleId && preview.return?.fromTime === payload.returnFromTime && preview.return?.toTime === payload.returnToTime)));
  const displayError = error || (view === "return" && candidatesQuery.error ? getErrorMessage(candidatesQuery.error, t("Could not load return shifts.")) : null)
    || (step === 3 && previewQuery.error ? getErrorMessage(previewQuery.error, t("Could not preview this request.")) : null);
  const enterReview = () => {
    if (!payload) return;
    void client.invalidateQueries({ queryKey: queryKeys.shiftSwaps.preview(payload) });
    setStep(3);
  };
  return createPortal(<dialog id="create-swap-panel" ref={dialogRef} className={`${styles.dialog} ${view === "wizard" ? `${styles.scheduleDialog} ${step === 2 ? styles.recipientDialog : step === 3 ? styles.reviewDialog : ""}` : view === "source" || view === "return" ? styles.pickerDialog : view === "type" ? styles.typeDialog : ""}`} aria-label={t("Create a swap offer")}
    onCancel={event => { event.preventDefault(); if (!busy) { if (view === "wizard") onClose(); else back(); } }}
    onClick={event => { if (event.target === event.currentTarget && !busy) onClose(); }}>
    <div className={styles.shell}>
      <header className={styles.header}><button type="button" aria-label={t("Close")} disabled={busy} onClick={() => view !== "wizard" ? back() : onClose()}>×</button></header>
      <div className={styles.body}>
        {displayError && <ErrorBanner dismissible={false}>{displayError}</ErrorBanner>}
        {view === "source" && <SwapShiftPickerDialog key={sourceScheduleId} shifts={ownShifts} selection={source?.shift.scheduleId === sourceScheduleId ? source : null}
          onBack={() => setView("wizard")} onConfirm={selection => {
            const changed = source?.shift.slotId !== selection.shift.slotId || source?.period.fromTime !== selection.period.fromTime || source?.period.toTime !== selection.period.toTime;
            setSource(selection);
            if (changed && recipient.mode === "specific" && recipient.kind === "exchange") setRecipient({ mode: "everyone" });
            setReturnSelection(null); setView("wizard");
          }} />}
        {view === "type" && typeEmployee && <SwapRequestTypeDialog employee={typeEmployee} kind={requestKind} selection={returnSelection}
          onKindChange={kind => setRequestKind(kind)}
          onChooseShift={() => { void client.invalidateQueries({ queryKey: ["shiftSwaps", "returnCandidates"] }); setView("return"); }}
          onBack={() => setView("wizard")} onSave={() => {
            if (requestKind === "exchange" && returnSelection) setRecipient({ mode: "specific", employee: typeEmployee, kind: "exchange", selection: returnSelection });
            else if (requestKind === "give") setRecipient({ mode: "specific", employee: typeEmployee, kind: "give" });
            setView("wizard");
          }} />}
        {view === "return" && source && typeEmployee && <SwapShiftPickerDialog shifts={candidates} selection={returnSelection}
          returnMode employeeName={typeEmployee.displayName} durationMinutes={swapMinutes(source.period.toTime) - swapMinutes(source.period.fromTime)}
          loading={candidatesQuery.isFetching} hasMore={candidatesQuery.data?.hasMore} onLoadMore={() => setPage(value => value + 1)}
          onBack={() => setView("type")} onConfirm={selection => { setReturnSelection(selection); setView("type"); }} />}
        {view === "wizard" && <>
          {step === 3 && <span className={styles.reviewHandle} aria-hidden="true" />}
          <ol className={styles.steps} aria-label={t("Request progress")}>{(["Schedule", "Recipient", "Review"] as const).map((label, index) =>
            <li key={label} aria-current={step === index + 1 ? "step" : undefined} data-complete={step > index + 1}>{step === 3 && index < 2 ? <button type="button" disabled={busy} aria-label={index === 0 ? t("Edit schedule") : t("Edit recipient")} onClick={() => setStep(index === 0 ? 1 : 2)}><span>✓</span>{t(label)}</button> : <><span>{step > index + 1 ? "✓" : index + 1}</span>{t(label)}</>}</li>)}</ol>
          {step === 2 && <button type="button" className={styles.previousStep} onClick={back} aria-label={t("Back")}>‹</button>}
          <h2 className={styles.heading} tabIndex={-1} data-subview-heading>{step === 1 ? t("Choose a schedule") : step === 2 ? t("Give to whom?") : t("Review & confirm")}</h2>
          {step === 1 && <p className={styles.description}>{t("Select a schedule, then choose the shift you want to give away.")}</p>}
          {step === 1 && <div className={styles.cards} tabIndex={0} role="region" aria-label={t("Schedules")}>
            {schedules.filter(item => item.publicationStatus === "public" && item.allowSwap !== false).map(item => <div key={item.id} className={styles.card} data-selected={source?.shift.scheduleId === item.id}>
              <button type="button" className={styles.schedule} aria-pressed={source?.shift.scheduleId === item.id} onClick={() => { setSourceScheduleId(item.id); setView("source"); }}>
                <span className={styles.month}>{new Intl.DateTimeFormat(getLocale(), { month: "short" }).format(new Date(item.year, item.month - 1, 1))}<small>{item.year}</small></span>
                <span className={styles.scheduleInfo}><strong>{item.name}</strong><small>{item.containerName || item.shopName}</small></span>
                <span className={styles.selectionMark} aria-hidden="true">{source?.shift.scheduleId === item.id ? "✓" : ""}</span>
              </button>
              {source?.shift.scheduleId === item.id && <div className={styles.nested}><span>{t("Selected shift")}</span><strong>{new Intl.DateTimeFormat(getLocale(), { weekday: "short", day: "2-digit", month: "short" }).format(new Date(source.shift.year, source.shift.month - 1, source.shift.dayOfMonth))} · {source.period.fromTime}–{source.period.toTime}</strong>
                <small>{source.period.fromTime === source.shift.fromTime && source.period.toTime === source.shift.toTime ? t("Full shift") : t("Custom period")}</small>
                <button type="button" onClick={() => { setSourceScheduleId(item.id); setView("source"); }}>{t("Change shift")}</button></div>}
              {source?.shift.scheduleId !== item.id && <span className={styles.chooseHint}>{t("Choose a shift")}</span>}
            </div>)}
            {schedules.filter(s => s.publicationStatus === "public" && s.allowSwap !== false).length === 0 && <p>{t("No published schedules are available for swap.")}</p>}
          </div>}
          {step === 2 && <><p className={styles.description}>{t("Choose who can take this shift.")}</p><div className={styles.recipientContent}>
            <div className={styles.recipientModes}>
              <button type="button" aria-pressed={recipientMode === "everyone"} onClick={() => { setRecipientMode("everyone"); setRecipient({ mode: "everyone" }); setReturnSelection(null); }} aria-label={t("Everyone")}><span className={styles.optionIcon} aria-hidden="true"><PeopleIcon group /></span><span className={styles.optionCopy}><strong>{t("Everyone")}</strong><small>{t("All employees can see and take this shift.")}</small></span><span className={styles.radioMark} aria-hidden="true" /></button>
              <button type="button" aria-pressed={recipientMode === "specific"} onClick={() => setRecipientMode("specific")} aria-label={t("Specific employee")}><span className={styles.optionIcon} aria-hidden="true"><PeopleIcon /></span><span className={styles.optionCopy}><strong>{t("Specific employee")}</strong><small>{t("Choose one employee to give this shift to.")}</small></span><span className={styles.radioMark} aria-hidden="true" /></button>
            </div>
            {recipientMode === "specific" && <>
              <div className={styles.search}><input type="search" aria-label={t("Search employee...")} placeholder={t("Search employee...")} value={search} onChange={event => setSearch(event.target.value)} />
                {search && <button type="button" aria-label={t("Clear search")} onClick={() => setSearch("")}>×</button>}</div>
              <div className={styles.employees} role="group" aria-label={t("Employees")}>
                {employees.filter(employee => employee.id !== employeeId && employee.displayName.toLocaleLowerCase().includes(search.trim().toLocaleLowerCase()))
                  .sort((a, b) => a.displayName.localeCompare(b.displayName)).map(employee => <button key={employee.id} type="button"
                    aria-pressed={recipient.mode === "specific" && recipient.employee.id === employee.id} onClick={() => selectEmployee(employee)}>
                    <span className={styles.avatar} aria-hidden="true">{employee.firstName[0]}{employee.lastName[0]}</span><span>{employee.displayName}
                      {recipient.mode === "specific" && recipient.employee.id === employee.id && <small>{recipient.kind === "exchange" ? t("Swap shifts") : t("Give shift")}</small>}</span><span className={styles.radioMark} aria-hidden="true" />
                  </button>)}
              </div>
            </>}
          </div></>}
          {step === 3 && <><p className={styles.description}>{t("Check all details before sending the request.")}</p>
            <div className={styles.reviewContent} tabIndex={0} role="region" aria-label={t("Review details")}>
            {previewQuery.isFetching && <p role="status">{t("Loading...")}</p>}
            {preview && previewMatches && !previewQuery.error && <>
              <ReviewShiftCard shift={preview.source} selection={source} />
              {preview.requestKind === "exchange" && preview.return && <>
                <div className={styles.exchangeArrows} aria-label={t("Reciprocal exchange")}><ExchangeIcon /></div>
                <ReviewShiftCard shift={preview.return} receive employeeName={preview.targetEmployeeName}
                  initials={recipient.mode === "specific" ? `${recipient.employee.firstName[0] ?? ""}${recipient.employee.lastName[0] ?? ""}` : ""}
                  selection={recipient.mode === "specific" && recipient.kind === "exchange" ? recipient.selection : null} />
              </>}
              <div className={styles.requestSummary}>
                <span className={styles.requestIcon} aria-hidden="true">{preview.requestKind === "exchange" ? <ExchangeIcon /> : <span>→</span>}</span>
                <div><span>{t("Type of request")}</span><strong>{preview.requestKind === "exchange" ? t("Swap shifts") : t("Give shift")}</strong>
                  <span className={styles.recipientName}>{preview.targetEmployeeId ? preview.targetEmployeeName : t("Everyone")}</span>
                  <p>{preview.requestKind === "exchange" ? t("You give your shift and receive one of {0}'s shifts.", preview.targetEmployeeName)
                    : preview.targetEmployeeId ? t("You give your selected shift to {0}.", preview.targetEmployeeName) : t("Your shift will be available to eligible employees.")}</p>
                </div>
              </div>
              <div className={styles.reviewStats}><h3><span aria-hidden="true">▥</span>{t("Your statistics")}</h3><p>{t("Estimated, pending acceptance")}</p>
                {preview.projectedMonths.slice(0, 1).map(month => <MonthStats key={`${month.year}-${month.month}`} month={month} />)}
                {preview.projectedMonths.length > 1 && <details><summary>{t("View details")}</summary>{preview.projectedMonths.slice(1).map(month => <MonthStats key={`${month.year}-${month.month}`} month={month} />)}</details>}
              </div>
              <div className={styles.reviewInfo}><span aria-hidden="true">ⓘ</span><p>{preview.requestKind === "exchange"
                ? t("Both shifts will transfer only after {0} accepts the request.", preview.targetEmployeeName)
                : preview.targetEmployeeId ? t("After sending, {0} will receive a request and must confirm it.", preview.targetEmployeeName)
                : t("After sending, eligible employees will be able to view and accept your offer.")}</p></div>
            </>}
            </div>
          </>}
          <footer className={styles.footer}>
            {step < 3 ? <button type="button" className={styles.primary} disabled={step === 1 ? !source : !payload}
              onClick={() => step === 1 ? setStep(2) : enterReview()}>{t("Continue")}{step < 3 && <span aria-hidden="true" className={styles.arrow}>→</span>}</button>
              : <button type="button" className={styles.primary} disabled={busy || !payload || !previewMatches || !preview?.isValid || previewQuery.isFetching || Boolean(previewQuery.error)} onClick={() => {
                if (!payload || submitGuard.current) return; submitGuard.current = true; onSubmit(payload);
              }}>{busy ? t("Sending...") : t("Send request")}<span className={styles.arrow} aria-hidden="true">→</span></button>}
          </footer>
        </>}
      </div>
    </div>
  </dialog>, document.body);
}
function MonthStats({ month }: { month: import("@entities/shift-swaps").ShiftSwapPreviewMonthStats }) {
  const format = (value: number) => new Intl.NumberFormat(getLocale(), { maximumFractionDigits: 2 }).format(value);
  return <div className={styles.reviewMonth}><span>{String(month.month).padStart(2, "0")}.{month.year}</span>
    <dl>{([ [t("Total hours"), month.hoursBefore, month.hoursAfter, "◷"], [t("Work days"), month.workDaysBefore, month.workDaysAfter, "▦"],
      [t("Free days"), month.freeDaysBefore, month.freeDaysAfter, "☼"] ] as const).map(([label, before, after, icon]) =>
        <div key={label}><dt>{label}<span aria-hidden="true">{icon}</span></dt><dd>{format(before)} → {format(after)}</dd></div>)}</dl></div>;
}

function ExchangeIcon() {
  return <svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><path d="M4 8h16m-5-5 5 5-5 5M20 16H4m5-5-5 5 5 5" /></svg>;
}

function ReviewShiftCard({ shift, selection, receive = false, employeeName, initials }: {
  shift: import("@entities/shift-swaps").ShiftSwapReturnCandidate; selection: SwapShiftSelection | null;
  receive?: boolean; employeeName?: string; initials?: string;
}) {
  const date = new Date(shift.year, shift.month - 1, shift.dayOfMonth);
  const custom = selection && (selection.period.fromTime !== selection.shift.fromTime || selection.period.toTime !== selection.shift.toTime);
  return <section className={styles.transferCard} aria-label={receive ? t("Employee shift (they give)") : t("Your shift (you give)")}>
    <header><span className={receive ? styles.receiveAvatar : styles.transferIcon} aria-hidden="true">{receive ? initials : <PeopleIcon />}</span>
      <div><h3>{receive ? t("Employee shift (they give)") : t("Your shift (you give)")}</h3>{receive && <p>{employeeName}</p>}</div>
      <span className={receive ? styles.receiveBadge : styles.giveBadge}>{receive ? t("Receive") : t("Give")}</span>
    </header>
    <div className={styles.transferPeriod}><div className={styles.dateBadge}><strong>{String(shift.dayOfMonth).padStart(2, "0")}</strong><span>{new Intl.DateTimeFormat(getLocale(), { month: "short" }).format(date)}</span><small>{shift.year}</small></div>
      <div><strong>{shift.fromTime} – {shift.toTime}</strong><span>{new Intl.NumberFormat(getLocale(), { maximumFractionDigits: 2 }).format(shift.durationMinutes / 60)}h</span></div>
    </div>
    <div className={styles.transferMeta}><span>{new Intl.DateTimeFormat(getLocale(), { weekday: "long" }).format(date)}, {swapShiftDate(shift)}</span><span>{shift.scheduleName}</span></div>
    <span className={styles.periodBadge}>{custom ? t("Custom period") : t("Full shift")}</span>
  </section>;
}

function PeopleIcon({ group = false }: { group?: boolean }) {
  return <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden="true"><circle cx="12" cy="7" r="3" /><path d="M6 21v-3a6 6 0 0 1 12 0v3Z" />{group && <><path d="M5 5a3 3 0 0 0 0 6M19 5a3 3 0 0 1 0 6M2 20v-3a5 5 0 0 1 3-4M22 20v-3a5 5 0 0 0-3-4" /></>}</svg>;
}
