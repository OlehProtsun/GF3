import { useMemo, type CSSProperties, type ReactNode } from "react";
import type { GraphMatrixColumn } from "@entities/containers";
import { ContainerGraphMatrix } from "@entities/containers";
import {
  AVAILABILITY_ANY_MARK,
  AVAILABILITY_NONE_MARK,
  normalizeAvailabilityCellValue,
  parseAvailabilityCode,
  type AvailabilityMatrixCellMap,
  type AvailabilityMatrixColumn,
} from "@entities/availability-groups/model/editor";
import { getAvailabilityMemberLastModifiedLabel } from "@entities/availability-groups/model/presentation";
import { AvailabilityIcon } from "@shared/ui/icons";

type AvailabilityScheduleMatrixProps = {
  year: number;
  month: number;
  columns: AvailabilityMatrixColumn[];
  cellMap: AvailabilityMatrixCellMap;
  visualHintMap?: AvailabilityMatrixCellMap;
  title?: string;
  helperText?: string;
  readOnly?: boolean;
  emptyMessage?: string;
  cellErrors?: Record<string, string>;
  className?: string;
  style?: CSSProperties;
  compactSize?: boolean;
  headerCenterSlot?: ReactNode;
  headerRightSlot?: ReactNode;
  bindValueByKey?: ReadonlyMap<string, string>;
  selectedCellKeys?: string[];
  enableSelectionWhenReadOnly?: boolean;
  onColumnMove?: (employeeId: number, targetEmployeeId: number) => void;
  onSelectedCellKeysChange?: (keys: string[]) => void;
  onCellChange?: (employeeId: number, dayOfMonth: number, value: string) => void;
  onVisualHintClick?: (employeeId: number, dayOfMonth: number) => void;
};

export function AvailabilityScheduleMatrix({
  year,
  month,
  columns,
  cellMap,
  visualHintMap,
  title = "Availability Schedule",
  helperText = `Use ${AVAILABILITY_ANY_MARK} for any shift, ${AVAILABILITY_NONE_MARK} for unavailable, a time interval like 08:00 - 16:00, or any text note. Text notes stay visible but are treated as unavailable during schedule generation.`,
  readOnly = false,
  emptyMessage,
  cellErrors = {},
  className,
  style,
  compactSize = false,
  headerCenterSlot,
  headerRightSlot,
  bindValueByKey,
  selectedCellKeys,
  enableSelectionWhenReadOnly,
  onColumnMove,
  onSelectedCellKeysChange,
  onCellChange,
  onVisualHintClick,
}: AvailabilityScheduleMatrixProps) {
  const graphColumns = useMemo<GraphMatrixColumn[]>(
    () =>
      columns.map(column => ({
        employeeId: column.employeeId,
        kind: "employee",
        manualColumnId: null,
        graphEmployeeId: column.memberId ?? null,
        label: column.label,
        minHoursMonth: null,
        totalMinutes: 0,
        totalText: getAvailabilityMemberLastModifiedLabel(column.employeeLastModifiedAtUtc),
      })),
    [columns],
  );
  const normalizedBindValueByKey = useMemo(() => {
    if (!bindValueByKey || bindValueByKey.size === 0) {
      return bindValueByKey;
    }

    return new Map(
      Array.from(bindValueByKey, ([bindKey, bindValue]) => {
        const parsedValue = parseAvailabilityCode(bindValue);
        return [bindKey, parsedValue.ok ? parsedValue.value.normalizedCode : bindValue];
      }),
    );
  }, [bindValueByKey]);

  return (
    <ContainerGraphMatrix
      className={className}
      style={style}
      title={title}
      icon={<AvailabilityIcon size={18} />}
      helperText={helperText}
      graph={{ year, month }}
      columns={graphColumns}
      cellMap={cellMap}
      visualHintMap={visualHintMap}
      lockVisualHintCells
      readOnly={readOnly}
      emptyMessage={
        emptyMessage ??
        (readOnly
          ? "No employees are assigned to this availability group yet."
          : "Add at least one employee to start filling the schedule.")
      }
      compactSize={compactSize}
      preserveShellHeightOnCompact
      editMode="inline"
      emptyCellVariant="danger"
      cellErrors={cellErrors}
      headerCenterSlot={headerCenterSlot}
      headerRightSlot={headerRightSlot}
      bindValueByKey={normalizedBindValueByKey}
      selectedCellKeys={selectedCellKeys}
      enableSelectionWhenReadOnly={enableSelectionWhenReadOnly}
      normalizeCellValue={(_, value) => normalizeAvailabilityCellValue(value)}
      onColumnMove={onColumnMove}
      onSelectedCellKeysChange={onSelectedCellKeysChange}
      onCellChange={onCellChange}
      onVisualHintCellClick={onVisualHintClick}
    />
  );
}
