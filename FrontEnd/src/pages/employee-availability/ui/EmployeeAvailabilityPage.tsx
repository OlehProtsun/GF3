import { dateTimeFormat, t } from "@shared/i18n";
import { useEffect, useMemo, useState, useRef, useCallback, type MouseEvent } from "react";
import { useAuth } from "@app/providers/AuthProvider";
import {
  useEmployeeAvailabilityListQuery,
  useSaveEmployeeAvailabilityMutation,
  type EmployeeAvailabilityGroup,
} from "@entities/employee-availability";
import { createPortal } from "react-dom";
import { useEmployeeAvailabilityTimePresetsQuery, useSaveEmployeeAvailabilityTimePresetsMutation } from "@entities/employee-ui-state";
import { EmployeeAvailabilityMatrix } from "./EmployeeAvailabilityMatrix";
import { EmployeeUrlopPanel } from "./EmployeeUrlopPanel";
import { useEmployeeUrlopCalendarQuery } from "@entities/urlop";
import { estimateAvailabilityHours } from "@entities/employee-availability/model/estimatedHours";
import { applyEmployeeAvailabilityValue, buildAvailabilityDraftSource, buildDraftFromAvailability, buildDraftSnapshot, buildSaveSlots, type DayDraft, type DayDraftMap } from "../model/employeeAvailabilityMatrix";
import {
  AVAILABILITY_ANY_MARK,
  AVAILABILITY_NONE_MARK,
  getDaysInMonth,
  parseAvailabilityCode,
} from "@entities/availability-groups/model/editor";
import {
  getAvailabilityGroupPeriodLabel,
  getAvailabilityMonthLabel,
} from "@entities/availability-groups/model/presentation";
import { getErrorMessage } from "@shared/api/httpClient";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import { AccountSettingsIcon, AvailabilityIcon, CheckIcon, ChevronRightIcon, CloseIcon, EyeIcon, SaveIcon } from "@shared/ui/icons";
import { parseFlexibleTimeRange } from "@shared/lib/timeRange";
import { useSyncedDraft } from "@shared/lib/useSyncedDraft";
import { useUnsavedChangesPrompt } from "@shared/lib/useUnsavedChangesPrompt";
import workspaceStyles from "@pages/shared/EmployeeWorkspacePage.module.css";
import styles from "./EmployeeAvailabilityPage.module.css";

const defaultTimePresets = ["09:00 - 15:00", "15:00 - 21:00", "09:00 - 21:00"];

function readTimePresets(storageKey: string | null): string[] | null {
  if (!storageKey) return null;
  try {
    const stored: unknown = JSON.parse(window.localStorage.getItem(storageKey) ?? "null");
    if (!Array.isArray(stored) || stored.length < 3 || stored.length > 100) return null;
    const ranges = stored.map(value => typeof value === "string" ? parseFlexibleTimeRange(value) : null);
    return ranges.every(range => range !== null) ? ranges.map(range => range!.label) : null;
  } catch { return null; }
}

function AvailabilityClockIcon() {
  return <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true" focusable="false"><circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 2" /></svg>;
}

function AvailabilityTimePresetEditor({ value, onSave, onCancel }: {
  value: string;
  onSave: (value: string) => Promise<void>;
  onCancel: () => void;
}) {
  const initial = parseFlexibleTimeRange(value);
  const [from, setFrom] = useState(initial?.from ?? "09:00");
  const [to, setTo] = useState(initial?.to ?? "15:00");
  const [isSaving, setIsSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);
  const range = parseFlexibleTimeRange(`${from} - ${to}`);
  return <form className={styles.presetEditor} aria-label={t("Edit preset")} onSubmit={async event => {
    event.preventDefault();
    if (!range || isSaving) return;
    setIsSaving(true); setSaveError(null);
    try { await onSave(range.label); }
    catch { setSaveError(t("Could not save this preset.")); }
    finally { setIsSaving(false); }
  }}>
    <h4>{t("Edit preset")}</h4>
    <div className={styles.timeFields}>
      <label><span>{t("From")}</span><input autoFocus type="time" value={from} aria-invalid={!range} disabled={isSaving} onChange={event => { setFrom(event.target.value); setSaveError(null); }} /></label>
      <span className={styles.timeSeparator} aria-hidden="true">–</span>
      <label><span>{t("To")}</span><input type="time" value={to} aria-invalid={!range} disabled={isSaving} onChange={event => { setTo(event.target.value); setSaveError(null); }} /></label>
    </div>
    {!range ? <p className={styles.dialogError}>{t("Use a valid time range like 09:00 - 15:00.")}</p> : null}
    {saveError ? <p className={styles.dialogError} role="alert">{saveError}</p> : null}
    <div className={styles.presetEditorActions}>
      <button type="button" className={styles.textButton} disabled={isSaving} onClick={onCancel}>{t("Cancel preset edit")}</button>
      <button type="submit" className={styles.presetSaveButton} disabled={!range || isSaving}>{isSaving ? t("Saving...") : t("Save preset")}</button>
    </div>
  </form>;
}

