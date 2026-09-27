import { dateTimeFormat, t } from "@shared/i18n";
import { useEffect, useMemo, useState, type ReactNode } from "react";
import type { Employee } from "@entities/employees/model/types";
import { getEmployeeFullName } from "@entities/employees/model/presentation";
import type { AvailabilityGroup } from "@entities/availability-groups/model/types";
import type { AvailabilityMatrixCellMap, AvailabilityMatrixColumn } from "@entities/availability-groups/model/editor";
import { AvailabilityBindCard } from "@entities/availability-groups/ui/AvailabilityBindCard";
import { AvailabilitySidebarCollapseButton, AvailabilitySidebarSection } from "@entities/availability-groups/ui/AvailabilitySidebarSection";
import { AvailabilityWorkspaceLayout } from "@entities/availability-groups/ui/AvailabilityWorkspaceLayout";
import type {
  ContainerGraphFormErrors,
  ContainerGraphFormState,
} from "@entities/containers/model/graphForm";
import type {
  GraphMatrixCellMap,
  GraphMatrixColumn,
  GraphRelatedScheduleHintDetailMap,
  GraphMatrixStyleMap,
  GraphTotals,
} from "@entities/containers/model/graphWorkspace";
import { normalizeGraphCellValue } from "@entities/containers/model/graphWorkspace";
import type {
  ManagerGraphFillColorBindDto,
  ManagerGraphTextColorBindDto,
  SaveSchedulePresetDto,
} from "@entities/containers/api/dto";
import type { Graph, SchedulePreset } from "@entities/containers/model/types";
import type { Shop } from "@entities/shops/model/types";
import {
  useGraphShiftSwapHighlightSettingQuery,
  useSaveGraphShiftSwapHighlightSettingMutation,
  type ShiftSwap,
} from "@entities/shift-swaps";
import { filterAcceptedShiftSwapHistory } from "@entities/shift-swaps/model/history";
import { getErrorMessage } from "@shared/api/httpClient";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import { SearchableSelect, type SearchableSelectOption } from "@shared/ui/components/SearchableSelect";
import { LabeledField, TextArea } from "@shared/ui/forms/Field";
import {
  BackIcon,
  BindIcon,
  ClearFormatAllIcon,
  ClearFormatIcon,
  CloseIcon,
  EmployeeIcon,
  EyeIcon,
  InformationIcon,
  NoteIcon,
  PlusIcon,
  SaveIcon,
  ScheduleIcon,
  ScheduleDetailsIcon,
  SearchIcon,
  SwapHistoryIcon,
} from "@shared/ui/icons";
import { CardSection } from "@shared/ui/sections/CardSection";
import { ContainerGraphColorDialog } from "./ContainerGraphColorDialog";
import { ContainerGraphHighlightColorDialog } from "./ContainerGraphHighlightColorDialog";
import { ContainerGraphDetailsFields } from "./ContainerGraphDetailsFields";
import {
  ContainerGraphManualColumnsCard,
  type ManualColumnShiftPublicationInput,
  type PendingManualColumnShiftPublication,
} from "./ContainerGraphManualColumnsCard";
import { ContainerGraphMatrix } from "./ContainerGraphMatrix";
import { ContainerGraphPresetDialog } from "./ContainerGraphPresetDialog";
import { ContainerGraphRelatedHintDialog } from "./ContainerGraphRelatedHintDialog";
import { ContainerGraphShiftCorrectionsCard } from "./ContainerGraphShiftCorrectionsCard";
import { ContainerGraphVersionsDialog } from "./ContainerGraphVersionsDialog";
import { ContainerGraphPresetSelect } from "./ContainerGraphPresetSelect";
import { ShiftSwapHistoryDialog } from "./ShiftSwapHistoryDialog";
import styles from "./ContainerGraphEditor.module.css";

const DESKTOP_MEDIA_QUERY = "(min-width: 961px)";

export type EditableGraphEmployeeRow = {
  id: number | null;
  employeeId: number;
  minHoursMonth: string;
};

export type EditableGraphBindRow = {
  clientId: string;
  id: number | null;
  key: string;
  value: string;
  isActive: boolean;
};

export type EditableGraphManualColumn = {
  columnId: number;
  label: string;
  cells: Record<string, string>;
};

type SidebarSectionKey = "details" | "publication" | "corrections" | "employees" | "bind" | "manualColumns" | "note";
type ColorDialogMode = "fill" | "text";
type PreviewLayoutMode = "side" | "stacked";
type ToolbarActionButtonProps = {
  label: string;
  icon?: ReactNode;
  disabled?: boolean;
  onClick: () => void;
};

