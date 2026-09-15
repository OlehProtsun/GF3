import { dateTimeFormat } from "@shared/i18n";
import { t } from "@shared/i18n";
import { useEffect, useMemo, useState, type MouseEvent } from "react";
import {
  useEmployeeAvailabilityListQuery,
  useSaveEmployeeAvailabilityMutation,
  type EmployeeAvailabilityGroup,
} from "@entities/employee-availability";
import type { AvailabilityKind } from "@entities/availability-groups/model/types";
import {
  AVAILABILITY_KIND_ANY,
  AVAILABILITY_KIND_INTERVAL,
  AVAILABILITY_KIND_NONE,
  AVAILABILITY_ANY_MARK,
  AVAILABILITY_NONE_MARK,
  getAvailabilityWeekdayLabel,
  getDaysInMonth,
  parseAvailabilityCode,
} from "@entities/availability-groups/model/editor";
import {
  getAvailabilityGroupPeriodLabel,
  getAvailabilityMonthLabel,
} from "@entities/availability-groups/model/presentation";
import { getErrorMessage } from "@shared/api/httpClient";
import { stableSerialize } from "@shared/lib/stableSerialize";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import { AvailabilityIcon, CheckIcon, CloseIcon, SaveIcon } from "@shared/ui/icons";
import { parseFlexibleTimeRange } from "@shared/lib/timeRange";
import { useSyncedDraft } from "@shared/lib/useSyncedDraft";
import { useUnsavedChangesPrompt } from "@shared/lib/useUnsavedChangesPrompt";
import workspaceStyles from "@pages/shared/EmployeeWorkspacePage.module.css";
import styles from "./EmployeeAvailabilityPage.module.css";

type DayDraft = {
  available: boolean;
  intervalStr: string | null;
};

type DayDraftMap = Record<number, DayDraft>;

const dayValuePresets = [
  { label: "9:00 - 15:00", value: "09:00 - 15:00" },
  { label: "15:00 - 21:00", value: "15:00 - 21:00" },
  { label: "9:00 - 21:00", value: "09:00 - 21:00" },
  { label: AVAILABILITY_ANY_MARK, value: AVAILABILITY_ANY_MARK },
  { label: AVAILABILITY_NONE_MARK, value: AVAILABILITY_NONE_MARK },
] as const;

const employeeDateTimeFormatter = dateTimeFormat("en-GB", {
  month: "short",
  day: "numeric",
  hour: "2-digit",
  minute: "2-digit",
  hourCycle: "h23",
});

function joinClassNames(...values: Array<string | false | undefined>) {
  return values.filter(Boolean).join(" ");
}

function padTimePart(value: number) {
  return String(value).padStart(2, "0");
}

function toTimeLabel(totalMinutes: number) {
  const normalizedMinutes = (totalMinutes + 24 * 60) % (24 * 60);
  const hour = Math.floor(normalizedMinutes / 60);
  const minute = normalizedMinutes % 60;
  return `${padTimePart(hour)}:${padTimePart(minute)}`;
}

function getTimeTotalMinutes(value: string) {
  const [rawHour, rawMinute] = value.split(":");
  const hour = Number(rawHour);
  const minute = Number(rawMinute);

  if (!Number.isInteger(hour) || !Number.isInteger(minute)) {
    return 0;
  }

  return Math.min(23, Math.max(0, hour)) * 60 + Math.min(59, Math.max(0, minute));
}

function stepTimeValue(value: string, deltaMinutes: number) {
  return toTimeLabel(getTimeTotalMinutes(value) + deltaMinutes);
}

function getInitialDialogRange(day?: DayDraft) {
  const parsedRange = day?.intervalStr ? parseFlexibleTimeRange(day.intervalStr) : null;

  return {
    from: parsedRange?.from ?? "09:00",
    to: parsedRange?.to ?? "15:00",
    value: day?.available ? day.intervalStr ?? AVAILABILITY_ANY_MARK : "",
  };
}

function formatEmployeeDateTimeLabel(value?: string | null) {
  if (!value) {
    return t("Not set");
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return t("Not set");
  }

  return employeeDateTimeFormatter.format(date);
}

function getEmployeeVisibilityWindowLabel(group: Pick<EmployeeAvailabilityGroup, "visibleFromUtc" | "visibleToUtc">) {
  const from = formatEmployeeDateTimeLabel(group.visibleFromUtc);
  const to = formatEmployeeDateTimeLabel(group.visibleToUtc);

  if (from === "Not set" && to === "Not set") {
    return t("Not configured");
  }

  return `${from} - ${to}`;
}

