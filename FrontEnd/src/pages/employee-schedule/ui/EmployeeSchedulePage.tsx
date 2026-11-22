import { useLanguageRevision } from "@shared/i18n/useLanguageRevision";
import { usePublicHolidayVisibility } from "@app/providers/PublicHolidayVisibilityProvider";
import { getPolishPublicHoliday } from "@shared/lib/polishPublicHolidays";
import { t, numberFormat, dateTimeFormat } from "@shared/i18n";
import { useEmployeeApprovedUrlopQuery, approvedKeys } from "@entities/urlop";
import { warsawToday } from "@entities/urlop/model/calendar";
import { useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { NavLink } from "react-router-dom";
import { useAuth } from "@app/providers/AuthProvider";
import {
  useEmployeeScheduleListQuery,
  type EmployeeSchedule,
  type EmployeeScheduleSlot,
} from "@entities/employee-schedule";
import {
  useCreateShiftCorrectionMutation,
  useEmployeeShiftCorrectionsQuery,
  type CreateShiftCorrectionInput,
} from "@entities/shift-corrections";
import { getGraphCellKey, type GraphMatrixColumn } from "@entities/containers/model/graphWorkspace";
import { ContainerGraphMatrix } from "@entities/containers/ui/ContainerGraphMatrix";
import { employeeUiStateApi, useEmployeeUiStateQuery } from "@entities/employee-ui-state";
import { useEmployeeShiftSwapsQuery } from "@entities/shift-swaps";
import { getErrorMessage } from "@shared/api/httpClient";
import { downloadExportFile } from "@entities/exports/lib/download";
import { employeeScheduleApi } from "@entities/employee-schedule/api/employeeScheduleApi";
import { formatScheduleLastUpdate } from "@shared/lib/scheduleLastUpdate";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { SearchableSelect, type SearchableSelectOption } from "@shared/ui/components/SearchableSelect";
import { AvailabilityIcon, NewsIcon, ScheduleIcon, StatisticsIcon } from "@shared/ui/icons";
import { CardSection } from "@shared/ui/sections";
import workspaceStyles from "@pages/shared/EmployeeWorkspacePage.module.css";
import loadingStyles from "@shared/ui/SavingOverlay/SavingOverlay.module.css";
import { EmployeeScheduleColumnOrderDialog } from "./EmployeeScheduleColumnOrderDialog";
import { EmployeeScheduleSelectDialog } from "./EmployeeScheduleSelectDialog";
import { rankEmployeeSchedules } from "./rankEmployeeSchedules";
import { ScheduleShiftBadges } from "./PendingCorrectionBadge";
import { buildScheduleShiftStatuses, getScheduleStatusTone } from "./scheduleShiftStatuses";
import { getPendingCorrectionDays } from "./pendingScheduleCorrections";
import { EmployeeShiftCorrectionDialog } from "./EmployeeShiftCorrectionDialog";
import { buildScheduleMatrixCellMap, buildScheduleMatrixColumns, buildScheduleMatrixDisplayCellMap } from "./employeeScheduleMatrix";
import { buildDailyScheduleDays, buildDailyScheduleWorkers, EmployeeScheduleDailyBody, getDefaultDailyScheduleDay, parseTimeMinutes } from "./EmployeeScheduleDailyBody";
import { useSchedulePdfExport } from "./useSchedulePdfExport";
import { EmployeeScheduleHeaderActions } from "./EmployeeScheduleHeaderActions";
import styles from "./EmployeeSchedulePage.module.css";
import { EmployeeScheduleHero } from "./EmployeeScheduleHero";

function prefersReducedMotion() {
  return window.matchMedia?.("(prefers-reduced-motion: reduce)").matches ?? false;
}

const scheduleMonthFormatter = dateTimeFormat("en-GB", {
  month: "long",
  year: "numeric",
  timeZone: "UTC",
});

const scheduleMonthOnlyFormatter = dateTimeFormat("en-GB", {
  month: "long",
  timeZone: "UTC",
});

function formatScheduleMonth(schedule: Pick<EmployeeSchedule, "year" | "month">) {
  return scheduleMonthFormatter.format(new Date(Date.UTC(schedule.year, schedule.month - 1, 1)));
}

function formatScheduleMonthOnly(schedule: Pick<EmployeeSchedule, "year" | "month">) {
  return scheduleMonthOnlyFormatter.format(new Date(Date.UTC(schedule.year, schedule.month - 1, 1)));
}

function getDaysInMonth(year: number, month: number) {
  return new Date(Date.UTC(year, month, 0)).getUTCDate();
}

function getWeekdayIndex(year: number, month: number, dayOfMonth: number) {
  return new Date(Date.UTC(year, month - 1, dayOfMonth)).getUTCDay();
}

function getSlotDurationHours(slot: EmployeeScheduleSlot) {
  const fromMinutes = parseTimeMinutes(slot.fromTime);
  const toMinutes = parseTimeMinutes(slot.toTime);

  if (fromMinutes === null || toMinutes === null) {
    return 0;
  }

  const durationMinutes = toMinutes > fromMinutes
    ? toMinutes - fromMinutes
    : toMinutes + 24 * 60 - fromMinutes;

  return durationMinutes / 60;
}

function formatHours(value: number) {
  const roundedValue = Math.round(value * 10) / 10;
  return t("{0}h", numberFormat("en-GB", { maximumFractionDigits: 1 }).format(roundedValue));
}

function isCurrentEmployeeSlot(slot: EmployeeScheduleSlot, employeeId: number | null) {
  if (employeeId === null) {
    return slot.employeeId === undefined || slot.employeeId === null;
  }

  return slot.employeeId === undefined || slot.employeeId === null || slot.employeeId === employeeId;
}

const summaryWeekdayLabels = ["su.", "mo.", "tu.", "we.", "th.", "fr.", "sa."] as const;

type ScheduleHoursSummaryRow = {
  key: string;
  dayOfMonth: number;
  dayLabel: string;
  hours: number;
  hoursText: string;
  scheduleName: string;
  sortValue: number;
};

type ScheduleSummaryPeriod = {
  key: string;
  year: number;
  month: number;
};

function formatSummaryDay(schedule: Pick<EmployeeSchedule, "year" | "month">, dayOfMonth: number) {
  const weekdayLabel = summaryWeekdayLabels[getWeekdayIndex(schedule.year, schedule.month, dayOfMonth)];
  return `${weekdayLabel}/${String(dayOfMonth).padStart(2, "0")}`;
}

function getScheduleSummaryName(schedule: EmployeeSchedule) {
  return schedule.name.trim() || schedule.shopName.trim() || schedule.containerName.trim() || t("Schedule #{0}", schedule.id);
}

function getScheduleSummaryPeriods(schedules: EmployeeSchedule[]) {
  const periodsByKey = new Map<string, ScheduleSummaryPeriod>();

  schedules.forEach(schedule => {
    const key = `${schedule.year}:${schedule.month}`;
    periodsByKey.set(key, {
      key,
      year: schedule.year,
      month: schedule.month,
    });
  });

  return [...periodsByKey.values()].sort((left, right) =>
    right.year - left.year || right.month - left.month,
  );
}

function buildScheduleHoursSummary(
  schedules: EmployeeSchedule[],
  period: ScheduleSummaryPeriod | null,
  employeeId: number | null,
) {
  if (!period) {
    return {
      rows: [] as ScheduleHoursSummaryRow[],
      totalHours: 0,
      totalHoursText: "0h",
    };
  }

  const rowsByDay = new Map<number, Omit<ScheduleHoursSummaryRow, "hoursText" | "scheduleName"> & {
    scheduleNames: string[];
  }>();
  schedules
    .filter(schedule => schedule.year === period.year && schedule.month === period.month)
    .forEach(schedule => {
      schedule.slots
        .filter(slot => isCurrentEmployeeSlot(slot, employeeId))
        .forEach(slot => {
          const existingRow = rowsByDay.get(slot.dayOfMonth);
          const scheduleName = getScheduleSummaryName(schedule);
          const nextHours = (existingRow?.hours ?? 0) + getSlotDurationHours(slot);
          rowsByDay.set(slot.dayOfMonth, {
            key: `day:${period.year}:${period.month}:${slot.dayOfMonth}`,
            dayOfMonth: slot.dayOfMonth,
            dayLabel: formatSummaryDay(schedule, slot.dayOfMonth),
            hours: nextHours,
            scheduleNames: existingRow?.scheduleNames.includes(scheduleName)
              ? existingRow.scheduleNames
              : [...(existingRow?.scheduleNames ?? []), scheduleName],
            sortValue: Date.UTC(schedule.year, schedule.month - 1, slot.dayOfMonth),
          });
        });
    });

  const workedRows = [...rowsByDay.values()]
    .map(row => ({
      ...row,
      hoursText: formatHours(row.hours),
      scheduleName: row.scheduleNames.sort((left, right) => left.localeCompare(right)).join(", "),
    }))
    .sort((left, right) => left.dayOfMonth - right.dayOfMonth);

  const workedRowsByDay = new Map(workedRows.map(row => [row.dayOfMonth, row]));

  const daysInMonth = getDaysInMonth(period.year, period.month);
  const rows: ScheduleHoursSummaryRow[] = [];
  for (let dayOfMonth = 1; dayOfMonth <= daysInMonth; dayOfMonth += 1) {
    const dayRow = workedRowsByDay.get(dayOfMonth);
    if (dayRow) {
      rows.push(dayRow);
      continue;
    }

    rows.push({
      key: `empty:${period.year}:${period.month}:${dayOfMonth}`,
      dayOfMonth,
      dayLabel: formatSummaryDay(period, dayOfMonth),
      hours: 0,
      hoursText: "-",
      scheduleName: "-",
      sortValue: Date.UTC(period.year, period.month - 1, dayOfMonth),
    });
  }

  const totalHours = rows.reduce((sum, row) => sum + row.hours, 0);

  return {
    rows,
    totalHours,
    totalHoursText: formatHours(totalHours),
  };
}

const employeeScheduleColumnOrderStorageVersion = 1;
const employeeScheduleColumnOrderStoragePrefix = "gf3:employee-schedule-column-order";

type StoredEmployeeScheduleColumnOrders = {
  version: number;
  scheduleOrders: Record<string, number[]>;
};

function getEmployeeScheduleColumnOrderStorageKey(employeeId: number | null, userName?: string | null) {
  const identity = employeeId && employeeId > 0
    ? "employee-" + employeeId
    : "user-" + encodeURIComponent(userName?.trim() || "unknown");
  return employeeScheduleColumnOrderStoragePrefix
    + ":v"
    + employeeScheduleColumnOrderStorageVersion
    + ":"
    + identity;
}

function sanitizeEmployeeScheduleColumnOrder(columnOrder: number[], columns: GraphMatrixColumn[]) {
  const availableIds = new Set(columns.map(column => column.employeeId));
  const seenIds = new Set<number>();
  const sanitizedOrder: number[] = [];

  columnOrder.forEach(columnId => {
    if (availableIds.has(columnId) && !seenIds.has(columnId)) {
      seenIds.add(columnId);
      sanitizedOrder.push(columnId);
    }
  });

  columns.forEach(column => {
    if (!seenIds.has(column.employeeId)) {
      sanitizedOrder.push(column.employeeId);
    }
  });

  return sanitizedOrder;
}

function applyEmployeeScheduleColumnOrder(columns: GraphMatrixColumn[], columnOrder: number[]) {
  const columnById = new Map(columns.map(column => [column.employeeId, column] as const));
  return sanitizeEmployeeScheduleColumnOrder(columnOrder, columns)
    .map(columnId => columnById.get(columnId))
    .filter((column): column is GraphMatrixColumn => Boolean(column));
}

function readEmployeeScheduleColumnOrders(storageKey: string): Record<string, number[]> {
  try {
    const parsed = JSON.parse(window.localStorage.getItem(storageKey) ?? "null") as StoredEmployeeScheduleColumnOrders | null;
    if (!parsed || parsed.version !== employeeScheduleColumnOrderStorageVersion || !parsed.scheduleOrders) {
      return {};
    }

    return Object.entries(parsed.scheduleOrders).reduce<Record<string, number[]>>((result, [scheduleId, columnOrder]) => {
      if (Array.isArray(columnOrder)) {
        result[scheduleId] = columnOrder.filter(columnId => Number.isInteger(columnId));
      }
      return result;
    }, {});
  } catch {
    return {};
  }
}

function writeEmployeeScheduleColumnOrders(storageKey: string, scheduleOrders: Record<string, number[]>) {
  const payload: StoredEmployeeScheduleColumnOrders = {
    version: employeeScheduleColumnOrderStorageVersion,
    scheduleOrders,
  };

  try {
    window.localStorage.setItem(storageKey, JSON.stringify(payload));
  } catch {
    // Keep customization available for this session when browser storage is unavailable.
  }
}
export function EmployeeSchedulePage() {
  const language = useLanguageRevision();
  const showPublicHolidays = usePublicHolidayVisibility();
  const { session } = useAuth();
  const scheduleQuery = useEmployeeScheduleListQuery();
  const shiftCorrectionsQuery = useEmployeeShiftCorrectionsQuery(Boolean(session?.employeeId));
  const shiftSwapsQuery = useEmployeeShiftSwapsQuery(Boolean(session?.employeeId));
  const createShiftCorrectionMutation = useCreateShiftCorrectionMutation();
  const { data: uiState, refetch: refetchUiState } = useEmployeeUiStateQuery(Boolean(session?.employeeId));
  const schedules = useMemo(() => scheduleQuery.data ?? [], [scheduleQuery.data]);
  const currentEmployeeId = session?.employeeId && session.employeeId > 0 ? session.employeeId : null;
  const currentMonth = warsawToday().slice(0, 7);
  const rankedSchedules = useMemo(
    () => currentEmployeeId === null ? [] : rankEmployeeSchedules(schedules, currentEmployeeId, currentMonth),
    [schedules, currentEmployeeId, currentMonth],
  );
  const columnOrderStorageKey = getEmployeeScheduleColumnOrderStorageKey(
    session?.employeeId ?? null,
    session?.userName,
  );
  const [savedColumnOrders, setSavedColumnOrders] = useState<Record<string, number[]>>(
    () => readEmployeeScheduleColumnOrders(columnOrderStorageKey),
  );
  const [columnOrderDialogEmployeeId, setColumnOrderDialogEmployeeId] = useState<number | null>(null);
  const [selectedScheduleId, setSelectedScheduleId] = useState<number | null>(null);
  const [isScheduleSelectOpen, setIsScheduleSelectOpen] = useState(false);
  const handleSelectSchedule = (scheduleId: number) => {
    setSelectedScheduleId(scheduleId);
    setIsScheduleSelectOpen(false);
  };
  const [scheduleViewMode, setScheduleViewMode] = useState<"matrix" | "daily">("matrix");
  const [hasSwitchedScheduleView, setHasSwitchedScheduleView] = useState(false);
  const scheduleViewToggleRef = useRef<HTMLButtonElement>(null);
  const handleToggleScheduleView = () => {
    setHasSwitchedScheduleView(true);
    setScheduleViewMode(current => current === "matrix" ? "daily" : "matrix");
  };
  useLayoutEffect(() => {
    if (hasSwitchedScheduleView) scheduleViewToggleRef.current?.focus({ preventScroll: true });
  }, [scheduleViewMode, hasSwitchedScheduleView]);
  const [selectedDayOfMonth, setSelectedDayOfMonth] = useState<number | null>(null);
  const activeDailyScheduleDayRef = useRef<HTMLButtonElement | null>(null);
  const [isShiftCorrectionDialogOpen, setIsShiftCorrectionDialogOpen] = useState(false);
  const correctionSendingRef = useRef(false);
  const [shiftCorrectionError, setShiftCorrectionError] = useState<string | null>(null);
  const [shiftCorrectionSubmitted, setShiftCorrectionSubmitted] = useState(false);
  const [preferenceSaveError, setPreferenceSaveError] = useState<string | null>(null);

  useEffect(() => {
    setSavedColumnOrders(readEmployeeScheduleColumnOrders(columnOrderStorageKey));
    setColumnOrderDialogEmployeeId(null);
  }, [columnOrderStorageKey]);

  useEffect(() => {
    if (!uiState) {
      return;
    }

    const localOrders = readEmployeeScheduleColumnOrders(columnOrderStorageKey);
    const serverOrders = uiState.scheduleColumnOrders ?? {};
    const mergedOrders = { ...localOrders, ...serverOrders };
    setSavedColumnOrders(mergedOrders);
    writeEmployeeScheduleColumnOrders(columnOrderStorageKey, mergedOrders);

    const localOnlyOrders = Object.entries(localOrders)
      .filter(([scheduleId]) => !Object.prototype.hasOwnProperty.call(serverOrders, scheduleId));
    if (localOnlyOrders.length > 0) {
      void Promise.all(localOnlyOrders.map(([scheduleId, columnOrder]) =>
        employeeUiStateApi.saveScheduleColumnOrder(Number(scheduleId), columnOrder),
      )).then(() => refetchUiState())
        .catch(error => setPreferenceSaveError(getErrorMessage(error, t("Could not sync the column order."))));
    }
  }, [columnOrderStorageKey, refetchUiState, setPreferenceSaveError, uiState]);
  const [selectedSummaryPeriodKey, setSelectedSummaryPeriodKey] = useState<string | null>(null);
  const selectedSchedule = useMemo(
    () => rankedSchedules.find(schedule => schedule.id === selectedScheduleId) ?? rankedSchedules[0] ?? null,
    [rankedSchedules, selectedScheduleId],
  );
  const pendingCorrectionDays = useMemo(
    () => getPendingCorrectionDays(selectedSchedule, currentEmployeeId, shiftCorrectionsQuery.data ?? []),
    [selectedSchedule, currentEmployeeId, shiftCorrectionsQuery.data],
  );
  const urlop = useEmployeeApprovedUrlopQuery(selectedSchedule?.shopId ?? 0, selectedSchedule?.year ?? 0, selectedSchedule?.month ?? 0);
  const urlopCellKeys = approvedKeys(urlop.data ?? [], selectedSchedule?.shopId ?? 0, selectedSchedule?.year ?? 2000, selectedSchedule?.month ?? 1);

  const { pdfExportError, isPdfLoading, isPdfPreparing, pdfFile, setPdfFile, handleExportPdf } = useSchedulePdfExport(
    selectedSchedule?.id, employeeScheduleApi.exportPdf, session?.employeeId, session?.userName,
  );

  useEffect(() => {
    setSelectedDayOfMonth(null);
  }, [selectedSchedule?.id]);

  const summaryPeriods = useMemo(
    () => getScheduleSummaryPeriods(schedules),
    [schedules],
  );
  const activeSummaryPeriod = useMemo(() => {
    const selectedPeriod = summaryPeriods.find(period => period.key === selectedSummaryPeriodKey);
    if (selectedPeriod) {
      return selectedPeriod;
    }

    const openedSchedulePeriod = selectedSchedule
      ? summaryPeriods.find(period => period.year === selectedSchedule.year && period.month === selectedSchedule.month)
      : null;
    return openedSchedulePeriod ?? summaryPeriods[0] ?? null;
  }, [selectedSchedule, selectedSummaryPeriodKey, summaryPeriods]);
  const summaryYears = useMemo(
    () => Array.from(new Set(summaryPeriods.map(period => period.year))),
    [summaryPeriods],
  );
  const summaryMonths = useMemo(() => {
    if (!activeSummaryPeriod) {
      return [];
    }

    return summaryPeriods
      .filter(period => period.year === activeSummaryPeriod.year)
      .slice()
      .sort((left, right) => left.month - right.month);
  }, [activeSummaryPeriod, summaryPeriods]);
  const summaryMonthOptions = useMemo<SearchableSelectOption[]>(
    () => summaryMonths.map(period => ({
      value: String(period.month),
      label: formatScheduleMonthOnly(period),
    })),
    // eslint-disable-next-line react-hooks/exhaustive-deps -- Translation helpers read the current language internally.
    [summaryMonths, language],
  );
  const summaryYearOptions = useMemo<SearchableSelectOption[]>(
    () => summaryYears.map(year => ({ value: String(year), label: String(year) })),
    [summaryYears],
  );
  const displayName = session?.displayName?.trim() || session?.userName || t("Employee");
  const shiftStatuses = useMemo(
    () => buildScheduleShiftStatuses(selectedSchedule, currentEmployeeId, displayName, pendingCorrectionDays, shiftSwapsQuery.data ?? []),
    [selectedSchedule, currentEmployeeId, displayName, pendingCorrectionDays, shiftSwapsQuery.data],
  );
  const matrixBadges = useMemo(() => new Map(
    [...shiftStatuses].flatMap(([day, employees]) => [...employees].map(([id, status]) => [
      getGraphCellKey(id, day),
      {
        content: <ScheduleShiftBadges status={status} />,
        tone: getScheduleStatusTone(status),
        count: Number(Boolean(status.correctionPending)) + Number(status.open.length > 0) + Number(status.accepted.length > 0),
      },
    ] as const)),
  ), [language, shiftStatuses]);
  const fallbackMatrixEmployeeId = currentEmployeeId ?? 1;
  const dailyScheduleDays = useMemo(
    () => buildDailyScheduleDays(selectedSchedule, currentEmployeeId),
    // eslint-disable-next-line react-hooks/exhaustive-deps -- Translation helpers read the current language internally.
    [currentEmployeeId, selectedSchedule, language],
  );
  const defaultDailyScheduleDay = useMemo(
    () => getDefaultDailyScheduleDay(selectedSchedule, dailyScheduleDays),
    [dailyScheduleDays, selectedSchedule],
  );
  const activeDailyScheduleDay = selectedDayOfMonth && selectedDayOfMonth <= dailyScheduleDays.length
    ? selectedDayOfMonth
    : defaultDailyScheduleDay;

  useLayoutEffect(() => {
    if (scheduleViewMode !== "daily") {
      return;
    }

    activeDailyScheduleDayRef.current?.scrollIntoView({
      behavior: prefersReducedMotion() ? "auto" : "smooth",
      block: "nearest",
      inline: "center",
    });
  }, [activeDailyScheduleDay, scheduleViewMode, selectedSchedule?.id]);

  const dailyScheduleWorkers = useMemo(
    () => selectedSchedule
      ? buildDailyScheduleWorkers(selectedSchedule, activeDailyScheduleDay, fallbackMatrixEmployeeId, displayName)
      : [],
  // eslint-disable-next-line react-hooks/exhaustive-deps -- Translation helpers read the current language internally.
    [activeDailyScheduleDay, displayName, fallbackMatrixEmployeeId, selectedSchedule, language],
  );
  const scheduleHoursSummary = useMemo(
    () => buildScheduleHoursSummary(schedules, activeSummaryPeriod, currentEmployeeId),
  // eslint-disable-next-line react-hooks/exhaustive-deps -- Translation helpers read the current language internally.
    [activeSummaryPeriod, currentEmployeeId, schedules, language],
  );
  const defaultScheduleMatrixColumns = useMemo(
    () => buildScheduleMatrixColumns(selectedSchedule, fallbackMatrixEmployeeId, displayName),
  // eslint-disable-next-line react-hooks/exhaustive-deps -- Translation helpers read the current language internally.
    [displayName, fallbackMatrixEmployeeId, selectedSchedule, language],
  );
  const scheduleMatrixColumns = useMemo(() => {
    if (!selectedSchedule) {
      return defaultScheduleMatrixColumns;
    }

    const savedOrder = savedColumnOrders[String(selectedSchedule.id)] ?? [];
    return applyEmployeeScheduleColumnOrder(defaultScheduleMatrixColumns, savedOrder);
  }, [defaultScheduleMatrixColumns, savedColumnOrders, selectedSchedule]);
  const scheduleMatrixCellMap = useMemo(
    () => buildScheduleMatrixCellMap(selectedSchedule, fallbackMatrixEmployeeId),
    [fallbackMatrixEmployeeId, selectedSchedule],
  );
  const scheduleMatrixDisplay = useMemo(
    () => buildScheduleMatrixDisplayCellMap(selectedSchedule, scheduleMatrixCellMap),
    [scheduleMatrixCellMap, selectedSchedule],
  );
  const queryErrorMessage = scheduleQuery.error || urlop.error
    ? getErrorMessage(scheduleQuery.error || urlop.error, t("Could not load published schedules."))
    : null;

  const handleSummaryYearChange = (value: string) => {
    const year = Number(value);
    const nextPeriod = summaryPeriods.find(period =>
      period.year === year && period.month === activeSummaryPeriod?.month,
    ) ?? summaryPeriods.find(period => period.year === year);

    if (nextPeriod) {
      setSelectedSummaryPeriodKey(nextPeriod.key);
    }
  };

  const handleSummaryMonthChange = (value: string) => {
    const month = Number(value);
    const nextPeriod = summaryPeriods.find(period =>
      period.year === activeSummaryPeriod?.year && period.month === month,
    );

    if (nextPeriod) {
      setSelectedSummaryPeriodKey(nextPeriod.key);
    }
  };

  const handleSaveColumnOrder = (columnOrder: number[]) => {
    if (!selectedSchedule) {
      return;
    }

    const scheduleKey = String(selectedSchedule.id);
    const sanitizedOrder = sanitizeEmployeeScheduleColumnOrder(columnOrder, defaultScheduleMatrixColumns);
    const defaultOrder = defaultScheduleMatrixColumns.map(column => column.employeeId);
    const nextSavedOrders = { ...savedColumnOrders };

    if (sanitizedOrder.every((columnId, index) => columnId === defaultOrder[index])) {
      delete nextSavedOrders[scheduleKey];
    } else {
      nextSavedOrders[scheduleKey] = sanitizedOrder;
    }

    setSavedColumnOrders(nextSavedOrders);
    writeEmployeeScheduleColumnOrders(columnOrderStorageKey, nextSavedOrders);
    setPreferenceSaveError(null);
    void employeeUiStateApi
      .saveScheduleColumnOrder(selectedSchedule.id, sanitizedOrder)
      .catch(error => setPreferenceSaveError(getErrorMessage(error, t("Could not sync the column order."))));
    setColumnOrderDialogEmployeeId(null);
  };

  const handleSubmitShiftCorrection = (input: CreateShiftCorrectionInput) => {
    if (shiftCorrectionSubmitted || correctionSendingRef.current || createShiftCorrectionMutation.isPending) return;
    correctionSendingRef.current = true;
    setShiftCorrectionError(null);
    createShiftCorrectionMutation.mutate(input, {
      onSuccess: () => {
        correctionSendingRef.current = false;
        setShiftCorrectionSubmitted(true);
      },
      onError: error => {
        correctionSendingRef.current = false;
        setShiftCorrectionError(getErrorMessage(error, t("Could not send the shift correction request.")));
      },
    });
  };

  const scheduleSelectorRow = selectedSchedule ? (
    <div className={styles.scheduleSelectorRow}>
      <button type="button" className={styles.scheduleSelectTrigger}
        aria-haspopup="dialog" aria-expanded={isScheduleSelectOpen}
        aria-label={t("Select schedule. Current schedule: {0}", selectedSchedule.name)}
        onClick={() => setIsScheduleSelectOpen(true)}>
        <span className={styles.scheduleSelectTriggerIcon} aria-hidden="true"><ScheduleIcon size={20} /></span>
        <span className={styles.scheduleSelectDivider} aria-hidden="true" />
        <span className={styles.scheduleSelectTriggerCopy}>
          <span className={styles.scheduleSelectNameRow}>
            <span className={styles.scheduleSelectName}>{selectedSchedule.name}</span>
            <span className={`${styles.scheduleSelectChevron} ${isScheduleSelectOpen ? styles.scheduleSelectChevronOpen : ""}`} aria-hidden="true" />
          </span>
          <span className={styles.scheduleSelectPeriod}>{formatScheduleMonth(selectedSchedule)}</span>
          <span className={styles.scheduleSelectUpdated}>{selectedSchedule.lastUpdatedAtUtc
            ? t("Updated {0}", formatScheduleLastUpdate(selectedSchedule.lastUpdatedAtUtc))
            : formatScheduleLastUpdate(selectedSchedule.lastUpdatedAtUtc)}</span>
        </span>
      </button>
      <button type="button" className={`${styles.openSchedulePdfButton} ${styles.openScheduleCorrectionButton}`}
        onClick={() => { setShiftCorrectionError(null); setShiftCorrectionSubmitted(false); setIsShiftCorrectionDialogOpen(true); }}
        title={t("Request a shift correction")} aria-label={t("Request a shift correction")}>{t("Adjust")}</button>
    </div>
  ) : null;

  const scheduleHeaderActions = (
    <EmployeeScheduleHeaderActions daily={scheduleViewMode === "daily"} toggleRef={scheduleViewToggleRef}
      onToggle={handleToggleScheduleView} onExportPdf={handleExportPdf} pdfLoading={isPdfLoading} pdfReady={Boolean(pdfFile)} />
  );

  return (
    <div className={workspaceStyles.page}>
      {isPdfPreparing ? createPortal(
        <div className={`${loadingStyles.overlay} ${styles.pdfLoadingOverlay}`} role="status" aria-live="polite" aria-label={t("Creating PDF...")} aria-busy="true">
          <div className={loadingStyles.panel}>
            <div className={loadingStyles.hero}>
              <div className={loadingStyles.spinnerShell} aria-hidden="true">
                <span className={loadingStyles.spinnerRing} />
                <span className={loadingStyles.spinnerCore}><NewsIcon size={22} /></span>
              </div>
              <div className={loadingStyles.copy}>
                <h3>{t("Creating PDF...")}</h3>
                <p>{t("Please wait while we prepare your schedule for sharing.")}</p>
              </div>
            </div>
          </div>
        </div>, document.body,
      ) : null}
      {queryErrorMessage ? <div data-employee-motion="schedule-content" data-motion-key={`queryErrorMessage:${queryErrorMessage}`}><ErrorBanner dismissible={false}>{queryErrorMessage}</ErrorBanner></div> : null}
      {pdfExportError ? <div data-employee-motion="schedule-content" data-motion-key={`pdfExportError:${pdfExportError}`}><ErrorBanner dismissible={false}>{pdfExportError}</ErrorBanner></div> : null}
      {pdfFile ? <div className={styles.shiftCorrectionSuccess} role="status">
        <p>{t("PDF is ready. Tap PDF again to share, or download it.")}</p>
        <button type="button" className={styles.openSchedulePdfButton} disabled={isPdfLoading} onClick={() => {
          downloadExportFile(pdfFile, pdfFile.fileName || "GF3_Graph.pdf");
          setPdfFile(null);
        }}>{t("Download PDF")}</button>
      </div> : null}
      {preferenceSaveError ? <div data-employee-motion="schedule-content" data-motion-key={`preferenceSaveError:${preferenceSaveError}`}><ErrorBanner dismissible>{preferenceSaveError}</ErrorBanner></div> : null}

      <EmployeeScheduleHero
        schedules={schedules}
        employeeId={currentEmployeeId}
        displayName={displayName}
        isLoading={scheduleQuery.isLoading}
        hasError={Boolean(scheduleQuery.error)}
      />

      {scheduleQuery.isLoading ? (
        <section className={workspaceStyles.panel} data-employee-motion="schedule-panel">
          <span className={workspaceStyles.panelEyebrow}>{t("Loading")}</span>
          <p className={workspaceStyles.panelText}>{t("Checking public schedules for your account.")}</p>
        </section>
      ) : null}

      {!scheduleQuery.isLoading && schedules.length === 0 ? (
        <section className={workspaceStyles.panel} data-employee-motion="schedule-panel">
          <span className={workspaceStyles.panelEyebrow}>{t("No published schedules")}</span>
          <h2 className={workspaceStyles.panelTitle}>{t("Nothing is public for your account yet.")}</h2>
          <NavLink to="/availability" className={workspaceStyles.linkCard}>
            <span className={workspaceStyles.linkIcon}>
              <AvailabilityIcon size={18} />
            </span>
            <span className={workspaceStyles.linkTitle}>{t("Go to Availability")}</span>
            <span className={workspaceStyles.linkText}>{t("Submit availability when a manager opens a window.")}</span>
          </NavLink>
        </section>
      ) : null}

      {selectedSchedule ? (
        <div className={styles.scheduleViewStage} data-employee-motion="schedule-content" data-motion-key={`${selectedSchedule.id}:${scheduleViewMode}`}>
          {scheduleViewMode === "matrix" ? (
            <ContainerGraphMatrix
              urlopCellKeys={urlopCellKeys}
              className={styles.openScheduleMatrix}
              icon={null}
              graph={selectedSchedule}
              columns={scheduleMatrixColumns}
              cellMap={scheduleMatrixDisplay.cellMap}
              mutedSuffixMap={scheduleMatrixDisplay.mutedSuffixMap}
              readOnlyCellAdornments={matrixBadges}
              title={t("Schedules")}
              headerCenterSlot={scheduleSelectorRow}
              headerClassName={styles.openScheduleHeader}
              titleClassName={styles.openScheduleHeaderTitle}
              headerRightClassName={styles.openScheduleHeaderRight}
              readOnly
              compactSize
              compactHeader
              neutralStyle
              showColumnTotals={false}
              allowColumnResize={false}
              stretchColumns={false}
              emptyMessage={t("No assigned shifts in this schedule yet.")}
              onColumnHeaderClick={column => setColumnOrderDialogEmployeeId(column.employeeId)}
              headerRightSlot={scheduleHeaderActions}
            />
          ) : null}

          {scheduleViewMode === "daily" ? (
            <CardSection
              className={styles.dailyScheduleCard}
              title={t("Schedules")}
              headerCenterSlot={scheduleSelectorRow}
              headerClassName={styles.openScheduleHeader}
              titleClassName={styles.openScheduleHeaderTitle}
              headerRightClassName={styles.openScheduleHeaderRight}
              headerRightSlot={scheduleHeaderActions}
            >
              <EmployeeScheduleDailyBody
                panelId="daily-schedule-content"
                selectedSchedule={selectedSchedule}
                dailyScheduleDays={dailyScheduleDays}
                activeDailyScheduleDay={activeDailyScheduleDay}
                dailyScheduleWorkers={dailyScheduleWorkers}
                shiftStatuses={shiftStatuses.get(activeDailyScheduleDay)}
                activeDailyScheduleDayRef={activeDailyScheduleDayRef}
                showPublicHolidays={showPublicHolidays}
                onSelectDay={setSelectedDayOfMonth}
              />
            </CardSection>
          ) : null}

        </div>
      ) : null}

      <EmployeeScheduleSelectDialog
        open={isScheduleSelectOpen}
        schedules={rankedSchedules}
        selectedScheduleId={selectedSchedule?.id ?? null}
        onSelect={handleSelectSchedule}
        onClose={() => setIsScheduleSelectOpen(false)}
      />
      <EmployeeScheduleColumnOrderDialog
        open={columnOrderDialogEmployeeId !== null}
        columns={scheduleMatrixColumns}
        defaultColumns={defaultScheduleMatrixColumns}
        activeEmployeeId={columnOrderDialogEmployeeId}
        onCancel={() => setColumnOrderDialogEmployeeId(null)}
        onSave={handleSaveColumnOrder}
      />

      <EmployeeShiftCorrectionDialog
        open={isShiftCorrectionDialogOpen}
        schedules={rankedSchedules}
        employeeId={currentEmployeeId}
        existingRequests={shiftCorrectionsQuery.data ?? []}
        isSending={createShiftCorrectionMutation.isPending}
        errorMessage={shiftCorrectionError}
        submitted={shiftCorrectionSubmitted}
        onCancel={() => { setIsShiftCorrectionDialogOpen(false); setShiftCorrectionError(null); }}
        onSubmit={handleSubmitShiftCorrection}
      />

      {selectedSchedule ? (
        <section className={`${workspaceStyles.panel} ${styles.hoursSummaryPanel}`} data-employee-motion="schedule-panel">
          <div className={styles.hoursSummaryHeader}>
            <div className={styles.hoursSummaryHeading}>
              <span className={styles.summaryIcon} aria-hidden="true">
                <StatisticsIcon size={20} />
              </span>
              <div>
                <span className={workspaceStyles.panelEyebrow}>{t("Work hours")}</span>
                <h2 className={workspaceStyles.panelTitle}>{t("Summary")}</h2>
              </div>
            </div>

            <span className={styles.hoursSummaryTotalPill} data-employee-motion="schedule-value" data-motion-key={`${activeSummaryPeriod?.key}:${scheduleHoursSummary.totalHoursText}`}>{scheduleHoursSummary.totalHoursText}</span>
          </div>

          <div className={styles.summaryPeriodControls} role="group" aria-label={t("Summary period")}>
            <SearchableSelect
              ariaLabel={t("Summary month")}
              value={activeSummaryPeriod ? String(activeSummaryPeriod.month) : ""}
              options={summaryMonthOptions}
              placeholder={t("Month")}
              dropdownTitle={t("Month")}
              leadingIcon={<ScheduleIcon size={16} />}
              size="summary"
              shadow="soft"
              searchEnabled={false}
              showSelectedHint={false}
              disabled={summaryMonths.length <= 1}
              className={styles.summaryPeriodSelect}
              onChange={handleSummaryMonthChange}
            />

            <SearchableSelect
              ariaLabel={t("Summary year")}
              value={activeSummaryPeriod ? String(activeSummaryPeriod.year) : ""}
              options={summaryYearOptions}
              placeholder={t("Year")}
              dropdownTitle={t("Year")}
              leadingIcon={<ScheduleIcon size={16} />}
              size="summary"
              shadow="soft"
              searchEnabled={false}
              showSelectedHint={false}
              disabled={summaryYears.length <= 1}
              className={styles.summaryPeriodSelect}
              onChange={handleSummaryYearChange}
            />
          </div>

          {scheduleHoursSummary.rows.length > 0 ? (
            <div className={styles.hoursSummaryGrid} data-employee-motion="schedule-content" data-motion-key={`${activeSummaryPeriod?.key}:rows`} role="table" aria-label={t("Schedule hours summary")}>
              <div className={styles.hoursSummaryGridHeader} role="row">
                <span role="columnheader">{t("Day")}</span>
                <span role="columnheader">{t("Hours")}</span>
                <span role="columnheader">{t("Schedule")}</span>
              </div>

              {scheduleHoursSummary.rows.map(row => {
                const holiday = showPublicHolidays && activeSummaryPeriod
                  ? getPolishPublicHoliday(activeSummaryPeriod.year, activeSummaryPeriod.month, row.dayOfMonth) : null;
                return (
                  <div key={row.key} className={styles.hoursSummaryGridRow} role="row">
                    <span role="cell" className={holiday ? styles.hoursSummaryDateHoliday : undefined}
                      title={holiday ? t(holiday.name) : undefined}
                      aria-label={holiday ? `${row.dayLabel}, ${t("Holiday")}: ${t(holiday.name)}` : undefined}>{row.dayLabel}</span>
                    <strong role="cell">{row.hoursText}</strong>
                    <span role="cell">{row.scheduleName}</span>
                  </div>
                );
              })}

              <div className={styles.hoursSummaryTotalRow} role="row">
                <span role="cell" className={styles.hoursSummaryTotalLabel}><span aria-hidden="true">Σ</span> {t("Total")}</span>
                <strong role="cell">{scheduleHoursSummary.totalHoursText}</strong>
                <span role="cell">{t("All schedules")}</span>
              </div>
            </div>
          ) : (
            <p className={styles.hoursSummaryEmpty} data-employee-motion="schedule-content" data-motion-key={`${activeSummaryPeriod?.key}:empty`}>{t("No assigned shifts in this period.")}</p>
          )}
        </section>
      ) : null}
    </div>
  );
}
