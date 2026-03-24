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
import { AvailabilityIcon } from "@shared/ui/icons";

type AvailabilityScheduleMatrixProps = {
  year: number;
  month: number;
  columns: AvailabilityMatrixColumn[];
  cellMap: AvailabilityMatrixCellMap;
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
  onColumnMove?: (employeeId: number, targetEmployeeId: number) => void;
  onCellChange?: (employeeId: number, dayOfMonth: number, value: string) => void;
};

export function AvailabilityScheduleMatrix({
  year,
  month,
  columns,
  cellMap,
  title = "Availability Schedule",
  helperText = `Use ${AVAILABILITY_ANY_MARK} for any shift, ${AVAILABILITY_NONE_MARK} for unavailable, or a time interval like 08:00 - 16:00.`,
  readOnly = false,
  emptyMessage,
  cellErrors = {},
  className,
  style,
  compactSize = false,
  headerCenterSlot,
  headerRightSlot,
  bindValueByKey,
  onColumnMove,
  onCellChange,
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
        totalText: "",
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
      icon={<AvailabilityIcon size={18} style={{ transform: "scaleY(-1)" }} />}
      helperText={helperText}
      graph={{ year, month }}
      columns={graphColumns}
      cellMap={cellMap}
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
      normalizeCellValue={(_, value) => normalizeAvailabilityCellValue(value)}
      onColumnMove={onColumnMove}
      onCellChange={onCellChange}
    />
  );
}