const availabilityEditLockedMessagePrefix = "This availability is currently being edited by ";

function getAvailabilityEditLockedMessage(group: Pick<EmployeeAvailabilityGroup, "editLockedBy">) {
  const lockedBy = group.editLockedBy?.trim() || t("a manager");
  return t("{0}{1}. You cannot edit it right now.", availabilityEditLockedMessagePrefix, lockedBy);
}

function isAvailabilityEditLockedMessage(value: string | null) {
  return value?.startsWith(availabilityEditLockedMessagePrefix) ?? false;
}

function isAvailabilityOpen(group?: EmployeeAvailabilityGroup | null) {
  return Boolean(group?.canSubmit) && group?.isEditLocked !== true;
}

function getAvailabilityStatusLabel(group: EmployeeAvailabilityGroup) {
  if (group.isEditLocked) {
    return t("Locked");
  }

  return group.canSubmit ? t("Open") : t("Closed");
}

function isAvailableKind(kind: AvailabilityKind, intervalStr?: string | null) {
  if (intervalStr?.trim()) {
    return true;
  }

  if (typeof kind === "number") {
    return kind === AVAILABILITY_KIND_ANY || kind === AVAILABILITY_KIND_INTERVAL;
  }

  const normalizedKind = kind.toUpperCase();
  return normalizedKind === "ANY" || normalizedKind === "INT" || normalizedKind === "AVAILABLE" || normalizedKind === "PREFERRED";
}

function buildDraftFromAvailability(availability: EmployeeAvailabilityGroup): DayDraftMap {
  const daysInMonth = getDaysInMonth(availability.year, availability.month);
  const slotByDay = new Map(availability.slots.map(slot => [slot.dayOfMonth, slot]));

  return Array.from({ length: daysInMonth }, (_, index) => index + 1).reduce<DayDraftMap>((draft, dayOfMonth) => {
    const slot = slotByDay.get(dayOfMonth);
    const intervalStr = slot?.intervalStr?.trim() || null;

    draft[dayOfMonth] = {
      available: slot ? isAvailableKind(slot.kind, intervalStr) : false,
      intervalStr,
    };

    return draft;
  }, {});
}

function buildDraftSnapshot(draft: DayDraftMap) {
  return stableSerialize(
    Object.entries(draft)
      .map(([dayOfMonth, day]) => ({
        dayOfMonth: Number(dayOfMonth),
        available: day.available,
        intervalStr: day.intervalStr ?? null,
      }))
      .sort((left, right) => left.dayOfMonth - right.dayOfMonth),
  );
}

function buildAvailabilityDraftSource(availability: EmployeeAvailabilityGroup | null) {
  const draft = availability ? buildDraftFromAvailability(availability) : {};
  const snapshot = buildDraftSnapshot(draft);
  const sourceKey = availability
    ? `employee-availability:${availability.id}:${availability.year}:${availability.month}:${snapshot}`
    : "employee-availability:none";

  return {
    draft,
    snapshot,
    sourceKey,
  };
}

function buildSaveSlots(availability: EmployeeAvailabilityGroup, draft: DayDraftMap) {
  const daysInMonth = getDaysInMonth(availability.year, availability.month);

  return Array.from({ length: daysInMonth }, (_, index) => {
    const dayOfMonth = index + 1;
    const day = draft[dayOfMonth];
    const intervalStr = day?.available ? day.intervalStr : null;
    const kind = !day?.available
      ? AVAILABILITY_KIND_NONE
      : intervalStr
        ? AVAILABILITY_KIND_INTERVAL
        : AVAILABILITY_KIND_ANY;

    return {
      dayOfMonth,
      kind,
      intervalStr,
    };
  });
}

function getDayStatusLabel(day?: DayDraft) {
  if (!day?.available) {
    return AVAILABILITY_NONE_MARK;
  }

  return day.intervalStr ?? AVAILABILITY_ANY_MARK;
}