type ContainerGraphEditorProps = {
  graph?: Graph | null;
  isHeaderCollapsed?: boolean;
  compactSize?: boolean;
  form: ContainerGraphFormState;
  formErrors: ContainerGraphFormErrors;
  shops: Shop[];
  availabilityGroups: AvailabilityGroup[];
  employees: Employee[];
  schedulePresets: SchedulePreset[];
  shiftSwapLog?: ShiftSwap[];
  isShiftSwapLogLoading?: boolean;
  selectedSchedulePresetId: number | null;
  graphEmployeeRows: EditableGraphEmployeeRow[];
  binds: EditableGraphBindRow[];
  manualColumns: EditableGraphManualColumn[];
  pendingManualShiftPublishes?: PendingManualColumnShiftPublication[];
  selectedEmployeeId: number | null;
  selectedBindClientId: string | null;
  scheduleColumns: GraphMatrixColumn[];
  cellMap: GraphMatrixCellMap;
  visualHintMap?: GraphMatrixCellMap;
  visualHintDetailMap?: GraphRelatedScheduleHintDetailMap;
  cellErrors: Record<string, string>;
  validationSignal?: number;
  styleMap: GraphMatrixStyleMap;
  dayConflictMap: Record<number, boolean>;
  totals: GraphTotals;
  previewColumns: AvailabilityMatrixColumn[];
  previewCellMap: AvailabilityMatrixCellMap;
  previewYear: number;
  previewMonth: number;
  previewAvailabilitySelection: string;
  previewAvailabilityOptions: SearchableSelectOption[];
  bindValueByKey: ReadonlyMap<string, string>;
  valueBindKeys: ReadonlySet<string>;
  fillColorBinds: ManagerGraphFillColorBindDto[];
  fillColorByKey: ReadonlyMap<string, string>;
  textColorBinds: ManagerGraphTextColorBindDto[];
  textColorByKey: ReadonlyMap<string, string>;
  selectedCellKeys: string[];
  previewSelectedCellKeys: string[];
  fillColor: string;
  textColor: string;
  isLoading: boolean;
  hasLoadError: boolean;
  isSaving: boolean;
  isGenerating: boolean;
  isStylingBusy: boolean;
  hasUnsavedChanges?: boolean;
  showMatrixSaveAction?: boolean;
  isBindsLoading: boolean;
  isBindBusy: boolean;
  isFillColorBindBusy: boolean;
  isTextColorBindBusy: boolean;
  isSchedulePresetsLoading: boolean;
  isPublishingManualShift?: boolean;
  isCancellingManualShift?: boolean;
  submitError?: string;
  bindErrorMessage?: string;
  manualShiftPublishError?: string | null;
  onFieldChange: <K extends keyof ContainerGraphFormState>(field: K) => (value: ContainerGraphFormState[K]) => void;
  onSelectedEmployeeIdChange: (value: number | null) => void;
  onPreviewAvailabilitySelectionChange: (value: string) => void;
  onSelectedBindChange: (clientId: string | null) => void;
  onApplySchedulePreset: (presetId: number) => void;
  onCreateSchedulePreset: (payload: SaveSchedulePresetDto) => Promise<void>;
  onBindFieldChange: (clientId: string, patch: Partial<Pick<EditableGraphBindRow, "key" | "value" | "isActive">>) => void;
  onBindCommit: (clientId: string) => void;
  onAddEmployee: () => void;
  onRemoveEmployee: (employeeId: number) => void;
  onAddBind: () => void;
  onDeleteBind: () => void;
  onManualColumnLabelChange: (columnId: number, value: string) => void;
  onAddManualColumn: () => void;
  onDeleteManualColumn: (columnId: number) => void;
  onPublishManualShift: (input: ManualColumnShiftPublicationInput) => void;
  onCancelManualShift: (shiftId: number) => void;
  onCancelPendingManualShift: (clientId: string) => void;
  onEmployeeMinHoursChange: (employeeId: number, value: string) => void;
  onColumnMove: (employeeId: number, targetEmployeeId: number) => void;
  onCellChange: (employeeId: number, dayOfMonth: number, value: string) => void;
  onSelectedCellKeysChange: (keys: string[]) => void;
  onPreviewSelectedCellKeysChange: (keys: string[]) => void;
  onFillColorChange: (value: string) => void;
  onTextColorChange: (value: string) => void;
  onApplyFillColor: () => void;
  onApplyFillColorShortcut: (fillColor: string, cellKeys: string[]) => void;
  onBindFillColor: (key: string, fillColor: string) => Promise<void>;
  onDeleteFillColorBind: (id: number) => Promise<void>;
  onApplyTextColor: () => void;
  onApplyTextColorShortcut: (textColor: string, cellKeys: string[]) => void;
  onBindTextColor: (key: string, textColor: string) => Promise<void>;
  onDeleteTextColorBind: (id: number) => Promise<void>;
  onClearCellStyle: () => void;
  onClearAllCellStyles: () => void;
  canUndo: boolean;
  onUndo: () => void;
  onVersionCheckoutComplete: () => void;
  onSave: () => void;
  onGenerate: () => void;
};

function joinClassNames(...values: Array<string | undefined | false>) {
  return values.filter(Boolean).join(" ");
}

function formatGraphHintDateLabel(year: number, month: number, dayOfMonth: number) {
  return dateTimeFormat("en-GB", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    timeZone: "UTC",
  }).format(new Date(Date.UTC(year, month - 1, dayOfMonth)));
}

const shiftSwapLogDayFormatter = dateTimeFormat("en-GB", {
  day: "2-digit",
  month: "short",
  timeZone: "UTC",
});

const shiftSwapLogAcceptedAtFormatter = dateTimeFormat("en-GB", {
  day: "2-digit",
  month: "short",
  hour: "2-digit",
  minute: "2-digit",
  hourCycle: "h23",
});

function formatShiftSwapLogDay(item: ShiftSwap) {
  return shiftSwapLogDayFormatter.format(new Date(Date.UTC(item.year, item.month - 1, item.dayOfMonth)));
}

function formatShiftSwapLogTime(value: string) {
  return value.slice(0, 5);
}

function formatShiftSwapLogHours(value: number) {
  const normalized = Math.abs(value) < 0.05 ? 0 : value;
  return `${Number.isInteger(normalized) ? normalized.toFixed(0) : normalized.toFixed(1)}h`;
}

function formatShiftSwapAcceptedAt(value?: string | null) {
  if (!value) {
    return t("Unknown");
  }

  return shiftSwapLogAcceptedAtFormatter.format(new Date(value));
}

function formatShiftSwapLogShift(item: ShiftSwap) {
  const locationLabel = item.shopName || item.containerName || item.scheduleName;
  const timeLabel = `${formatShiftSwapLogTime(item.fromTime)}-${formatShiftSwapLogTime(item.toTime)}`;
  const baseLabel = `${formatShiftSwapLogDay(item)} ${timeLabel} (${formatShiftSwapLogHours(item.shiftHours)})`;

  return locationLabel ? `${baseLabel} - ${locationLabel}` : baseLabel;
}

type SplitColorButtonProps = {
  label: string;
  color: string;
  disabled?: boolean;
  pickerDisabled?: boolean;
  onApply: () => void;
  onOpenDialog: () => void;
};

function SplitColorButton({
  label,
  color,
  disabled = false,
  pickerDisabled = false,
  onApply,
  onOpenDialog,
}: SplitColorButtonProps) {
  return (
    <div className={styles.splitColorButton}>
      <button
        type="button"
        className={styles.splitColorApply}
        disabled={disabled}
        onClick={onApply}
      >
        <span className={styles.splitColorSwatch} style={{ backgroundColor: color }} />
        <span className={styles.splitColorText}>
          <span className={styles.splitColorLabel}>{label}</span>
          <span className={styles.splitColorValue}>{color.toUpperCase()}</span>
        </span>
      </button>

      <button
        type="button"
        className={styles.splitColorPicker}
        disabled={pickerDisabled}
        aria-label={t("Open {0} color palette", label.toLowerCase())}
        onClick={onOpenDialog}
      >
        <span className={styles.splitColorDots} aria-hidden="true" />
      </button>
    </div>
  );
}

function ToolbarActionButton({
  label,
  icon,
  disabled = false,
  onClick,
}: ToolbarActionButtonProps) {
  return (
    <button
      type="button"
      className={styles.toolbarActionButton}
      disabled={disabled}
      onClick={onClick}
    >
      {icon ? <span className={styles.toolbarActionIcon}>{icon}</span> : null}
      <span>{label}</span>
    </button>
  );
}

