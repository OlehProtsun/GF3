import { useEffect, useEffectEvent, useId, useLayoutEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { useQuery } from "@tanstack/react-query";
import type { EmployeeSchedule } from "@entities/employee-schedule";
import { employeeScheduleApi } from "@entities/employee-schedule/api/employeeScheduleApi";
import { downloadExportFile } from "@entities/exports/lib/download";
import type { PublicScheduleSummary } from "@entities/employee-schedule/model/types";
import { ContainerGraphMatrix } from "@entities/containers/ui/ContainerGraphMatrix";
import { ApiError } from "@shared/api/httpClient";
import { usePublicHolidayVisibility } from "@app/providers/PublicHolidayVisibilityProvider";
import { queryKeys } from "@shared/api/queryKeys";
import { dateTimeFormat, t } from "@shared/i18n";
import { formatScheduleLastUpdate } from "@shared/lib/scheduleLastUpdate";
import { CheckIcon, ChevronLeftIcon, ChevronRightIcon, CloseIcon, EyeIcon, ScheduleIcon } from "@shared/ui/icons";
import { CardSection } from "@shared/ui/sections";
import { buildDailyScheduleDays, buildDailyScheduleWorkers, EmployeeScheduleDailyBody, getDefaultDailyScheduleDay } from "./EmployeeScheduleDailyBody";
import { EmployeeScheduleHeaderActions } from "./EmployeeScheduleHeaderActions";
import { useSchedulePdfExport } from "./useSchedulePdfExport";
import { buildScheduleMatrixCellMap, buildScheduleMatrixColumns, buildScheduleMatrixDisplayCellMap } from "./employeeScheduleMatrix";
import scheduleStyles from "./EmployeeSchedulePage.module.css";
import styles from "./EmployeeScheduleSelectDialog.module.css";

type EmployeeScheduleSelectDialogProps = {
  open: boolean;
  schedules: EmployeeSchedule[];
  selectedScheduleId: number | null;
  onSelect: (scheduleId: number) => void;
  onClose: () => void;
};

const periodFormatter = dateTimeFormat("en-GB", { month: "long", year: "numeric", timeZone: "UTC" });
type ScheduleView = "select" | "public" | "preview";

export function EmployeeScheduleSelectDialog({ open, ...props }: EmployeeScheduleSelectDialogProps) {
  return open ? <ScheduleSelectSheet {...props} /> : null;
}

function ScheduleSelectSheet({ schedules, selectedScheduleId, onSelect, onClose }: Omit<EmployeeScheduleSelectDialogProps, "open">) {
  const titleId = useId();
  const listId = useId();
  const dialogRef = useRef<HTMLDivElement>(null);
  const listRef = useRef<HTMLDivElement>(null);
  const titleRef = useRef<HTMLHeadingElement>(null);
  const restoreScroll = useRef<number | null>(null);
  const publicScroll = useRef(0);
  const selectionScroll = useRef(0);
  const [expanded, setExpanded] = useState(false);
  const [view, setView] = useState<ScheduleView>("select");
  const [previewScheduleId, setPreviewScheduleId] = useState<number | null>(null);
  const { data: publicSchedules, isError: publicError } = useQuery({
    queryKey: queryKeys.employeeSchedules.publicList(),
    queryFn: ({ signal }) => employeeScheduleApi.listPublic(signal),
    enabled: view === "public",
    staleTime: 0,
    cancelOnUnmount: true,
  });
  const closeOnEscape = useEffectEvent(onClose);
  const groupsByMonth = new Map<number, EmployeeSchedule[]>();

  for (const schedule of schedules) {
    const key = schedule.year * 12 + schedule.month;
    const group = groupsByMonth.get(key);
    if (group) group.push(schedule);
    else groupsByMonth.set(key, [schedule]);
  }

  const groups = [...groupsByMonth];
  const recentGroups = groups.filter(([, group], index) =>
    index < 2 || group.some(schedule => schedule.id === selectedScheduleId));
  const visibleGroups = expanded ? groups : recentGroups;

  useLayoutEffect(() => {
    const previousFocus = document.activeElement;
    const root = document.getElementById("root");
    const wasInert = root?.inert ?? false;
    const scrollX = window.scrollX;
    const scrollY = window.scrollY;
    const bodyStyle = document.body.style;
    const oldLock = document.body.getAttribute("data-schedule-scroll-lock");
    const lockStyles = {
      "--schedule-scroll-top": `${-scrollY}px`,
      "--schedule-scroll-left": `${-scrollX}px`,
      "--schedule-scrollbar-gap": `${window.innerWidth - document.documentElement.clientWidth}px`,
    };
    const savedStyles = Object.keys(lockStyles).map(name => [name, bodyStyle.getPropertyValue(name), bodyStyle.getPropertyPriority(name)]);
    for (const [name, value] of Object.entries(lockStyles)) bodyStyle.setProperty(name, value);
    document.body.setAttribute("data-schedule-scroll-lock", "");
    if (root) root.inert = true;
    const dialog = dialogRef.current;
    const buttons = () => Array.from(dialog?.querySelectorAll<HTMLElement>('button:not([disabled]), [tabindex="0"]') ?? []);
    (dialog?.querySelector<HTMLButtonElement>('[aria-pressed="true"]') ?? buttons()[0])?.focus();
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") closeOnEscape();
      if (event.key === "Tab") {
        const targets = buttons();
        const first = targets[0];
        const last = targets.at(-1);
        if (event.shiftKey && (document.activeElement === first || document.activeElement === titleRef.current)) {
          event.preventDefault();
          last?.focus();
        } else if (!event.shiftKey && document.activeElement === last) {
          event.preventDefault();
          first?.focus();
        }
      }
    };
    window.addEventListener("keydown", handleKeyDown);
    return () => {
      window.removeEventListener("keydown", handleKeyDown);
      if (root) root.inert = wasInert;
      if (oldLock === null) document.body.removeAttribute("data-schedule-scroll-lock");
      else document.body.setAttribute("data-schedule-scroll-lock", oldLock);
      for (const [name, value, priority] of savedStyles) {
        if (value) bodyStyle.setProperty(name, value, priority);
        else bodyStyle.removeProperty(name);
      }
      window.scrollTo({ left: scrollX, top: scrollY, behavior: "instant" });
      if (previousFocus instanceof HTMLElement) previousFocus.focus({ preventScroll: true });
    };
  }, []);

  function navigate(next: ScheduleView, scroll = 0) {
    restoreScroll.current = scroll;
    setView(next);
  }

  useLayoutEffect(() => {
    if (restoreScroll.current === null) return;
    if (listRef.current) listRef.current.scrollTop = restoreScroll.current;
    restoreScroll.current = null;
    titleRef.current?.focus({ preventScroll: true });
  }, [view]);

  useLayoutEffect(() => {
    const list = listRef.current;
    if (!list) return;

    const updateFades = () => {
      list.toggleAttribute("data-fade-top", list.scrollTop > 1);
      list.toggleAttribute("data-fade-bottom", list.scrollHeight - list.clientHeight - list.scrollTop > 1);
    };
    updateFades();
    list.addEventListener("scroll", updateFades, { passive: true });
    const observer = typeof ResizeObserver === "undefined" ? null : new ResizeObserver(updateFades);
    observer?.observe(list);
    for (const child of list.children) observer?.observe(child);
    return () => {
      list.removeEventListener("scroll", updateFades);
      observer?.disconnect();
    };
  }, [schedules, expanded, selectedScheduleId, view, publicSchedules, publicError]);

  return createPortal(
    <div className={styles.overlay} role="dialog" aria-modal="true" aria-labelledby={titleId}
      onMouseDown={event => { if (event.target === event.currentTarget) { event.preventDefault(); onClose(); } }}>
      <div className={styles.sheet} ref={dialogRef}>
        <span className={styles.handle} aria-hidden="true" />
        <header className={view === "select" ? styles.header : styles.browserHeader}>
          {view !== "select" && (
            <button type="button" className={styles.close} aria-label={t("Back")} onClick={() => {
              if (view === "preview") {
                setPreviewScheduleId(null);
                navigate("public", publicScroll.current);
              } else {
                navigate("select", selectionScroll.current);
              }
            }}><ChevronLeftIcon size={18} /></button>
          )}
          <div className={view === "select" ? undefined : styles.browserTitle}>
            <h2 id={titleId} ref={titleRef} tabIndex={-1}>{view === "select" ? t("Select schedule") : view === "public" ? t("Public schedules") : t("Schedule Preview")}</h2>
            {view === "public" && <p>{t("View available published schedules")}</p>}
          </div>
          <button type="button" className={styles.close} aria-label={t("Close")} onClick={onClose}><CloseIcon size={18} /></button>
        </header>
        <div className={styles.list} id={listId} ref={listRef} data-preview={view === "preview" || undefined}>
          {view === "select" && visibleGroups.map(([key, group], index) => {
            const { year, month } = group[0];
            const period = periodFormatter.format(new Date(Date.UTC(year, month - 1, 1)));
            return (
              <section key={key} className={`${styles.monthGroup} ${index > 0 ? styles.olderMonth : ""}`}>
                <header className={styles.monthHeader}>
                  <ScheduleIcon size={18} />
                  <h3>{period}</h3>
                  <span className={styles.count}>{t("{0} schedules", group.length)}</span>
                </header>
                <div className={styles.monthRows}>
                  {group.map(schedule => {
                    const selected = schedule.id === selectedScheduleId;
                    const updated = formatScheduleLastUpdate(schedule.lastUpdatedAtUtc);
                    return (
                      <button key={schedule.id} type="button" className={`${styles.row} ${selected ? styles.selected : ""}`}
                        aria-pressed={selected} onClick={() => onSelect(schedule.id)}>
                        <span className={styles.icon} aria-hidden="true"><ScheduleIcon size={20} /></span>
                        <span className={styles.iconDivider} aria-hidden="true" />
                        <span className={styles.copy}>
                          <strong>{schedule.name}</strong>
                          <span className={styles.updated}>{schedule.lastUpdatedAtUtc ? t("Updated {0}", updated) : updated}</span>
                        </span>
                        <span className={styles.indicator} aria-hidden="true">{selected ? <CheckIcon size={15} /> : null}</span>
                      </button>
                    );
                  })}
                </div>
              </section>
            );
          })}
          {view === "select" && <button type="button" className={`${styles.disclosure} ${styles.viewMore}`} onClick={() => {
            selectionScroll.current = listRef.current?.scrollTop ?? 0;
            navigate("public");
          }}>
            <span>{t("View More")}</span>
            <ChevronRightIcon size={15} />
          </button>}
          {view === "public" && (
            publicError ? <p className={styles.message} role="alert">{t("Could not load public schedules.")}</p>
              : publicSchedules === undefined ? <p className={styles.message} role="status">{t("Loading schedules...")}</p>
              : publicSchedules.length === 0 ? <p className={styles.message}>{t("No other public schedules available")}</p>
              : publicSchedules.map(schedule => (
                <button type="button" key={schedule.id} className={styles.row} onClick={() => {
                  publicScroll.current = listRef.current?.scrollTop ?? 0;
                  setPreviewScheduleId(schedule.id);
                  navigate("preview");
                }}>
                  <PublishedScheduleInfo schedule={schedule} showPeriod />
                  <ChevronRightIcon size={17} className={styles.previewChevron} />
                </button>
              ))
          )}
          {view === "preview" && previewScheduleId !== null && <SchedulePreview key={previewScheduleId} scheduleId={previewScheduleId} />}
        </div>
        {view === "select" && groups.length > recentGroups.length && (
          <button type="button" className={styles.disclosure} aria-expanded={expanded} aria-controls={listId}
            onClick={() => setExpanded(value => !value)}>
            <EyeIcon size={20} />
            <span>{expanded ? t("Show fewer schedules") : t("View other schedules")}</span>
            <ChevronRightIcon size={17} />
          </button>
        )}
      </div>
    </div>,
    document.body,
  );
}