const employeeDateTimeFormatter = dateTimeFormat("en-GB", {
  year: "numeric",
  month: "2-digit",
  day: "2-digit",
  hour: "2-digit",
  minute: "2-digit",
  hourCycle: "h23",
});

function joinClassNames(...values: Array<string | false | undefined>) {
  return values.filter(Boolean).join(" ");
}

function formatEmployeeDateTimeLabel(value?: string | null) {
  if (!value) {
    return t("Not set");
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return t("Not set");
  }

  const parts = Object.fromEntries(employeeDateTimeFormatter.formatToParts(date).map(part => [part.type, part.value]));
  return t("{0} at {1}", `${parts.day}.${parts.month}.${parts.year}`, `${parts.hour}:${parts.minute}`);
}

function getEmployeeVisibilityWindowLabel(group: Pick<EmployeeAvailabilityGroup, "visibleFromUtc" | "visibleToUtc">) {
  const from = formatEmployeeDateTimeLabel(group.visibleFromUtc);
  const to = formatEmployeeDateTimeLabel(group.visibleToUtc);

  if (from === t("Not set") && to === t("Not set")) {
    return t("Not configured");
  }

  return `${from} - ${to}`;
}

function getAvailabilityEditLockedMessage(group: Pick<EmployeeAvailabilityGroup, "editLockedBy">) {
  const lockedBy = group.editLockedBy?.trim() || t("a manager");
  return t("This availability is currently being edited by {0}. You cannot edit it right now.", lockedBy);
}

function isAvailabilityOpen(group?: EmployeeAvailabilityGroup | null) {
  return Boolean(group?.canSubmit) && !group?.isAutomatic && group?.isEditLocked !== true;
}

function getAvailabilityStatusLabel(group: EmployeeAvailabilityGroup) {
  if (group.isAutomatic) return t("Automatic");
  if (group.isEditLocked) {
    return t("Locked");
  }

  return group.canSubmit ? t("Open") : t("Closed");
}

type AvailabilityChoice = "any" | "none" | "custom";