type PreviewLayoutButtonProps = {
  mode: PreviewLayoutMode;
  active: boolean;
  onClick: () => void;
};

function PreviewLayoutButton({ mode, active, onClick }: PreviewLayoutButtonProps) {
  return (
    <button
      type="button"
      className={joinClassNames(styles.previewLayoutButton, active && styles.previewLayoutButtonActive)}
      aria-pressed={active}
      aria-label={mode === "side" ? t("Show Availability Preview on the right") : t("Show Availability Preview below")}
      onClick={onClick}
    >
      <span
        className={joinClassNames(
          styles.previewLayoutGlyph,
          mode === "side" ? styles.previewLayoutGlyphSide : styles.previewLayoutGlyphStacked,
        )}
        aria-hidden="true"
      >
        <span />
        <span />
      </span>
    </button>
  );
}

export function ContainerGraphEditor({
  graph,
  isHeaderCollapsed = false,
  compactSize = false,
  form,
  formErrors,
  shops,
  availabilityGroups,
  employees,
  schedulePresets,
  shiftSwapLog = [],
  isShiftSwapLogLoading = false,
  selectedSchedulePresetId,
  graphEmployeeRows,
  binds,
  manualColumns,
  pendingManualShiftPublishes = [],
  selectedEmployeeId,
  selectedBindClientId,
  scheduleColumns,
  cellMap,
  visualHintMap,
  visualHintDetailMap = {},
  cellErrors,
  validationSignal = 0,
  styleMap,
  dayConflictMap,
  totals,
  previewColumns,
  previewCellMap,
  previewYear,
  previewMonth,
  previewAvailabilitySelection,
  previewAvailabilityOptions,
  bindValueByKey,
  valueBindKeys,
  fillColorBinds,
  fillColorByKey,
  textColorBinds,
  textColorByKey,
  selectedCellKeys,
  previewSelectedCellKeys,
  fillColor,
  textColor,
  isLoading,
  hasLoadError,
  isSaving,
  isGenerating,
  isStylingBusy,
  hasUnsavedChanges = false,
  showMatrixSaveAction = true,
  isBindsLoading,
  isBindBusy,
  isFillColorBindBusy,
  isTextColorBindBusy,
  isSchedulePresetsLoading,
  isPublishingManualShift = false,
  isCancellingManualShift = false,
  submitError,
  bindErrorMessage,
  manualShiftPublishError = null,
  onFieldChange,
  onSelectedEmployeeIdChange,
  onPreviewAvailabilitySelectionChange,
  onSelectedBindChange,
  onApplySchedulePreset,
  onCreateSchedulePreset,
  onBindFieldChange,
  onBindCommit,
  onAddEmployee,
  onRemoveEmployee,
  onAddBind,
  onDeleteBind,
  onManualColumnLabelChange,
  onAddManualColumn,
  onDeleteManualColumn,
  onPublishManualShift,
  onCancelManualShift,
  onCancelPendingManualShift,
  onEmployeeMinHoursChange,
  onColumnMove,
  onCellChange,
  onSelectedCellKeysChange,
  onPreviewSelectedCellKeysChange,
  onFillColorChange,
  onTextColorChange,
  onApplyFillColor,
  onApplyFillColorShortcut,
  onBindFillColor,
  onDeleteFillColorBind,
  onApplyTextColor,
  onApplyTextColorShortcut,
  onBindTextColor,
  onDeleteTextColorBind,
  onClearCellStyle,
  onClearAllCellStyles,
  canUndo,
  onUndo,
  onVersionCheckoutComplete,
  onSave,
  onGenerate,
}: ContainerGraphEditorProps) {
  const [collapsedSections, setCollapsedSections] = useState<Record<SidebarSectionKey, boolean>>({
    details: true,
    publication: true,
    corrections: true,
    employees: true,
    bind: true,
    manualColumns: true,
    note: true,
  });
  const [pendingCorrectionCount, setPendingCorrectionCount] = useState(0);
  const [swapHighlightColor, setSwapHighlightColor] = useState("#BBF7D0");
  const [isSwapColorDialogOpen, setIsSwapColorDialogOpen] = useState(false);
  const [swapColorError, setSwapColorError] = useState<string | null>(null);
  const [colorDialogMode, setColorDialogMode] = useState<ColorDialogMode | null>(null);
  const [isPresetDialogOpen, setIsPresetDialogOpen] = useState(false);
  const [isVersionsDialogOpen, setIsVersionsDialogOpen] = useState(false);
  const [previewLayoutMode, setPreviewLayoutMode] = useState<PreviewLayoutMode>("stacked");
  const [isGenerationOverlayArmed, setIsGenerationOverlayArmed] = useState(false);
  const [activeRelatedHintCellKey, setActiveRelatedHintCellKey] = useState<string | null>(null);
  const [shiftSwapLogSearch, setShiftSwapLogSearch] = useState("");
  const [selectedShiftSwapHistory, setSelectedShiftSwapHistory] = useState<ShiftSwap | null>(null);
  const [viewportHeight, setViewportHeight] = useState(() => {
    if (typeof window === "undefined") {
      return 0;
    }

    return window.innerHeight;
  });
  const [isDesktopLayout, setIsDesktopLayout] = useState(() => {
    if (typeof window === "undefined") {
      return true;
    }

    return window.matchMedia(DESKTOP_MEDIA_QUERY).matches;
  });
  const graphContainerId = graph?.containerId ?? null;
  const graphId = graph?.id ?? null;
  const swapHighlightSettingQuery = useGraphShiftSwapHighlightSettingQuery(graphContainerId, graphId);
  const saveSwapHighlightSettingMutation = useSaveGraphShiftSwapHighlightSettingMutation();

  const allSectionsCollapsed = Object.values(collapsedSections).every(Boolean);

  useEffect(() => {
    if (swapHighlightSettingQuery.data?.highlightColor) {
      setSwapHighlightColor(swapHighlightSettingQuery.data.highlightColor);
    }
  }, [swapHighlightSettingQuery.data?.highlightColor]);

  const handleSwapHighlightColorChange = (highlightColor: string) => {
    if (graphContainerId === null || graphId === null) return;
    setSwapHighlightColor(highlightColor);
    setIsSwapColorDialogOpen(false);
    setSwapColorError(null);
    saveSwapHighlightSettingMutation.mutate({ containerId: graphContainerId, graphId, highlightColor }, {
      onError: error => setSwapColorError(getErrorMessage(error, t("Could not save the accepted swap highlight color."))),
    });
  };

  useEffect(() => {
    if (typeof window === "undefined") {
      return;
    }

    const mediaQuery = window.matchMedia(DESKTOP_MEDIA_QUERY);
    const handleChange = () => {
      setIsDesktopLayout(mediaQuery.matches);
      setViewportHeight(window.innerHeight);
    };

    handleChange();

    window.addEventListener("resize", handleChange);

    if (typeof mediaQuery.addEventListener === "function") {
      mediaQuery.addEventListener("change", handleChange);
      return () => {
        window.removeEventListener("resize", handleChange);
        mediaQuery.removeEventListener("change", handleChange);
      };
    }

    mediaQuery.addListener(handleChange);
    return () => {
      window.removeEventListener("resize", handleChange);
      mediaQuery.removeListener(handleChange);
    };
  }, []);

  useEffect(() => {
    if (!isGenerating) {
      return;
    }

    const timeoutId = window.setTimeout(() => {
      setIsGenerationOverlayArmed(true);
    }, 550);

    return () => {
      window.clearTimeout(timeoutId);
      setIsGenerationOverlayArmed(false);
    };
  }, [isGenerating]);

  const employeeById = useMemo(
    () => new Map(employees.map(employee => [employee.id, employee])),
    [employees],
  );
  const availableEmployees = useMemo(() => {
    const assignedEmployeeIdSet = new Set(graphEmployeeRows.map(row => row.employeeId));

    return employees
      .filter(employee => !assignedEmployeeIdSet.has(employee.id) || employee.id === selectedEmployeeId)
      .sort((left, right) => getEmployeeFullName(left).localeCompare(getEmployeeFullName(right)));
  }, [employees, graphEmployeeRows, selectedEmployeeId]);
  const hasScrollableEmployeeList = graphEmployeeRows.length > 5;
  const employeeOptions = useMemo<SearchableSelectOption[]>(
    () =>
      availableEmployees.map(employee => ({
        value: String(employee.id),
        label: getEmployeeFullName(employee),
        keywords: [
          String(employee.id),
          employee.firstName,
          employee.lastName,
          employee.email ?? "",
          employee.phone ?? "",
          getEmployeeFullName(employee),
        ].join(" "),
      })),
    [availableEmployees],
  );
  const previewGraphColumns = useMemo<GraphMatrixColumn[]>(
    () =>
      previewColumns.map(column => ({
        employeeId: column.employeeId,
        kind: "employee",
        manualColumnId: null,
        graphEmployeeId: column.memberId ?? null,
        label: column.label,
        minHoursMonth: null,
        totalMinutes: 0,
        totalText: "",
    })),
    [previewColumns],
  );
  const scheduleColumnKindByEmployeeId = useMemo(
    () => new Map(scheduleColumns.map(column => [column.employeeId, column.kind])),
    [scheduleColumns],
  );
  const acceptedShiftSwapLog = useMemo(
    () => shiftSwapLog.filter(item => item.status === "accepted"),
    [shiftSwapLog],
  );
  const filteredAcceptedShiftSwapLog = useMemo(
    () => filterAcceptedShiftSwapHistory(acceptedShiftSwapLog, shiftSwapLogSearch),
    [acceptedShiftSwapLog, shiftSwapLogSearch],
  );
  const openManagerManualShifts = useMemo(
    () => shiftSwapLog.filter(item => item.status === "open" && item.isManagerCreated),
    [shiftSwapLog],
  );

  if (isLoading) {
    return <div className={styles.state}>{t("Loading schedule editor...")}</div>;
  }

  if (hasLoadError) {
    return <ErrorBanner className={styles.banner}>{t("Could not load this schedule editor.")}</ErrorBanner>;
  }

  const scheduleMatrixBaseMinHeight = Math.max(
    isHeaderCollapsed ? 680 : 660,
    viewportHeight - (isHeaderCollapsed ? 188 : 228),
  );
  const previewMatrixBaseMinHeight =
    isDesktopLayout
      ? Math.max(660, viewportHeight - 228)
      : null;
  const scheduleMatrixCardHeight = Math.min(
    isHeaderCollapsed ? 1000 : 980,
    Math.round(scheduleMatrixBaseMinHeight * 1.15),
  );
  const previewMatrixCardHeight =
    previewMatrixBaseMinHeight !== null
      ? Math.min(980, Math.round(previewMatrixBaseMinHeight * 1.15))
      : null;
  const scheduleMatrixCardShellStyle =
    !compactSize
      ? {
        minHeight: `${scheduleMatrixCardHeight}px`,
        height: `${scheduleMatrixCardHeight}px`,
        maxHeight: `${scheduleMatrixCardHeight}px`,
      }
      : undefined;
  const previewMatrixCardShellStyle =
    isDesktopLayout && previewMatrixCardHeight !== null && !compactSize
      ? {
        minHeight: `${previewMatrixCardHeight}px`,
        height: `${previewMatrixCardHeight}px`,
        maxHeight: `${previewMatrixCardHeight}px`,
      }
      : undefined;
  const scheduleMatrixCardStyle =
    !compactSize
      ? { height: "100%", maxHeight: "100%" }
      : undefined;
  const previewMatrixCardStyle =
    isDesktopLayout && !compactSize
      ? { height: "100%", maxHeight: "100%" }
      : undefined;
  const hasSelection = selectedCellKeys.length > 0;
  const canApplySelectedStyles = hasSelection && !isStylingBusy;
  const hasStyledCells = Object.keys(styleMap).length > 0;
  const activeColorValue = colorDialogMode === "text" ? textColor : fillColor;
  const isSplitPreviewLayout = isDesktopLayout && previewLayoutMode === "side";
  const showGenerationOverlay = isGenerating && isGenerationOverlayArmed;
  const displayGraphYear = Number(form.year) || graph?.year || new Date().getFullYear();
  const displayGraphMonth = Number(form.month) || graph?.month || 1;
  const displayGraphName = form.name.trim() || graph?.name?.trim() || t("Current schedule");
  const activeRelatedHint =
    activeRelatedHintCellKey
      ? visualHintDetailMap[activeRelatedHintCellKey] ?? null
      : null;
  const activeHintEmployeeName =
    activeRelatedHint
      ? getEmployeeFullName(employeeById.get(activeRelatedHint.employeeId), t("Employee {0}", activeRelatedHint.employeeId))
      : "";
  const activeHintDayLabel =
    activeRelatedHint
      ? formatGraphHintDateLabel(displayGraphYear, displayGraphMonth, activeRelatedHint.dayOfMonth)
      : "";
  const setSectionCollapsed = (section: SidebarSectionKey, collapsed: boolean) => {
    setCollapsedSections(current => (
      current[section] === collapsed ? current : { ...current, [section]: collapsed }
    ));
  };

  const handleColorDialogSave = (nextColor: string) => {
    if (colorDialogMode === "text") {
      onTextColorChange(nextColor);
    } else {
      onFillColorChange(nextColor);
    }

    setColorDialogMode(null);
  };
  const handleGenerate = () => {
    setIsGenerationOverlayArmed(false);
    onGenerate();
  };

  const renderCollapseButton = (label: string, section: SidebarSectionKey) => (
    <AvailabilitySidebarCollapseButton
      label={label}
      onCollapse={() => setSectionCollapsed(section, true)}
    />
  );

  return (
    <>
      <AvailabilityWorkspaceLayout
        preserveSideLayoutOnMobile
        className={joinClassNames(styles.workspaceLayout, allSectionsCollapsed && styles.layoutCollapsed)}
        sidebarColumnClassName={joinClassNames(styles.sidebarColumn, allSectionsCollapsed && styles.sidebarColumnCollapsed)}
        sidebarContentClassName={joinClassNames(styles.sidebar, allSectionsCollapsed && styles.sidebarCollapsed)}
        mainColumnClassName={joinClassNames(styles.mainColumn, allSectionsCollapsed && styles.mainColumnCollapsed)}
        mainBlockClassName={styles.mainBlock}
        sidebar={
          <>
            <AvailabilitySidebarSection
              label={t("Schedule Details")}
              preserveCollapsedOnMobile
              collapsed={collapsedSections.details}
              collapsedIcon={<ScheduleDetailsIcon size={18} />}
              collapsedOffset={allSectionsCollapsed ? "flush" : "default"}
              onExpand={() => setSectionCollapsed("details", false)}
            >
              <CardSection
                className={styles.sidebarCard}
                title={t("Schedule Details")}
                icon={<ScheduleDetailsIcon size={18} />}
                headerRightSlot={renderCollapseButton(t("Schedule Details"), "details")}
              >
                <ContainerGraphDetailsFields
                  form={form}
                  formErrors={formErrors}
                  shops={shops}
                  availabilityGroups={availabilityGroups}
                  employees={employees}
                  graphEmployeeRows={graphEmployeeRows}
                  topSlot={(
                    <ContainerGraphPresetSelect
                      presets={schedulePresets}
                      selectedPresetId={selectedSchedulePresetId}
                      shops={shops}
                      isLoading={isSchedulePresetsLoading}
                      onSelect={onApplySchedulePreset}
                      onAddPreset={() => setIsPresetDialogOpen(true)}
                    />
                  )}
                  onFieldChange={onFieldChange}
                  onEmployeeMinHoursChange={onEmployeeMinHoursChange}
                />

                <div className={styles.detailsActions}>
                  <IosButton
                    className={styles.generateButton}
                    label={isGenerating ? t("Generating...") : t("Generate")}
                    disabled={isGenerating || isSaving}
                    onClick={handleGenerate}
                  />
                </div>
              </CardSection>
            </AvailabilitySidebarSection>

            <AvailabilitySidebarSection
              label={t("Publication")}
              preserveCollapsedOnMobile
              collapsed={collapsedSections.publication}
              collapsedIcon={<EyeIcon size={18} />}
              collapsedOffset={allSectionsCollapsed ? "flush" : "default"}
              onExpand={() => setSectionCollapsed("publication", false)}
            >
              <CardSection
                className={styles.sidebarCard}
                title={t("Publication")}
                icon={<EyeIcon size={18} />}
                headerRightSlot={renderCollapseButton(t("Publication"), "publication")}
              >
                <div className={styles.publicationHeaderTools}>
                  <button type="button" className={styles.publicationColorControl}
                    title={t("Accepted swap highlight color")} aria-label={t("Accepted swap highlight color")}
                    disabled={graphId === null || saveSwapHighlightSettingMutation.isPending}
                    onClick={() => setIsSwapColorDialogOpen(true)}>
                    <span className={styles.publicationColorSwatch}
                      style={{ backgroundColor: swapHighlightColor }} aria-hidden="true" />
                  </button>
                </div>

                <ContainerGraphHighlightColorDialog open={isSwapColorDialogOpen} value={swapHighlightColor}
                  eyebrow={t("Publication")} title={t("Choose accepted swap color")} inputLabel={t("Accepted swap highlight hex color")}
                  isSaving={saveSwapHighlightSettingMutation.isPending}
                  onCancel={() => setIsSwapColorDialogOpen(false)} onSave={handleSwapHighlightColorChange} />

                <div className={styles.publicationCard}>
                  {swapColorError ? <ErrorBanner dismissible={false}>{swapColorError}</ErrorBanner> : null}
                  <div className={styles.publicationControl} role="radiogroup" aria-label={t("Schedule publication status")}>
                    <button
                      type="button"
                      role="radio"
                      aria-checked={form.publicationStatus === "private"}
                      className={joinClassNames(
                        styles.publicationSegment,
                        form.publicationStatus === "private" && styles.publicationSegmentActive,
                      )}
                      onClick={() => onFieldChange("publicationStatus")("private")}
                    >
                      {t("Private")}</button>

                    <button
                      type="button"
                      role="radio"
                      aria-checked={form.publicationStatus === "public"}
                      className={joinClassNames(
                        styles.publicationSegment,
                        form.publicationStatus === "public" && styles.publicationSegmentActive,
                      )}
                      onClick={() => onFieldChange("publicationStatus")("public")}
                    >
                      {t("Public")}</button>
                  </div>

                  <div className={styles.swapPermission}>
                    <div className={styles.swapPermissionCopy}>
                      <strong>{t("Allow swap")}</strong>
                      <span>{t("Controls employee offers and manager Manual-column swaps.")}</span>
                    </div>
                    <div className={styles.swapPermissionControl} role="radiogroup" aria-label={t("Allow schedule swaps")}>
                      <button type="button" role="radio" aria-checked={!form.allowSwap} className={joinClassNames(styles.publicationSegment, !form.allowSwap && styles.publicationSegmentActive)} onClick={() => onFieldChange("allowSwap")(false)}>{t("Off")}</button>
                      <button type="button" role="radio" aria-checked={form.allowSwap} className={joinClassNames(styles.publicationSegment, form.allowSwap && styles.publicationSegmentActive)} onClick={() => onFieldChange("allowSwap")(true)}>{t("On")}</button>
                    </div>
                  </div>

                  <div className={styles.publicationLog}>
                    <div className={styles.publicationLogHeader}>
                      <span>{t("Swap log")}</span>
                      <strong>
                        {shiftSwapLogSearch.trim()
                          ? `${filteredAcceptedShiftSwapLog.length}/${acceptedShiftSwapLog.length}`
                          : acceptedShiftSwapLog.length}
                      </strong>
                    </div>

                    <label className={styles.publicationLogSearch}>
                      <SearchIcon size={16} />
                      <input
                        type="search"
                        value={shiftSwapLogSearch}
                        placeholder={t("Search employee, date or schedule...")}
                        aria-label={t("Search accepted swaps by giver, receiver, date or schedule")}
                        onChange={event => setShiftSwapLogSearch(event.target.value)}
                      />
                    </label>

                    {isShiftSwapLogLoading ? (
                      <p className={styles.publicationLogState}>{t("Loading swap log...")}</p>
                    ) : acceptedShiftSwapLog.length === 0 ? (
                      <p className={styles.publicationLogState}>{t("No accepted swaps yet.")}</p>
                    ) : filteredAcceptedShiftSwapLog.length === 0 ? (
                      <p className={styles.publicationLogState}>{t("No swaps match this search.")}</p>
                    ) : (
                      <div className={styles.publicationLogList}>
                        {filteredAcceptedShiftSwapLog.map(item => (
                          <article key={item.id} className={styles.publicationLogItem}>
                            <div className={styles.publicationLogItemHeader}>
                              <div className={styles.publicationLogItemMain}>
                                <strong>{item.fromEmployeeName}</strong>
                                <span>{t("to")}</span>
                                <strong>{item.acceptedByEmployeeName ?? t("Employee")}</strong>
                              </div>
                              <button
                                type="button"
                                className={styles.publicationLogViewButton}
                                aria-label={t("View swap comparison for {0}", item.fromEmployeeName)}
                                title={t("View before and after")}
                                onClick={() => setSelectedShiftSwapHistory(item)}
                              >
                                <EyeIcon size={17} />
                              </button>
                            </div>
                            <div className={styles.publicationLogDetails}>
                              <div className={styles.publicationLogDetail}>
                                <span>{t("Shift:")}</span>
                                <strong>{formatShiftSwapLogShift(item)}</strong>
                              </div>
                              <div className={styles.publicationLogDetail}>
                                <span>{t("When was accepted:")}</span>
                                <strong>{formatShiftSwapAcceptedAt(item.acceptedAtUtc)}</strong>
                              </div>
                            </div>
                          </article>
                        ))}
                      </div>
                    )}
                  </div>
                </div>
              </CardSection>
            </AvailabilitySidebarSection>

            <AvailabilitySidebarSection
              label={t("Shift Corrections")}
              preserveCollapsedOnMobile
              collapsed={collapsedSections.corrections}
              collapsedIcon={<ScheduleIcon size={18} />}
              collapsedOffset={allSectionsCollapsed ? "flush" : "default"}
              attention={pendingCorrectionCount > 0}
              onExpand={() => setSectionCollapsed("corrections", false)}
            >
              <CardSection
                className={styles.sidebarCard}
                title={t("Shift Corrections")}
                icon={<ScheduleIcon size={18} />}
                headerRightSlot={renderCollapseButton(t("Shift Corrections"), "corrections")}
              >
                <ContainerGraphShiftCorrectionsCard
                  containerId={graph?.containerId ?? null}
                  graphId={graph?.id ?? null}
                  hasUnsavedChanges={Boolean(hasUnsavedChanges)}
                  disabled={isSaving || isGenerating}
                  onApproved={onVersionCheckoutComplete}
                  onPendingCountChange={setPendingCorrectionCount}
                />
              </CardSection>
            </AvailabilitySidebarSection>

          <AvailabilitySidebarSection
            label={t("Employees")}
            preserveCollapsedOnMobile
            collapsed={collapsedSections.employees}
            collapsedIcon={<EmployeeIcon size={18} />}
            collapsedOffset={allSectionsCollapsed ? "flush" : "default"}
            onExpand={() => setSectionCollapsed("employees", false)}
          >
            <CardSection
              className={styles.sidebarCard}
              title={t("Employees")}
              icon={<EmployeeIcon size={18} />}
              headerRightSlot={renderCollapseButton(t("Employees"), "employees")}
            >
              <div className={styles.employeeSection}>
                <div className={styles.employeeControls}>
                  <div className={styles.employeeActionRow}>
                    <SearchableSelect
                      id="graph-employee-select"
                      className={styles.controlFull}
                      size="compact"
                      value={selectedEmployeeId !== null ? String(selectedEmployeeId) : ""}
                      options={employeeOptions}
                      placeholder={availableEmployees.length > 0 ? t("Select employee...") : t("No employees available")}
                      dropdownTitle={t("Employees")}
                      searchPlaceholder={t("Search employee...")}
                      emptyMessage={t("No employees match your search.")}
                      fallbackHint=""
                      ariaLabel={t("employee list")}
                      onChange={value => onSelectedEmployeeIdChange(value ? Number(value) : null)}
                    />

                    <IosButton
                      label={t("Add")}
                      icon={<PlusIcon size={16} />}
                      disabled={selectedEmployeeId === null}
                      onClick={onAddEmployee}
                    />
                  </div>
                </div>

                {graphEmployeeRows.length === 0 ? (
                  <div className={joinClassNames(styles.detailsMinHoursBlock, styles.employeeRosterBlock)}>
                    <div className={styles.detailsMinHoursHeader}>
                      <span className={styles.detailsMinHoursTitle}>{t("Employees in schedule")}</span>
                      <span className={styles.detailsMinHoursMeta}>{t("{0} assigned", graphEmployeeRows.length)}</span>
                    </div>
                    <div className={joinClassNames(styles.detailsMinHoursEmpty, styles.employeeEmpty)}>
                      {t("No employees added yet.")}</div>
                  </div>
                ) : (
                  <div className={joinClassNames(styles.detailsMinHoursBlock, styles.employeeRosterBlock)}>
                    <div className={styles.detailsMinHoursHeader}>
                      <span className={styles.detailsMinHoursTitle}>{t("Employees in schedule")}</span>
                      <span className={styles.detailsMinHoursMeta}>{t("{0} assigned", graphEmployeeRows.length)}</span>
                    </div>
                    <div
                      className={joinClassNames(
                        styles.employeeList,
                        hasScrollableEmployeeList && styles.employeeListScrollable,
                      )}
                    >
                      {graphEmployeeRows.map(row => {
                        const employee = employeeById.get(row.employeeId);
                        const employeeLabel = getEmployeeFullName(employee, t("Employee {0}", row.employeeId));

                        return (
                          <div key={row.employeeId} className={styles.employeeRow}>
                            <span className={styles.employeeName}>{employeeLabel}</span>

                            <div className={styles.employeeRowActions}>
                              <button
                                type="button"
                                className={styles.removeEmployeeButton}
                                aria-label={t("Remove {0} from schedule", employeeLabel)}
                                title={t("Remove employee")}
                                onClick={() => onRemoveEmployee(row.employeeId)}
                              >
                                <CloseIcon size={15} />
                              </button>
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  </div>
                )}
              </div>
            </CardSection>
          </AvailabilitySidebarSection>

          <AvailabilitySidebarSection
            label={t("Bind Information")}
            preserveCollapsedOnMobile
            collapsed={collapsedSections.bind}
            collapsedIcon={<BindIcon size={18} />}
            collapsedOffset={allSectionsCollapsed ? "flush" : "default"}
            onExpand={() => setSectionCollapsed("bind", false)}
          >
            <AvailabilityBindCard
              binds={binds}
              selectedBindClientId={selectedBindClientId}
              isLoading={isBindsLoading}
              isBusy={isBindBusy}
              errorMessage={bindErrorMessage}
              headerRightSlot={renderCollapseButton(t("Bind Information"), "bind")}
              onSelectedBindChange={onSelectedBindChange}
              onBindFieldChange={onBindFieldChange}
              onBindCommit={onBindCommit}
              onAddBind={onAddBind}
              onDeleteBind={onDeleteBind}
            />
          </AvailabilitySidebarSection>

          <AvailabilitySidebarSection
            label={t("Manual Columns")}
            preserveCollapsedOnMobile
            collapsed={collapsedSections.manualColumns}
            collapsedIcon={<InformationIcon size={18} />}
            collapsedOffset={allSectionsCollapsed ? "flush" : "default"}
            onExpand={() => setSectionCollapsed("manualColumns", false)}
          >
            <ContainerGraphManualColumnsCard
              columns={manualColumns}
              allowSwap={form.allowSwap}
              year={displayGraphYear}
              month={displayGraphMonth}
              employees={employees}
              isPublishingShift={isPublishingManualShift}
              isCancellingPublishedShift={isCancellingManualShift}
              publishError={manualShiftPublishError}
              publishedShifts={openManagerManualShifts}
              pendingPublishedShifts={pendingManualShiftPublishes}
              headerRightSlot={renderCollapseButton(t("Manual Columns"), "manualColumns")}
              onAddColumn={onAddManualColumn}
              onDeleteColumn={onDeleteManualColumn}
              onPublishShift={onPublishManualShift}
              onCancelPublishedShift={onCancelManualShift}
              onCancelPendingPublishedShift={onCancelPendingManualShift}
            />
          </AvailabilitySidebarSection>

          <AvailabilitySidebarSection
            label={t("Note")}
            preserveCollapsedOnMobile
            collapsed={collapsedSections.note}
            collapsedIcon={<NoteIcon size={18} />}
            collapsedOffset={allSectionsCollapsed ? "flush" : "default"}
            onExpand={() => setSectionCollapsed("note", false)}
          >
            <CardSection
              className={styles.sidebarCard}
              title={t("Note")}
              icon={<NoteIcon size={18} />}
              headerRightSlot={renderCollapseButton(t("Note"), "note")}
            >
              <LabeledField id="graph-note" label={t("Schedule note")} error={formErrors.note} className={styles.noteField}>
                <TextArea
                  id="graph-note"
                  rows={12}
                  className={styles.noteInput}
                  placeholder={t("Add scheduling notes, constraints, or handoff details...")}
                  value={form.note}
                  onChange={event => onFieldChange("note")(event.target.value)}
                />
              </LabeledField>
            </CardSection>
            </AvailabilitySidebarSection>
          </>
        }
        main={
          <div className={styles.mainStack}>
            {submitError ? <ErrorBanner key={validationSignal}>{submitError}</ErrorBanner> : null}

            <div className={joinClassNames(styles.matrixWorkspace, isSplitPreviewLayout && styles.matrixWorkspaceSplit)}>
              <div
                className={joinClassNames(
                  styles.matrixCardShell,
                  styles.scheduleMatrixCardShell,
                  compactSize && styles.matrixCardShellCompact,
                )}
                style={scheduleMatrixCardShellStyle}
              >
                <ContainerGraphMatrix
                  className={joinClassNames(
                    styles.matrixCard,
                    styles.trimmedMatrixCard,
                    compactSize && styles.matrixCardCompact,
                  )}
                  style={scheduleMatrixCardStyle}
                  compactSize={compactSize}
                  helperText=""
                  graph={{
                    year: Number(form.year) || graph?.year || new Date().getFullYear(),
                    month: Number(form.month) || graph?.month || 1,
                    shift1Time: form.shift1Time,
                    shift2Time: form.shift2Time,
                  }}
                  showShiftStaffingCounts
                  columns={scheduleColumns}
                  cellMap={cellMap}
                  visualHintMap={visualHintMap}
                  visualHintDetailMap={visualHintDetailMap}
                  cellErrors={cellErrors}
                  validationSignal={validationSignal}
                  styleMap={styleMap}
                  dayConflictMap={dayConflictMap}
                  onColumnMove={onColumnMove}
                  onColumnLabelChange={onManualColumnLabelChange}
                  selectedCellKeys={selectedCellKeys}
                  bindValueByKey={bindValueByKey}
                  fillColorByKey={fillColorByKey}
                  textColorByKey={textColorByKey}
                  normalizeCellValue={(employeeId, value) => (
                    scheduleColumnKindByEmployeeId.get(employeeId) === "employee"
                      ? normalizeGraphCellValue(value)
                      : value
                  )}
                  onSelectedCellKeysChange={onSelectedCellKeysChange}
                  onCellChange={onCellChange}
                  onFillColorShortcut={onApplyFillColorShortcut}
                  onTextColorShortcut={onApplyTextColorShortcut}
                  onVisualHintClick={detail => setActiveRelatedHintCellKey(`${detail.employeeId}:${detail.dayOfMonth}`)}
                  toolbar={
                    <div className={styles.matrixToolbar}>
                      <ToolbarActionButton
                        label={t("Undo")}
                        icon={<BackIcon size={15} />}
                        disabled={!canUndo || isSaving || isGenerating || isStylingBusy}
                        onClick={onUndo}
                      />
                      <SplitColorButton
                        label={isStylingBusy ? t("Applying Fill...") : t("Fill")}
                        color={fillColor}
                        disabled={!canApplySelectedStyles}
                        pickerDisabled={isStylingBusy}
                        onApply={onApplyFillColor}
                        onOpenDialog={() => setColorDialogMode("fill")}
                      />
                      <SplitColorButton
                        label={isStylingBusy ? t("Applying Text...") : t("Text")}
                        color={textColor}
                        disabled={!canApplySelectedStyles}
                        pickerDisabled={isStylingBusy}
                        onApply={onApplyTextColor}
                        onOpenDialog={() => setColorDialogMode("text")}
                      />
                      <ToolbarActionButton
                        label={t("C. Selected")}
                        icon={<ClearFormatIcon size={15} />}
                        disabled={!hasSelection || isStylingBusy}
                        onClick={onClearCellStyle}
                      />
                      <ToolbarActionButton
                        label={t("C. All")}
                        icon={<ClearFormatAllIcon size={15} />}
                        disabled={!hasStyledCells || isStylingBusy}
                        onClick={onClearAllCellStyles}
                      />
                      <ToolbarActionButton
                        label={t("Versions")}
                        icon={<SwapHistoryIcon size={15} />}
                        disabled={!graph || isSaving || isGenerating || isStylingBusy}
                        onClick={() => setIsVersionsDialogOpen(true)}
                      />

                    </div>
                  }
                  headerRightSlot={
                    <div className={styles.matrixHeaderActions}>
                      <span className={styles.headerBadge}>{t("Employees: {0}", totals.totalEmployees)}</span>
                      <span className={styles.headerBadge}>{t("Hours: {0}", totals.totalHoursText)}</span>
                      {showMatrixSaveAction ? (
                        <IosButton
                          label={isSaving ? t("Saving...") : t("Save")}
                          icon={<SaveIcon size={18} />}
                          disabled={isSaving || isGenerating}
                          onClick={onSave}
                        />
                      ) : null}
                    </div>
                  }
                />
              </div>

              <div
                className={joinClassNames(styles.matrixCardShell, compactSize && styles.matrixCardShellCompact)}
                style={previewMatrixCardShellStyle}
              >
                <ContainerGraphMatrix
                  className={joinClassNames(
                    styles.matrixCard,
                    styles.trimmedMatrixCard,
                    compactSize && styles.matrixCardCompact,
                  )}
                  style={previewMatrixCardStyle}
                  compactSize={compactSize}
                  title={t("Availability Preview")}
                  helperText=""
                  graph={{
                    year: previewYear,
                    month: previewMonth,
                  }}
                  columns={previewGraphColumns}
                  cellMap={previewCellMap}
                  readOnly
                  enableSelectionWhenReadOnly
                  highlightReadOnlyEmpty
                  emptyMessage={t("No availability preview is available for the selected group yet.")}
                  selectedCellKeys={previewSelectedCellKeys}
                  onSelectedCellKeysChange={onPreviewSelectedCellKeysChange}
                  headerRightSlot={
                    <div className={styles.previewHeaderActions}>
                      <SearchableSelect
                        id="graph-preview-availability"
                        className={styles.previewAvailabilitySelect}
                        size="compact"
                        value={previewAvailabilitySelection}
                        options={previewAvailabilityOptions}
                        placeholder={t("Select preview availability...")}
                        dropdownTitle={t("Availability Preview")}
                        searchPlaceholder={t("Search availability...")}
                        emptyMessage={t("No availability groups match your search.")}
                        showSelectedHint={false}
                        ariaLabel={t("availability preview groups")}
                        onChange={onPreviewAvailabilitySelectionChange}
                      />

                      <div className={styles.previewLayoutActions}>
                        <PreviewLayoutButton
                          mode="side"
                          active={previewLayoutMode === "side"}
                          onClick={() => setPreviewLayoutMode("side")}
                        />
                        <PreviewLayoutButton
                          mode="stacked"
                          active={previewLayoutMode === "stacked"}
                          onClick={() => setPreviewLayoutMode("stacked")}
                        />
                      </div>
                    </div>
                  }
                />
              </div>
            </div>
          </div>
        }
      />

      <ContainerGraphColorDialog
        open={colorDialogMode !== null}
        mode={colorDialogMode}
        value={activeColorValue}
        fillColorBinds={fillColorBinds}
        textColorBinds={textColorBinds}
        reservedValueBindKeys={valueBindKeys}
        isFillColorBindBusy={isFillColorBindBusy}
        isTextColorBindBusy={isTextColorBindBusy}
        onCancel={() => setColorDialogMode(null)}
        onSave={handleColorDialogSave}
        onBindFillColor={onBindFillColor}
        onDeleteFillColorBind={onDeleteFillColorBind}
        onBindTextColor={onBindTextColor}
        onDeleteTextColorBind={onDeleteTextColorBind}
      />
      <ContainerGraphVersionsDialog
        open={isVersionsDialogOpen}
        containerId={graph?.containerId ?? null}
        graphId={graph?.id ?? null}
        hasUnsavedChanges={hasUnsavedChanges}
        onCancel={() => setIsVersionsDialogOpen(false)}
        onCheckoutComplete={onVersionCheckoutComplete}
      />
      <ContainerGraphPresetDialog
        open={isPresetDialogOpen}
        initialForm={form}
        shops={shops}
        onCancel={() => setIsPresetDialogOpen(false)}
        onSave={onCreateSchedulePreset}
      />
      <ContainerGraphRelatedHintDialog
        open={activeRelatedHint !== null}
        graphName={displayGraphName}
        employeeName={activeHintEmployeeName}
        year={displayGraphYear}
        month={displayGraphMonth}
        dayLabel={activeHintDayLabel}
        currentCellMap={cellMap}
        detail={activeRelatedHint}
        onCancel={() => setActiveRelatedHintCellKey(null)}
      />
      <ShiftSwapHistoryDialog
        open={selectedShiftSwapHistory !== null}
        swap={selectedShiftSwapHistory}
        onCancel={() => setSelectedShiftSwapHistory(null)}
      />
      {showGenerationOverlay ? (
        <div className={styles.generationOverlay} role="status" aria-live="polite" aria-label={t("Generating schedule")}>
          <div className={styles.generationOverlayCard}>
            <div className={styles.generationOverlaySpinner} aria-hidden="true">
              <span />
              <span />
              <span />
            </div>
            <div className={styles.generationOverlayEyebrow}>{t("Schedule Generator")}</div>
            <div className={styles.generationOverlayTitle}>{t("Generating schedule...")}</div>
            <div className={styles.generationOverlayText}>
              {t("We are filling the grid using availability and workload limits. The matrix will refresh automatically when it is ready.")}</div>
          </div>
        </div>
      ) : null}
    </>
  );
}
