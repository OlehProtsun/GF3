import { AVAILABILITY_NONE_MARK, getAvailabilityCellKey, getAvailabilityCodeFromKind } from "./matrix";
import type { AvailabilityMatrixCellMap } from "./matrix";
import type { AvailabilityKind } from "./types";

export type AvailabilityTransferSourceDay = {
  dayOfMonth: number;
  kind: AvailabilityKind;
  intervalStr?: string | null;
  canTransfer: boolean;
};

export type AvailabilityTransferSource = {
  groupId: number;
  groupName: string;
  memberId: number;
  employeeId: number;
  days: AvailabilityTransferSourceDay[];
};

export type AvailabilityTransferHint = {
  employeeId: number;
  dayOfMonth: number;
  targetGroupId: number;
  targetGroupName: string;
  kind: AvailabilityKind;
  intervalStr?: string | null;
};

export type AvailabilityTransferResult = {
  sourceGroupId: number;
  sourceGroupName: string;
  targetGroupId: number;
  targetGroupName: string;
  employeeId: number;
  dayOfMonths: number[];
};

export type StagedAvailabilityTransfer = {
  employeeId: number;
  sourceGroupId: number;
  dayOfMonths: number[];
};

export type AvailabilityTransferSourceHintDetail = {
  employeeId: number;
  dayOfMonth: number;
  sourceGroupId: number;
  sourceGroupName: string;
};

export type AvailabilityTransferSourceHintData = {
  visualHintMap: AvailabilityMatrixCellMap;
  detailMap: Record<string, AvailabilityTransferSourceHintDetail>;
};

export function stageAvailabilityTransfer({
  cellMap,
  transfers,
  sources,
  employeeId,
  sourceGroupId,
  dayOfMonths,
}: {
  cellMap: AvailabilityMatrixCellMap;
  transfers: StagedAvailabilityTransfer[];
  sources: AvailabilityTransferSource[];
  employeeId: number;
  sourceGroupId: number;
  dayOfMonths: number[];
}) {
  const source = sources.find(item => item.employeeId === employeeId && item.groupId === sourceGroupId);
  if (!source) {
    return { cellMap, transfers };
  }

  const requestedDays = new Set(dayOfMonths);
  const selectedDays = source.days
    .filter(day => day.canTransfer && requestedDays.has(day.dayOfMonth))
    .sort((left, right) => left.dayOfMonth - right.dayOfMonth);
  if (selectedDays.length === 0) {
    return { cellMap, transfers };
  }

  const selectedDayNumbers = new Set(selectedDays.map(day => day.dayOfMonth));
  const nextCellMap = { ...cellMap };
  selectedDays.forEach(day => {
    nextCellMap[getAvailabilityCellKey(employeeId, day.dayOfMonth)] =
      getAvailabilityCodeFromKind(day.kind, day.intervalStr);
  });

  const remainingTransfers = transfers
    .map(transfer => transfer.employeeId !== employeeId
      ? transfer
      : {
          ...transfer,
          dayOfMonths: transfer.dayOfMonths.filter(day => !selectedDayNumbers.has(day)),
        })
    .filter(transfer => transfer.dayOfMonths.length > 0);
  const existingSourceDays = transfers
    .filter(transfer => transfer.employeeId === employeeId && transfer.sourceGroupId === sourceGroupId)
    .flatMap(transfer => transfer.dayOfMonths);
  const nextTransfer: StagedAvailabilityTransfer = {
    employeeId,
    sourceGroupId,
    dayOfMonths: [...new Set([...existingSourceDays, ...selectedDayNumbers])].sort((left, right) => left - right),
  };

  return {
    cellMap: nextCellMap,
    transfers: [...remainingTransfers, nextTransfer],
  };
}

export function removeStagedAvailabilityTransferDay(
  transfers: StagedAvailabilityTransfer[],
  employeeId: number,
  dayOfMonth: number,
) {
  return transfers
    .map(transfer => transfer.employeeId !== employeeId
      ? transfer
      : {
          ...transfer,
          dayOfMonths: transfer.dayOfMonths.filter(day => day !== dayOfMonth),
        })
    .filter(transfer => transfer.dayOfMonths.length > 0);
}

export function buildAvailabilityTransferSourceHintMap(
  sources: AvailabilityTransferSource[],
): AvailabilityMatrixCellMap {
  return buildAvailabilityTransferSourceHintData(sources).visualHintMap;
}

export function buildAvailabilityTransferSourceHintData(
  sources: AvailabilityTransferSource[],
): AvailabilityTransferSourceHintData {
  return sources.reduce<AvailabilityTransferSourceHintData>((result, source) => {
    const sourceName = source.groupName.trim();
    if (!sourceName) {
      return result;
    }

    source.days.forEach(day => {
      const code = getAvailabilityCodeFromKind(day.kind, day.intervalStr);
      const cellKey = getAvailabilityCellKey(source.employeeId, day.dayOfMonth);
      if (code !== AVAILABILITY_NONE_MARK && !result.visualHintMap[cellKey]) {
        result.visualHintMap[cellKey] = `${code} (${sourceName})`;
        result.detailMap[cellKey] = {
          employeeId: source.employeeId,
          dayOfMonth: day.dayOfMonth,
          sourceGroupId: source.groupId,
          sourceGroupName: sourceName,
        };
      }
    });
    return result;
  }, { visualHintMap: {}, detailMap: {} });
}

export function buildAvailabilityTransferHintMap(
  hints: AvailabilityTransferHint[],
): AvailabilityMatrixCellMap {
  return hints.reduce<AvailabilityMatrixCellMap>((result, hint) => {
    const code = getAvailabilityCodeFromKind(hint.kind, hint.intervalStr);
    const targetName = hint.targetGroupName.trim();
    if (code === AVAILABILITY_NONE_MARK || !targetName) {
      return result;
    }

    result[getAvailabilityCellKey(hint.employeeId, hint.dayOfMonth)] = `${code} (${targetName})`;
    return result;
  }, {});
}

export function buildAvailabilityProfileHintMap(
  sources: AvailabilityTransferSource[],
  transferredAwayHints: AvailabilityTransferHint[],
): AvailabilityMatrixCellMap {
  return {
    ...buildAvailabilityTransferSourceHintMap(sources),
    ...buildAvailabilityTransferHintMap(transferredAwayHints),
  };
}