function PublishedScheduleInfo({ schedule, showPeriod = false }: {
  schedule: PublicScheduleSummary | EmployeeSchedule;
  showPeriod?: boolean;
}) {
  const updated = formatScheduleLastUpdate(schedule.lastUpdatedAtUtc);
  return <>
    <span className={styles.icon} aria-hidden="true"><ScheduleIcon size={20} /></span>
    <span className={styles.iconDivider} aria-hidden="true" />
    <span className={styles.copy}>
      <span className={styles.publishedName}><strong>{schedule.name}</strong><span className={styles.publishedBadge}>{t("Published")}</span></span>
      {showPeriod && <span>{periodFormatter.format(new Date(Date.UTC(schedule.year, schedule.month - 1, 1)))}</span>}
      <span className={styles.updated}>{schedule.lastUpdatedAtUtc ? t("Updated {0}", updated) : updated}</span>
    </span>
  </>;
}

function SchedulePreview({ scheduleId }: { scheduleId: number }) {
  const [schedule, setSchedule] = useState<EmployeeSchedule | null>(null);
  const [error, setError] = useState<"unavailable" | "failed" | null>(null);
  const [viewMode, setViewMode] = useState<"matrix" | "daily">("matrix");
  const [selectedDay, setSelectedDay] = useState<number | null>(null);
  const previewRef = useRef<HTMLDivElement>(null);
  const toggleRef = useRef<HTMLButtonElement>(null);
  const restoreToggleFocus = useRef(false);
  const activeDayRef = useRef<HTMLButtonElement>(null);
  const showPublicHolidays = usePublicHolidayVisibility();
  const pdf = useSchedulePdfExport(scheduleId, employeeScheduleApi.exportPublicPdf);
  const days = buildDailyScheduleDays(schedule);
  const activeDay = selectedDay ?? getDefaultDailyScheduleDay(schedule, days);
  const matrixDisplay = buildScheduleMatrixDisplayCellMap(schedule, buildScheduleMatrixCellMap(schedule, 0));

  useLayoutEffect(() => {
    if (restoreToggleFocus.current) {
      restoreToggleFocus.current = false;
      toggleRef.current?.focus({ preventScroll: true });
    }
    if (viewMode !== "daily") return;
    const dayButton = activeDayRef.current;
    const daySelector = dayButton?.parentElement;
    if (dayButton && daySelector) {
      const dayBounds = dayButton.getBoundingClientRect();
      const selectorBounds = daySelector.getBoundingClientRect();
      daySelector.scrollLeft += dayBounds.left - selectorBounds.left + (dayBounds.width - selectorBounds.width) / 2;
    }
  }, [viewMode, activeDay]);

  useLayoutEffect(() => {
    if (!schedule || viewMode !== "matrix") return;
    const viewport = previewRef.current?.querySelector<HTMLElement>("[data-phone-matrix-scroll]");
    if (viewport) {
      viewport.scrollTop = 0;
      viewport.scrollLeft = 0;
    }
  }, [schedule, viewMode]);

  const headerActions = <EmployeeScheduleHeaderActions daily={viewMode === "daily"} toggleRef={toggleRef}
    pdfLoading={pdf.isPdfLoading} pdfReady={Boolean(pdf.pdfFile)} onExportPdf={pdf.handleExportPdf}
    onToggle={() => {
      restoreToggleFocus.current = true;
      setViewMode(mode => mode === "matrix" ? "daily" : "matrix");
    }} />;

  useEffect(() => {
    const request = new AbortController();
    employeeScheduleApi.getPublic(scheduleId, request.signal).then(
      result => {
        if (request.signal.aborted) return;
        if (result.publicationStatus === "public") setSchedule(result);
        else setError("unavailable");
      },
      failure => {
        if (request.signal.aborted) return;
        setError(failure instanceof ApiError && [403, 404].includes(failure.status) ? "unavailable" : "failed");
      },
    );
    return () => request.abort();
  }, [scheduleId]);

  return <div className={styles.preview} ref={previewRef}>
    {error ? <p className={styles.message} role="alert">{error === "unavailable" ? t("This schedule is no longer available.") : t("Could not load the schedule preview.")}</p>
      : !schedule ? <p className={styles.message} role="status">{t("Loading schedule preview...")}</p>
      : <>
        <div className={`${styles.row} ${styles.previewSummary}`}><PublishedScheduleInfo schedule={schedule} showPeriod /></div>
        {pdf.isPdfPreparing && <p className={styles.message} role="status">{t("Creating PDF...")}</p>}
        {pdf.pdfExportError && <p className={styles.message} role="alert">{pdf.pdfExportError}</p>}
        {pdf.pdfFile && <div className={scheduleStyles.shiftCorrectionSuccess} role="status">
          <p>{t("PDF is ready. Tap PDF again to share, or download it.")}</p>
          <button type="button" className={scheduleStyles.openSchedulePdfButton} disabled={pdf.isPdfLoading} onClick={() => {
            if (!pdf.pdfFile) return;
            downloadExportFile(pdf.pdfFile, pdf.pdfFile.fileName || "GF3_Graph.pdf");
            pdf.setPdfFile(null);
          }}>{t("Download PDF")}</button>
        </div>}
        {viewMode === "matrix" ? <ContainerGraphMatrix
          className={styles.previewMatrix}
          graph={schedule}
          title={t("Schedule Matrix")}
          icon={null}
          columns={buildScheduleMatrixColumns(schedule, null, "")}
          cellMap={matrixDisplay.cellMap}
          mutedSuffixMap={matrixDisplay.mutedSuffixMap}
          readOnly
          compactSize
          compactHeader
          neutralStyle
          mobileReadOnlyViewport
          showColumnTotals={false}
          allowColumnResize={false}
          stretchColumns={false}
          headerRightSlot={headerActions}
          headerClassName={styles.previewViewHeader}
          emptyMessage={t("No assigned shifts in this schedule yet.")}
        /> : <CardSection className={`${scheduleStyles.dailyScheduleCard} ${styles.previewDaily}`}
          title={t("Daily schedule")} headerRightSlot={headerActions} headerClassName={styles.previewViewHeader}>
          <EmployeeScheduleDailyBody className={styles.previewDailyBody} selectedSchedule={schedule} dailyScheduleDays={days}
            activeDailyScheduleDay={activeDay} dailyScheduleWorkers={buildDailyScheduleWorkers(schedule, activeDay, 0, "")}
            activeDailyScheduleDayRef={activeDayRef} showPublicHolidays={showPublicHolidays} onSelectDay={setSelectedDay} />
        </CardSection>}
      </>}
  </div>;
}