function AvailabilitySelectionSheet({ open, editingDays, dateLabel, day, storageKey, employeeId, onClose, onApply }: {
  open: boolean;
  editingDays: number[];
  dateLabel: string;
  day?: DayDraft;
  storageKey: string | null;
  employeeId: number;
  onClose: () => void;
  onApply: (days: number[], value: string) => void;
}) {
  const sheetRef = useRef<HTMLDivElement>(null);
  const [choice, setChoice] = useState<AvailabilityChoice | null>(null);
  const [fromTime, setFromTime] = useState("09:00");
  const [toTime, setToTime] = useState("15:00");
  const [customValue, setCustomValue] = useState("");
  const [textMode, setTextMode] = useState(false);
  const [timePresets, setTimePresets] = useState(() => readTimePresets(storageKey) ?? [...defaultTimePresets]);
  const [managingPresets, setManagingPresets] = useState(false);
  const [editingPresetIndex, setEditingPresetIndex] = useState<number | null>(null);
  const [presetError, setPresetError] = useState<string | null>(null);
  const imported = useRef(false);
  const presetsQuery = useEmployeeAvailabilityTimePresetsQuery(employeeId, open);
  const presetsMutation = useSaveEmployeeAvailabilityTimePresetsMutation(employeeId);

  useEffect(() => {
    if (!open) return;
    const range = day?.intervalStr ? parseFlexibleTimeRange(day.intervalStr) : null;
    setChoice(!day ? null : !day.available ? "none" : day.intervalStr ? "custom" : "any");
    setFromTime(range?.from ?? "09:00");
    setToTime(range?.to ?? "15:00");
    setCustomValue(day?.intervalStr ?? "09:00 - 15:00");
    setTextMode(Boolean(day?.intervalStr && !range));
    setManagingPresets(false); setEditingPresetIndex(null); setPresetError(null);
  }, [day, open]);

  useEffect(() => {
    if (!open || !presetsQuery.data || presetsQuery.error) return;
    if (presetsQuery.data.presets !== null) {
      const ranges = Array.isArray(presetsQuery.data.presets) ? presetsQuery.data.presets.map(value => parseFlexibleTimeRange(value)) : [];
      const next = ranges.length >= 3 && ranges.every(Boolean) ? ranges.map(range => range!.label) : [...defaultTimePresets];
      setTimePresets(current => current.length === next.length && current.every((value, index) => value === next[index]) ? current : next);
      return;
    }
    const legacy = readTimePresets(storageKey);
    if (!legacy || imported.current) return;
    imported.current = true;
    presetsMutation.mutate({ presets: legacy, onlyIfUnset: true }, {
      onError: () => setPresetError(t("Could not save this preset.")),
    });
  }, [open, presetsQuery.data, presetsQuery.error, presetsMutation, storageKey]);

  useEffect(() => {
    if (!open) return;
    const previous = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    const sheet = sheetRef.current;
    const updateViewport = () => {
      const viewport = window.visualViewport;
      sheet?.parentElement?.style.setProperty("--sheet-height", `${viewport?.height ?? window.innerHeight}px`);
      sheet?.parentElement?.style.setProperty("--sheet-top", `${viewport?.offsetTop ?? 0}px`);
    };
    updateViewport();
    window.addEventListener("resize", updateViewport);
    window.visualViewport?.addEventListener("resize", updateViewport);
    window.visualViewport?.addEventListener("scroll", updateViewport);
    sheet?.querySelector<HTMLElement>("button")?.focus();
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") { event.preventDefault(); onClose(); }
      if (event.key !== "Tab") return;
      const focusable = Array.from(sheet?.querySelectorAll<HTMLElement>("button, input, [tabindex='0']") ?? [])
        .filter(element => !(element as HTMLButtonElement).disabled && element.tabIndex >= 0 && !element.closest("[inert], [hidden], [aria-hidden='true']"));
      const first = focusable[0]; const last = focusable[focusable.length - 1];
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last?.focus(); }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus(); }
    };
    window.addEventListener("keydown", handleKeyDown, true);
    return () => {
      window.removeEventListener("keydown", handleKeyDown, true);
      window.removeEventListener("resize", updateViewport);
      window.visualViewport?.removeEventListener("resize", updateViewport);
      window.visualViewport?.removeEventListener("scroll", updateViewport);
      document.body.style.overflow = previousOverflow;
      if (previous?.isConnected) previous.focus();
    };
  }, [onClose, open]);

  if (!open || !editingDays.length) return null;
  const parsedValue = parseAvailabilityCode(customValue);
  const range = parseFlexibleTimeRange(customValue);
  const hasError = choice === null || (choice === "custom" && (textMode
    ? !parsedValue.ok || !customValue.trim() || parsedValue.value.intervalStr === null
    : !range));
  const canManage = Boolean(presetsQuery.data) && !presetsQuery.error && !presetsQuery.isFetching && !presetsMutation.isPending;
  const commitRange = (from: string, to: string) => {
    setFromTime(from); setToTime(to); setCustomValue(`${from} - ${to}`);
  };
  const savePresets = async (next: string[]) => {
    const result = await new Promise<{ presets: string[] | null }>((resolve, reject) => presetsMutation.mutate({ presets: next }, { onSuccess: resolve, onError: reject }));
    if (result.presets) setTimePresets(result.presets);
    setPresetError(null);
    try { if (storageKey && result.presets) window.localStorage.setItem(storageKey, JSON.stringify(result.presets)); } catch { /* The database remains authoritative when browser storage is unavailable. */ }
  };
  const closePresetEditor = () => {
    setEditingPresetIndex(null);
    window.requestAnimationFrame(() => sheetRef.current?.querySelector<HTMLButtonElement>("[data-edit-presets]")?.focus());
  };
  const options: { value: AvailabilityChoice; title: string; subtitle: string }[] = [
    { value: "any", title: t("Available (whole day)"), subtitle: t("Available for the entire day") },
    { value: "none", title: t("Not available"), subtitle: t("Not working this day") },
    { value: "custom", title: t("Custom time"), subtitle: t("Select specific working hours") },
  ];

  return createPortal(
    <div className={styles.dialogOverlay} role="dialog" aria-modal="true" aria-label={editingDays.length === 1 ? t("Day {0} availability", editingDays[0]) : t("{0} days selected", editingDays.length)} onMouseDown={(event: MouseEvent<HTMLDivElement>) => { if (event.target === event.currentTarget) onClose(); }}>
      <div className={styles.dayDialog} ref={sheetRef}>
        <span className={styles.dragHandle} aria-hidden="true" />
        <div className={styles.dialogHeader}>
          <div><span className={styles.dialogEyebrow}>{editingDays.length === 1 ? dateLabel : t("{0} days selected", editingDays.length)}</span><h3>{t("Availability")}</h3></div>
          <button type="button" className={styles.iconButton} aria-label={t("Close")} onClick={onClose}><CloseIcon size={16} /></button>
        </div>
        <div className={styles.dialogContent}>
          <div className={styles.availabilityOptions} role="radiogroup" aria-label={t("Availability")} onKeyDown={event => {
            const current = options.findIndex(option => option.value === (event.target as HTMLElement).dataset.choice);
            if (current < 0 || !["ArrowDown", "ArrowRight", "ArrowUp", "ArrowLeft", "Home", "End"].includes(event.key)) return;
            event.preventDefault();
            const index = event.key === "Home" ? 0 : event.key === "End" ? 2 : (current + (["ArrowDown", "ArrowRight"].includes(event.key) ? 1 : 2)) % 3;
            setChoice(options[index].value); setEditingPresetIndex(null); setManagingPresets(false);
            event.currentTarget.querySelectorAll<HTMLButtonElement>("[role='radio']")[index]?.focus();
          }}>
            {options.map((option, index) => <button key={option.value} type="button" role="radio" aria-checked={choice === option.value} data-choice={option.value}
              tabIndex={choice === option.value || (choice === null && index === 0) ? 0 : -1}
              className={joinClassNames(styles.availabilityOption, choice === option.value && styles.availabilityOptionSelected)}
              onClick={() => { setChoice(option.value); setManagingPresets(false); setEditingPresetIndex(null); }}>
              <span className={joinClassNames(styles.optionIcon, option.value === "none" && styles.optionIconNone)} aria-hidden="true">
                {option.value === "custom" ? <AvailabilityClockIcon /> : option.value === "none" ? "−" : <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round"><circle cx="12" cy="12" r="4" /><path d="M12 2v2m0 16v2M2 12h2m16 0h2M5 5l1.5 1.5m11 11L19 19M5 19l1.5-1.5m11-11L19 5" /></svg>}
              </span>
              <span className={styles.optionText}><strong>{option.title}</strong><small>{option.subtitle}</small></span>
              <span className={styles.optionCheck} aria-hidden="true">{choice === option.value ? <CheckIcon size={12} /> : null}</span>
            </button>)}
          </div>
          <div className={styles.customTimePanel} data-open={choice === "custom"} aria-hidden={choice !== "custom"} inert={choice !== "custom"}>
            <div className={styles.customTimeInner}>
              <section className={styles.otherTime} aria-label={t("Other time")}>
                <h4>{t("Other time")}</h4>
                {textMode ? <label className={styles.customField}><span>{t("Custom")}</span><input value={customValue} onChange={event => setCustomValue(event.target.value)} aria-invalid={hasError} /></label> : <div className={styles.timeFields}>
                  <label><span>{t("From")}</span><span className={styles.timeInput}><AvailabilityClockIcon /><input type="time" value={fromTime} onChange={event => commitRange(event.target.value, toTime)} aria-invalid={hasError} /></span></label>
                  <span className={styles.timeSeparator} aria-hidden="true">–</span>
                  <label><span>{t("To")}</span><span className={styles.timeInput}><AvailabilityClockIcon /><input type="time" value={toTime} onChange={event => commitRange(fromTime, event.target.value)} aria-invalid={hasError} /></span></label>
                </div>}
                <button type="button" className={styles.textButton} onClick={() => {
                  if (textMode) { const parsed = parseFlexibleTimeRange(customValue); commitRange(parsed?.from ?? fromTime, parsed?.to ?? toTime); }
                  setTextMode(!textMode);
                }}>{textMode ? t("Use time range") : t("Use custom value")}</button>
                {hasError && choice === "custom" ? <p className={styles.dialogError}>{!parsedValue.ok ? parsedValue.error : t("Use a valid time range like 09:00 - 15:00.")}</p> : null}
                <div className={styles.quickPresetsHeader}><h4>{t("Quick presets")}</h4>
                  <button type="button" className={styles.textButton} data-edit-presets disabled={!canManage} onClick={() => { setManagingPresets(!managingPresets); setEditingPresetIndex(null); }}>
                    <AccountSettingsIcon name="edit" size={14} />{managingPresets ? t("Done") : t("Edit presets")}
                  </button>
                </div>
                {presetsQuery.error ? <p className={styles.dialogError} role="alert">{t("Could not load time presets.")}</p> : null}
                {presetError ? <p className={styles.dialogError} role="alert">{presetError}</p> : null}
                {editingPresetIndex !== null ? <AvailabilityTimePresetEditor key={editingPresetIndex} value={timePresets[editingPresetIndex] ?? defaultTimePresets[0]}
                  onCancel={closePresetEditor} onSave={async value => {
                    const next = editingPresetIndex === timePresets.length ? [...timePresets, value] : timePresets.map((preset, index) => index === editingPresetIndex ? value : preset);
                    await savePresets(next); closePresetEditor();
                  }} /> : <>
                  <div className={styles.presetChips}>{timePresets.map((value, index) => <div className={styles.presetChipItem} key={index}>
                    <button type="button" className={joinClassNames(styles.presetChip, range?.label === value && styles.presetChipSelected)}
                      aria-label={managingPresets ? t("Edit preset {0}", value) : value.replace(" - ", " – ")}
                      aria-pressed={!managingPresets && range?.label === value} disabled={managingPresets && !canManage}
                      onClick={() => { if (managingPresets) setEditingPresetIndex(index); else { const parsed = parseFlexibleTimeRange(value)!; commitRange(parsed.from, parsed.to); setTextMode(false); } }}>
                      {value.replace(" - ", " – ")}
                    </button>
                    {managingPresets && index >= 3 ? <button type="button" className={styles.removePreset} aria-label={t("Remove preset {0}", value)} disabled={!canManage} onClick={async () => {
                      try { await savePresets(timePresets.filter((_, position) => position !== index)); } catch { setPresetError(t("Could not save this preset.")); }
                    }}><CloseIcon size={12} /></button> : null}
                  </div>)}</div>
                  {managingPresets ? <button type="button" className={styles.textButton} disabled={!canManage || timePresets.length >= 100} onClick={() => setEditingPresetIndex(timePresets.length)}>{t("Add preset")}</button> : null}
                </>}
              </section>
            </div>
          </div>
        </div>
        <div className={styles.dialogActions}>
          <IosButton label={t("Cancel")} variant="secondary" className={styles.dialogActionButton} disabled={presetsMutation.isPending} onClick={onClose} />
          <IosButton label={t("Save")} icon={<CheckIcon size={16} />} className={styles.dialogActionButton} disabled={hasError || editingPresetIndex !== null || presetsMutation.isPending} onClick={() => {
            if (!hasError && choice) onApply(editingDays, choice === "any" ? AVAILABILITY_ANY_MARK : choice === "none" ? AVAILABILITY_NONE_MARK : range?.label ?? (parsedValue.ok ? parsedValue.value.normalizedCode : customValue));
          }} />
        </div>
      </div>
    </div>, document.body,
  );
}