function AvailabilityDayDialog({
  open,
  dayOfMonth,
  day,
  onClose,
  onApply,
}: {
  open: boolean;
  dayOfMonth: number | null;
  day?: DayDraft;
  onClose: () => void;
  onApply: (dayOfMonth: number, value: string) => void;
}) {
  const [fromTime, setFromTime] = useState("09:00");
  const [toTime, setToTime] = useState("15:00");
  const [customValue, setCustomValue] = useState("");

  useEffect(() => {
    if (!open) {
      return;
    }

    const nextRange = getInitialDialogRange(day);
    setFromTime(nextRange.from);
    setToTime(nextRange.to);
    setCustomValue(nextRange.value);
  }, [day, open]);

  useEffect(() => {
    if (!open) {
      return;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onClose();
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [onClose, open]);

  if (!open || dayOfMonth === null) {
    return null;
  }

  const parsedValue = parseAvailabilityCode(customValue);
  const hasError = customValue.trim().length > 0 && !parsedValue.ok;
  const selectedPreset = dayValuePresets.find(preset => preset.value === customValue.trim());

  const handleOverlayMouseDown = (event: MouseEvent<HTMLDivElement>) => {
    if (event.target === event.currentTarget) {
      onClose();
    }
  };

  const commitRange = (nextFrom: string, nextTo: string) => {
    setFromTime(nextFrom);
    setToTime(nextTo);
    setCustomValue(`${nextFrom} - ${nextTo}`);
  };

  const handleApply = () => {
    if (hasError) {
      return;
    }

    onApply(dayOfMonth, customValue.trim() || AVAILABILITY_NONE_MARK);
  };

  const handleClear = () => {
    onApply(dayOfMonth, AVAILABILITY_NONE_MARK);
  };

  return (
    <div className={styles.dialogOverlay} role="dialog" aria-modal="true" aria-label={t("Day {0} availability", dayOfMonth)} onMouseDown={handleOverlayMouseDown}>
      <div className={styles.dayDialog}>
        <div className={styles.dialogHeader}>
          <div>
            <span className={styles.dialogEyebrow}>{t("Day")} {dayOfMonth}</span>
            <h3>{t("Availability")}</h3>
          </div>

          <button type="button" className={styles.iconButton} aria-label={t("Close")} onClick={onClose}>
            <CloseIcon size={16} />
          </button>
        </div>

        <div className={styles.presetGrid}>
          {dayValuePresets.map(preset => (
            <button
              key={preset.value}
              type="button"
              className={joinClassNames(
                styles.presetButton,
                selectedPreset?.value === preset.value && styles.presetButtonActive,
              )}
              onClick={() => {
                const parsedRange = parseFlexibleTimeRange(preset.value);
                if (parsedRange) {
                  setFromTime(parsedRange.from);
                  setToTime(parsedRange.to);
                }

                setCustomValue(preset.value);
              }}
            >
              {preset.label}
            </button>
          ))}
        </div>

        <div className={styles.timePanel}>
          <div className={styles.timeStepper}>
            <button type="button" aria-label={t("Decrease start time")} onClick={() => commitRange(stepTimeValue(fromTime, -15), toTime)}>
              -
            </button>
            <span>
              <strong>{fromTime}</strong>
              <small>{t("From")}</small>
            </span>
            <button type="button" aria-label={t("Increase start time")} onClick={() => commitRange(stepTimeValue(fromTime, 15), toTime)}>
              +
            </button>
          </div>

          <span className={styles.timeDash}>-</span>

          <div className={styles.timeStepper}>
            <button type="button" aria-label={t("Decrease end time")} onClick={() => commitRange(fromTime, stepTimeValue(toTime, -15))}>
              -
            </button>
            <span>
              <strong>{toTime}</strong>
              <small>{t("To")}</small>
            </span>
            <button type="button" aria-label={t("Increase end time")} onClick={() => commitRange(fromTime, stepTimeValue(toTime, 15))}>
              +
            </button>
          </div>
        </div>

        <label className={styles.customField}>
          <span>{t("Custom")}</span>
          <input
            value={customValue}
            onChange={event => setCustomValue(event.target.value)}
            placeholder="09:00 - 15:00"
            aria-invalid={hasError}
          />
        </label>

        {hasError ? <p className={styles.dialogError}>{parsedValue.error}</p> : null}

        <div className={styles.dialogActions}>
          <IosButton
            label={t("Clear")}
            icon={<CloseIcon size={16} />}
            variant="secondary"
            disabled={false}
            onClick={handleClear}
          />
          <IosButton
            label={t("Save")}
            icon={<CheckIcon size={16} />}
            disabled={hasError}
            onClick={handleApply}
          />
        </div>
      </div>
    </div>
  );
}

export function EmployeeAvailabilityPage() {
  const availabilityQuery = useEmployeeAvailabilityListQuery();
  const saveMutation = useSaveEmployeeAvailabilityMutation();
  const groups = availabilityQuery.data ?? [];
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const selectedAvailability = useMemo(
    () => groups.find(group => group.id === selectedId) ?? groups[0] ?? null,
    [groups, selectedId],
  );
  const availabilityDraftSource = useMemo(
    () => buildAvailabilityDraftSource(selectedAvailability),
    [selectedAvailability],
  );
  const {
    value: draft,
    setValue: setDraft,
    reset: resetDraft,
  } = useSyncedDraft(availabilityDraftSource.sourceKey, availabilityDraftSource.draft);
  const [committedDraftSnapshot, setCommittedDraftSnapshot] = useState(() => availabilityDraftSource.snapshot);
  const [editingDayOfMonth, setEditingDayOfMonth] = useState<number | null>(null);
  const [saveSuccessMessage, setSaveSuccessMessage] = useState<string | null>(null);
  const [closedMessage, setClosedMessage] = useState<string | null>(null);
  const selectedEditLockedMessage = selectedAvailability?.isEditLocked
    ? getAvailabilityEditLockedMessage(selectedAvailability)
    : null;

  useEffect(() => {
    setCommittedDraftSnapshot(availabilityDraftSource.snapshot);
    setEditingDayOfMonth(null);
    setClosedMessage(null);
  }, [availabilityDraftSource.snapshot, availabilityDraftSource.sourceKey]);

  useEffect(() => {
    setSaveSuccessMessage(null);
    setClosedMessage(null);
  }, [selectedId]);

  useEffect(() => {
    if (selectedEditLockedMessage) {
      setEditingDayOfMonth(null);
      setClosedMessage(selectedEditLockedMessage);
      return;
    }

    setClosedMessage(currentMessage =>
      isAvailabilityEditLockedMessage(currentMessage) ? null : currentMessage,
    );
  }, [selectedEditLockedMessage]);

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

  const availableDaysCount = useMemo(
    () => Object.values(draft).filter(day => day.available).length,
    [draft],
  );
  const currentDraftSnapshot = useMemo(() => buildDraftSnapshot(draft), [draft]);
  const hasUnsavedChanges = useMemo(
    () => Boolean(selectedAvailability) && currentDraftSnapshot !== committedDraftSnapshot,
    [committedDraftSnapshot, currentDraftSnapshot, selectedAvailability],
  );
  const { confirmIfNeeded, dialog: unsavedChangesDialog } = useUnsavedChangesPrompt({
    when: hasUnsavedChanges && !saveMutation.isPending,
  });

  const canSubmit = isAvailabilityOpen(selectedAvailability) && !saveMutation.isPending;
  const queryErrorMessage = availabilityQuery.error
    ? getErrorMessage(availabilityQuery.error, t("Could not load availability."))
    : null;
  const saveErrorMessage = saveMutation.error
    ? getErrorMessage(saveMutation.error, t("Could not save availability."))
    : null;

  const handleSelectAvailability = (nextId: number) => {
    if (nextId === selectedAvailability?.id) {
      return;
    }

    confirmIfNeeded(() => setSelectedId(nextId));
  };

  const handleOpenDayDialog = (dayOfMonth: number) => {
    if (selectedAvailability?.isEditLocked) {
      setClosedMessage(getAvailabilityEditLockedMessage(selectedAvailability));
      return;
    }

    if (!canSubmit) {
      setClosedMessage(t("The time for editing this availability has expired."));
      return;
    }

    setClosedMessage(null);
    setEditingDayOfMonth(dayOfMonth);
  };

  const handleApplyDayValue = (dayOfMonth: number, value: string) => {
    const parsed = parseAvailabilityCode(value);
    if (!parsed.ok) {
      return;
    }

    setSaveSuccessMessage(null);
    setDraft(currentDraft => {
      const isNone = parsed.value.normalizedCode === AVAILABILITY_NONE_MARK && !parsed.value.intervalStr;
      return {
        ...currentDraft,
        [dayOfMonth]: isNone
          ? { available: false, intervalStr: null }
          : {
              available: true,
              intervalStr: parsed.value.normalizedCode === AVAILABILITY_ANY_MARK ? null : parsed.value.intervalStr ?? parsed.value.normalizedCode,
            },
      };
    });
    setEditingDayOfMonth(null);
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
        <section className={workspaceStyles.panel}>
          <span className={workspaceStyles.panelEyebrow}>{t("Loading")}</span>
          <p className={workspaceStyles.panelText}>{t("Checking public availability windows.")}</p>
        </section>
      ) : null}

      {!availabilityQuery.isLoading && groups.length === 0 ? (
        <section className={workspaceStyles.panel}>
          <span className={workspaceStyles.panelEyebrow}>{t("No active windows")}</span>
          <h2 className={workspaceStyles.panelTitle}>{t("Nothing is public for your account yet.")}</h2>
          <p className={workspaceStyles.panelText}>
            {t("When a manager publishes an availability window for you, it will show up here.")}</p>
        </section>
      ) : null}

      {selectedAvailability ? (
        <div className={styles.shell}>
          <section className={`${workspaceStyles.panel} ${styles.windowsPanel}`}>
            <div className={styles.windowsHeader}>
              <div className={styles.windowsHeading}>
                <span className={styles.windowsIcon} aria-hidden="true">
                  <AvailabilityIcon size={20} />
                </span>
                <div>
                  <span className={workspaceStyles.panelEyebrow}>{t("Public windows")}</span>
                  <strong>{groups.length}  {t("active")}</strong>
                </div>
              </div>

              <span className={styles.availableCount}>{availableDaysCount}  {t("available")}</span>
            </div>

            <div className={styles.groupList}>
              {groups.map(group => {
                const isSelected = group.id === selectedAvailability.id;

                return (
                  <button
                    key={group.id}
                    type="button"
                    className={[styles.groupButton, isSelected ? styles.groupButtonActive : ""].filter(Boolean).join(" ")}
                    aria-pressed={isSelected}
                    onClick={() => handleSelectAvailability(group.id)}
                  >
                    <span className={styles.groupDateBadge}>
                      <span>{getAvailabilityMonthLabel(group.month, "short")}</span>
                      <strong>{group.year}</strong>
                    </span>

                    <span className={styles.groupContent}>
                      <span className={styles.groupName}>{group.name}</span>
                      <span className={styles.groupMeta}>{getEmployeeVisibilityWindowLabel(group)}</span>
                    </span>

                    <span className={joinClassNames(
                      styles.groupState,
                      isAvailabilityOpen(group) && styles.groupStateOpen,
                      group.isEditLocked && styles.groupStateLocked,
                    )}>
                      {getAvailabilityStatusLabel(group)}
                    </span>
                  </button>
                );
              })}
            </div>
          </section>

          <section className={`${workspaceStyles.panel} ${styles.editorPanel}`}>
            <div className={styles.editorHeader}>
              <div className={styles.editorTitleBlock}>
                <h2 className={workspaceStyles.panelTitle}>{selectedAvailability.name}</h2>
                <span className={styles.editorMeta}>{getAvailabilityGroupPeriodLabel(selectedAvailability, "compact")}</span>
              </div>

              <span className={joinClassNames(
                styles.statusPill,
                isAvailabilityOpen(selectedAvailability) && styles.statusPillOpen,
                selectedAvailability.isEditLocked && styles.statusPillLocked,
              )}>
                {getAvailabilityStatusLabel(selectedAvailability)}
              </span>
            </div>

            <div className={styles.detailsGrid}>
              <div className={styles.detailItem}>
                <span>{t("Visible")}</span>
                <strong>{getEmployeeVisibilityWindowLabel(selectedAvailability)}</strong>
              </div>
              <div className={styles.detailItem}>
                <span>{t("Closes")}</span>
                <strong>{formatEmployeeDateTimeLabel(selectedAvailability.visibleToUtc)}</strong>
              </div>
            </div>

            {closedMessage ? (
              <div className={styles.closedMessage} role="status">
                {closedMessage}
              </div>
            ) : null}

            <div className={styles.dayGrid}>
              {dayNumbers.map(dayOfMonth => {
                const day = draft[dayOfMonth];
                const isAvailable = Boolean(day?.available);

                return (
                  <button
                    key={dayOfMonth}
                    type="button"
                    className={[
                      styles.dayButton,
                      isAvailable ? styles.dayButtonAvailable : "",
                      !canSubmit ? styles.dayButtonClosed : "",
                    ].filter(Boolean).join(" ")}
                    aria-pressed={isAvailable}
                    aria-disabled={!canSubmit}
                    disabled={saveMutation.isPending}
                    onClick={() => handleOpenDayDialog(dayOfMonth)}
                  >
                    <span className={styles.dayNumber}>{dayOfMonth}</span>
                    <span className={styles.dayWeekday}>
                      {getAvailabilityWeekdayLabel(selectedAvailability.year, selectedAvailability.month, dayOfMonth)}
                    </span>
                    <span className={styles.dayState}>{getDayStatusLabel(day)}</span>
                  </button>
                );
              })}
            </div>

            <div className={styles.actions}>
              <IosButton
                label={saveMutation.isPending ? t("Saving...") : t("Save")}
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
          </section>

          <AvailabilityDayDialog
            open={editingDayOfMonth !== null}
            dayOfMonth={editingDayOfMonth}
            day={editingDayOfMonth === null ? undefined : draft[editingDayOfMonth]}
            onClose={() => setEditingDayOfMonth(null)}
            onApply={handleApplyDayValue}
          />
        </div>
      ) : null}

      {unsavedChangesDialog}
    </div>
  );
}
