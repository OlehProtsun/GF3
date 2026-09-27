import { t } from "@shared/i18n";
import { useEffect, useId, useMemo, useState, type MouseEvent } from "react";
import type { AvailabilityTransferSource } from "@entities/availability-groups/model/transfer";
import {
  getAvailabilityCellKey,
  getAvailabilityCodeFromKind,
  type AvailabilityMatrixCellMap,
} from "@entities/availability-groups/model/matrix";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import { SearchableSelect, type SearchableSelectOption } from "@shared/ui/components/SearchableSelect";
import { CloseIcon } from "@shared/ui/icons";
import { AvailabilityScheduleMatrix } from "./AvailabilityScheduleMatrix";
import styles from "./AvailabilityTransferDialog.module.css";

type AvailabilityTransferDialogProps = {
  open: boolean;
  employeeId: number;
  employeeName: string;
  year: number;
  month: number;
  sources: AvailabilityTransferSource[];
  isLoading: boolean;
  isPending: boolean;
  errorMessage?: string;
  onCancel: () => void;
  onConfirm: (sourceGroupId: number, dayOfMonths: number[]) => void;
};

export function AvailabilityTransferDialog({
  open,
  employeeId,
  employeeName,
  year,
  month,
  sources,
  isLoading,
  isPending,
  errorMessage,
  onCancel,
  onConfirm,
}: AvailabilityTransferDialogProps) {
  const titleId = useId();
  const descriptionId = useId();
  const [selectedSourceId, setSelectedSourceId] = useState<number | null>(null);
  const [selectedCellKeys, setSelectedCellKeys] = useState<string[]>([]);
  const selectedSource = sources.find(source => source.groupId === selectedSourceId) ?? sources[0] ?? null;

  useEffect(() => {
    if (!open) {
      return;
    }
    setSelectedSourceId(current => sources.some(source => source.groupId === current) ? current : sources[0]?.groupId ?? null);
  }, [open, sources]);

  useEffect(() => {
    setSelectedCellKeys([]);
  }, [selectedSource?.groupId]);

  useEffect(() => {
    if (!open) {
      return;
    }
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape" && !isPending) {
        onCancel();
      }
    };
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [isPending, onCancel, open]);

  const sourceOptions = useMemo<SearchableSelectOption[]>(() => sources.map(source => ({
    value: String(source.groupId),
    label: source.groupName,
    hint: t("{0} days available", source.days.filter(day => day.canTransfer).length),
    keywords: `${source.groupName} ${source.groupId}`,
  })), [sources]);
  const cellMap = useMemo<AvailabilityMatrixCellMap>(() => (selectedSource?.days ?? []).reduce<AvailabilityMatrixCellMap>(
    (result, day) => {
      result[getAvailabilityCellKey(employeeId, day.dayOfMonth)] = getAvailabilityCodeFromKind(day.kind, day.intervalStr);
      return result;
    },
    {},
  ), [employeeId, selectedSource?.days]);
  const transferableCellKeys = useMemo(() => new Set(
    (selectedSource?.days ?? [])
      .filter(day => day.canTransfer)
      .map(day => getAvailabilityCellKey(employeeId, day.dayOfMonth)),
  ), [employeeId, selectedSource?.days]);
  const dayOfMonths = selectedCellKeys
    .filter(key => transferableCellKeys.has(key))
    .map(key => Number(key.split(":")[1]))
    .filter(Number.isInteger)
    .sort((left, right) => left - right);

  if (!open) {
    return null;
  }

  const handleOverlayMouseDown = (event: MouseEvent<HTMLDivElement>) => {
    if (event.target === event.currentTarget && !isPending) {
      onCancel();
    }
  };

  return (
    <div
      className={styles.overlay}
      role="dialog"
      aria-modal="true"
      aria-labelledby={titleId}
      aria-describedby={descriptionId}
      onMouseDown={handleOverlayMouseDown}
    >
      <div className={styles.dialog}>
        <header className={styles.header}>
          <div className={styles.titleBlock}>

            <h3 id={titleId} className={styles.title}>{t("Move availability for")} {employeeName}</h3>
            <p id={descriptionId} className={styles.description}>
              {t("Select another availability, then click or drag across the days to stage. Nothing moves until Save Changes.")}</p>
          </div>
          <div className={styles.headerActions}>
            <div className={styles.sourceSelect}>
              <SearchableSelect
                id="availability-transfer-source"
                value={selectedSource ? String(selectedSource.groupId) : ""}
                options={sourceOptions}
                placeholder={isLoading ? t("Loading availability...") : t("Choose availability...")}
                dropdownTitle={t("Availability in this month")}
                searchPlaceholder={t("Search availability...")}
                emptyMessage={t("No other filled availability was found for this employee in this month.")}
                ariaLabel={t("source availability")}
                disabled={isLoading || isPending || sourceOptions.length === 0}
                onChange={value => setSelectedSourceId(value ? Number(value) : null)}
              />
            </div>
            <button type="button" className={styles.closeButton} aria-label={t("Close availability transfer")} onClick={onCancel} disabled={isPending}>
              <CloseIcon size={18} />
            </button>
          </div>
        </header>

        <div className={styles.controls}>

          <span className={styles.selectionSummary}>{dayOfMonths.length}  {t("day(s) selected")}</span>
        </div>

        {errorMessage ? <ErrorBanner className={styles.error}>{errorMessage}</ErrorBanner> : null}

        <div className={styles.matrixShell}>
          {selectedSource ? (
            <AvailabilityScheduleMatrix
              className={styles.matrix}
              year={year}
              month={month}
              columns={[{
                employeeId,
                memberId: selectedSource.memberId,
                label: employeeName,
              }]}
              cellMap={cellMap}
              readOnly
              enableSelectionWhenReadOnly
              selectedCellKeys={selectedCellKeys}
              onSelectedCellKeysChange={keys => setSelectedCellKeys(keys.filter(key => transferableCellKeys.has(key)))}
              title={selectedSource.groupName}
              helperText={t("Blue selection marks days staged for this draft. Unavailable or already transferred days cannot be selected.")}
            />
          ) : (
            <div className={styles.empty}>{t("No source availability is available for this employee and month.")}</div>
          )}
        </div>

        <footer className={styles.footer}>
          <IosButton label={t("Cancel")} variant="secondary" onClick={onCancel} disabled={isPending} />
          <IosButton
            label={isPending ? t("Applying...") : t("Add {0} day{1} to draft", dayOfMonths.length || "", dayOfMonths.length === 1 ? "" : "s").trim()}
            onClick={() => selectedSource && onConfirm(selectedSource.groupId, dayOfMonths)}
            disabled={!selectedSource || dayOfMonths.length === 0 || isPending}
          />
        </footer>
      </div>
    </div>
  );
}