export function EmployeeAvailabilityPage() {
  const [tab, setTab] = useState<"availability" | "urlop">("availability");
  const urlopCalendar = useEmployeeUrlopCalendarQuery();
  const { session } = useAuth();
  const presetStorageKey = session?.employeeId
    ? `gf3:employee-availability:time-presets:${session.employeeId}`
    : null;
  const availabilityQuery = useEmployeeAvailabilityListQuery();
  const saveMutation = useSaveEmployeeAvailabilityMutation();
  const groups = availabilityQuery.data ?? [];
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [expandedId, setExpandedId] = useState<number | null>(null);
  const selectedAvailability = useMemo(
    () => groups.find(group => group.id === selectedId) ?? null,
    [groups, selectedId],
  );
  const availabilityDraftSource = useMemo(
    () => buildAvailabilityDraftSource(selectedAvailability),
    [selectedAvailability],
  );
  const estimatedHours = selectedAvailability && session?.employeeId
    ? estimateAvailabilityHours(selectedAvailability)
    : null;
  const {
    value: draft,
    setValue: setDraft,
    reset: resetDraft,
  } = useSyncedDraft(availabilityDraftSource.sourceKey, availabilityDraftSource.draft);
  const [committedDraftSnapshot, setCommittedDraftSnapshot] = useState(() => availabilityDraftSource.snapshot);
  const [editingDays, setEditingDays] = useState<number[]>([]);
  const [selectedDays, setSelectedDays] = useState<number[]>([]);
  const closeSheet = useCallback(() => { setEditingDays([]); setSelectedDays([]); }, []);
  const [saveSuccessMessage, setSaveSuccessMessage] = useState<string | null>(null);
  const [closedMessage, setClosedMessage] = useState<string | null>(null);
  const selectedEditLockedMessage = selectedAvailability?.isEditLocked
    ? getAvailabilityEditLockedMessage(selectedAvailability)
    : null;

  useEffect(() => {
    setCommittedDraftSnapshot(availabilityDraftSource.snapshot);
    closeSheet();
    setClosedMessage(null);
  }, [availabilityDraftSource.snapshot, availabilityDraftSource.sourceKey, closeSheet]);

  useEffect(() => {
    setSaveSuccessMessage(null);
    setClosedMessage(null);
  }, [selectedId]);

  useEffect(() => {
    if (selectedEditLockedMessage) {
      closeSheet();
      setClosedMessage("availability-locked");
      return;
    }

    setClosedMessage(currentMessage =>
      currentMessage === "availability-locked" ? null : currentMessage,
    );
  }, [selectedEditLockedMessage, closeSheet]);

  useEffect(() => {
    if (!closedMessage) {
      return;
    }

    const timeoutId = window.setTimeout(() => setClosedMessage(null), 4200);
    return () => window.clearTimeout(timeoutId);
  }, [closedMessage]);

  useEffect(() => {
    if (!saveSuccessMessage) {
      return;
    }

    const timeoutId = window.setTimeout(() => setSaveSuccessMessage(null), 3000);
    return () => window.clearTimeout(timeoutId);
  }, [saveSuccessMessage]);

  const dayNumbers = useMemo(() => {
    if (!selectedAvailability) {
      return [];
    }

    return Array.from(
      { length: getDaysInMonth(selectedAvailability.year, selectedAvailability.month) },
      (_, index) => index + 1,
    );
  }, [selectedAvailability]);

  const currentDraftSnapshot = useMemo(() => buildDraftSnapshot(draft), [draft]);
  const hasUnsavedChanges = useMemo(
    () => Boolean(selectedAvailability) && currentDraftSnapshot !== committedDraftSnapshot,
    [committedDraftSnapshot, currentDraftSnapshot, selectedAvailability],
  );
  const { confirmIfNeeded, dialog: unsavedChangesDialog } = useUnsavedChangesPrompt({
    when: hasUnsavedChanges && !saveMutation.isPending,
  });

  const canSubmit = expandedId === selectedAvailability?.id && isAvailabilityOpen(selectedAvailability) && !saveMutation.isPending
    && Boolean(selectedAvailability?.members?.some(member => member.employeeId === session?.employeeId));
  useEffect(() => { if (!canSubmit) closeSheet(); }, [canSubmit, closeSheet]);
  const commonDay = editingDays.length ? draft[editingDays[0]] : undefined;
  const selectedDay = commonDay && editingDays.every(day => draft[day]?.available === commonDay.available && draft[day]?.intervalStr === commonDay.intervalStr) ? commonDay : undefined;
  const queryErrorMessage = availabilityQuery.error
    ? getErrorMessage(availabilityQuery.error, t("Could not load availability."))
    : null;
  const saveErrorMessage = saveMutation.error
    ? getErrorMessage(saveMutation.error, t("Could not save availability."))
    : null;

  const handleToggleAvailability = (groupId: number) => {
    if (saveMutation.isPending) return;
    const nextExpandedId = expandedId === groupId ? null : groupId;
    confirmIfNeeded(() => {
      closeSheet();
      if (nextExpandedId === null && hasUnsavedChanges) {
        resetDraft(availabilityDraftSource.draft);
        setCommittedDraftSnapshot(availabilityDraftSource.snapshot);
      }
      if (nextExpandedId !== null) setSelectedId(nextExpandedId);
      setExpandedId(nextExpandedId);
    });
  };

  const handleOpenDayDialog = (days: number[]) => {
    if (selectedAvailability?.isEditLocked) {
      setClosedMessage("availability-locked");
      return;
    }

    if (!canSubmit) {
      setClosedMessage(t("The time for editing this availability has expired."));
      return;
    }

    setClosedMessage(null);
    setSelectedDays(days);
    setEditingDays(days);
  };

  const handleApplyDayValue = (days: number[], value: string) => {
    const parsed = parseAvailabilityCode(value);
    if (!parsed.ok) {
      return;
    }

    setSaveSuccessMessage(null);
    if (!canSubmit) return;
    setDraft(currentDraft => applyEmployeeAvailabilityValue(currentDraft, days, parsed.value));
    closeSheet();
  };

  const handleClear = () => {
    if (!selectedAvailability || !canSubmit) {
      return;
    }

    setSaveSuccessMessage(null);
    setDraft(
      dayNumbers.reduce<DayDraftMap>((nextDraft, dayOfMonth) => {
        nextDraft[dayOfMonth] = { available: false, intervalStr: null };
        return nextDraft;
      }, {}),
    );
  };

  const handleSave = () => {
    if (!selectedAvailability || !canSubmit) {
      return;
    }

    closeSheet();
    setSaveSuccessMessage(null);
    saveMutation.mutate(
      {
        id: selectedAvailability.id,
        payload: {
          slots: buildSaveSlots(selectedAvailability, draft),
        },
      },
      {
        onSuccess: (availability) => {
          const nextDraft = buildDraftFromAvailability(availability);

          resetDraft(nextDraft);
          setCommittedDraftSnapshot(buildDraftSnapshot(nextDraft));
          setSaveSuccessMessage(t("Availability saved successfully."));
        },
      },
    );
  };

  return (
    <div className={workspaceStyles.page}>
      <div className={styles.urlopTabs} role="tablist" aria-label={t("Availability")}>
        <button type="button" role="tab" id="availability-tab" aria-controls="availability-panel" aria-selected={tab === "availability"} onClick={() => setTab("availability")}>{t("Availability")}</button>
        <button type="button" role="tab" id="urlop-tab" aria-controls="urlop-panel" aria-selected={tab === "urlop"} onClick={() => setTab("urlop")}>{t("Urlop")}</button>
      </div>
      <div role="tabpanel" id="urlop-panel" aria-labelledby="urlop-tab" hidden={tab !== "urlop"}><EmployeeUrlopPanel /></div>
      <div role="tabpanel" id="availability-panel" aria-labelledby="availability-tab" hidden={tab !== "availability"}>
      <h1 className={styles.pageTitle}>{t("Public windows")}</h1>
      {queryErrorMessage ? <ErrorBanner dismissible={false}>{queryErrorMessage}</ErrorBanner> : null}
      {saveErrorMessage ? <ErrorBanner>{saveErrorMessage}</ErrorBanner> : null}
      {saveSuccessMessage ? (
        <div className={styles.successBanner} role="status">
          <div className={styles.successToast}>
            <span className={styles.successIcon} aria-hidden="true">
              <CheckIcon size={16} />
            </span>
            <span>{saveSuccessMessage}</span>
            <button
              type="button"
              className={styles.successClose}
              aria-label={t("Close success message")}
              onClick={() => setSaveSuccessMessage(null)}
            >
              <CloseIcon size={14} />
            </button>
          </div>
        </div>
      ) : null}

      {availabilityQuery.isLoading ? (
        <section className={workspaceStyles.panel} data-employee-motion>
          <span className={workspaceStyles.panelEyebrow}>{t("Loading")}</span>
          <p className={workspaceStyles.panelText}>{t("Checking public availability windows.")}</p>
        </section>
      ) : null}

      {!availabilityQuery.isLoading && groups.length === 0 ? (
        <section className={workspaceStyles.panel} data-employee-motion>
          <span className={workspaceStyles.panelEyebrow}>{t("No active windows")}</span>
          <h2 className={workspaceStyles.panelTitle}>{t("Nothing is public for your account yet.")}</h2>
          <p className={workspaceStyles.panelText}>
            {t("When a manager publishes an availability window for you, it will show up here.")}</p>
        </section>
      ) : null}

      <div className={styles.shell}>
        {[...groups].sort((left, right) => Number(right.id === expandedId) - Number(left.id === expandedId)).map(group => {
          const isExpanded = group.id === expandedId;
          const contentId = `employee-availability-content-${group.id}`;
          const toggleId = `employee-availability-toggle-${group.id}`;
          return (
            <section key={group.id} className={styles.availabilityCard} aria-labelledby={toggleId}>
              <h2 className={styles.cardHeading}>
                <button id={toggleId} type="button" className={styles.cardToggle} aria-expanded={isExpanded} aria-controls={contentId}
                  aria-label={`${group.name} ${getAvailabilityGroupPeriodLabel(group)} ${getAvailabilityStatusLabel(group)}`}
                  disabled={saveMutation.isPending} onClick={() => handleToggleAvailability(group.id)}>
                  <span className={styles.calendarIcon} aria-hidden="true"><AvailabilityIcon size={23} /></span>
                  <span className={styles.cardTitles}>
                    <span className={styles.cardName} title={group.name}>{group.name}</span>
                    <span className={styles.cardMonth}>{getAvailabilityGroupPeriodLabel(group)}</span>
                  </span>
                  <span className={joinClassNames(styles.statusPill, isAvailabilityOpen(group) && styles.statusPillOpen, group.isEditLocked && styles.statusPillLocked)}>
                    {getAvailabilityStatusLabel(group)}
                  </span>
                  <ChevronRightIcon size={16} className={styles.cardChevron} aria-hidden="true" />
                </button>
              </h2>
              <div id={contentId} className={styles.cardBody} data-expanded={isExpanded} aria-hidden={!isExpanded} inert={!isExpanded}>
                <div className={styles.cardBodyInner}>
                  {group.id === selectedId && selectedAvailability ? <div className={styles.cardContent}>
                    {selectedAvailability.isAutomatic ? <p role="status">{t("Availability set automatically by contract: + Mon–Fri, − Sat–Sun")}</p> : null}
                    <div className={styles.editorMeta}>{getAvailabilityMonthLabel(selectedAvailability.month)} {selectedAvailability.year} · 1–{dayNumbers.length}</div>
                    <div className={styles.detailsGrid}>
                      <div className={styles.detailItem}>
                        <span className={styles.detailIcon} aria-hidden="true"><EyeIcon size={20} /></span>
                        <div><span>{t("Visible")}</span><strong>{getEmployeeVisibilityWindowLabel(selectedAvailability)}</strong></div>
                      </div>
                      <div className={styles.detailItem}>
                        <span className={`${styles.detailIcon} ${styles.detailIconClock}`} aria-hidden="true">
                          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" focusable="false"><circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 2" /></svg>
                        </span>
                        <div><span>{t("Closes")}</span><strong>{formatEmployeeDateTimeLabel(selectedAvailability.visibleToUtc)}</strong></div>
                      </div>
                    </div>

                    {closedMessage && isAvailabilityOpen(selectedAvailability) ? (
                      <div className={styles.closedMessage} role="status">
                        {closedMessage === "availability-locked" ? selectedEditLockedMessage : closedMessage}
                      </div>
                    ) : null}

                    {!isAvailabilityOpen(selectedAvailability) ? <p className={styles.closedMessage} role="status">{selectedEditLockedMessage ?? (selectedAvailability.isAutomatic ? t("Automatic availability cannot be edited.") : t("The time for editing this availability has expired."))}</p> : null}
                    <div className={styles.teamNotice}>
                      <strong>{t("Team availability")}</strong><span>{t("{0} employees", new Set((selectedAvailability.members ?? []).map(member => member.employeeId)).size)}</span>
                      <small>{t("You can edit only your availability")}</small>
                    </div>
                    <EmployeeAvailabilityMatrix group={selectedAvailability} currentEmployeeId={session?.employeeId ?? 0} ownDraft={draft} canEdit={canSubmit} selectedDays={selectedDays} onSelectedDaysChange={setSelectedDays} onRequestEdit={handleOpenDayDialog} urlopCalendar={urlopCalendar.data} />
                    <div className={styles.legend}><span>+ {t("Available (whole day)")}</span><span>− {t("Not available")}</span><span>09:00–15:00 {t("Other time")}</span></div>
                    {estimatedHours && estimatedHours.status !== "unsubmitted" ? (
                      <div className={styles.hoursSummary} role="region" aria-label={t("Estimated working hours")}>
                        <span className={styles.hoursIcon} aria-hidden="true"><AvailabilityClockIcon /></span>
                        <div>
                          <strong>{t("Estimated working hours")}</strong>
                          <span className={styles.hoursValue}>{estimatedHours.status === "unavailable" ? t("Estimate unavailable")
                            : estimatedHours.potentialHours === 0 ? t("{0} h", 0)
                            : t("~{0}–{1} h", estimatedHours.lowerEstimate, estimatedHours.upperEstimate)}</span>
                          <small>{estimatedHours.status === "unavailable" ? t("Some saved days are missing or use unsupported values.")
                            : estimatedHours.potentialHours === 0 ? t("No available working hours in your saved availability.")
                            : t("Based on your availability.")} {t("Actual scheduled hours may differ.")}</small>
                        </div>
                      </div>
                    ) : null}
                    <div className={styles.actions}>
                      <IosButton
                        label={saveMutation.isPending ? t("Saving...") : t("Save availability")}
                        icon={<SaveIcon size={14} />}
                        size="compact"
                        className={styles.editorActionButton}
                        disabled={!canSubmit}
                        onClick={handleSave}
                      />
                      <IosButton
                        label={t("Clear")}
                        icon={<CloseIcon size={14} />}
                        variant="secondary"
                        size="compact"
                        className={`${styles.editorActionButton} ${styles.editorActionButtonClear}`}
                        disabled={!canSubmit}
                        onClick={handleClear}
                      />
                    </div>
                  </div> : null}
                </div>
              </div>
            </section>
          );
        })}
      </div>

      {selectedAvailability ? (
        <AvailabilitySelectionSheet
          key={presetStorageKey}
          storageKey={presetStorageKey}
          employeeId={session?.employeeId ?? 0}
          open={editingDays.length > 0}
          editingDays={editingDays}
          dateLabel={`${editingDays[0]} ${getAvailabilityMonthLabel(selectedAvailability.month)} ${selectedAvailability.year}`}
          day={selectedDay}
          onClose={closeSheet}
          onApply={handleApplyDayValue}
        />
      ) : null}

      </div>
      {unsavedChangesDialog}
    </div>
  );
}
