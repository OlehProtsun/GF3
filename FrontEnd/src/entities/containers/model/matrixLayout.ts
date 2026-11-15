import type { GraphMatrixCellMap, GraphMatrixColumn } from "./graphWorkspace";

export const MATRIX_COLUMN_MIN_WIDTH_PX = 135;
export const MATRIX_COLUMN_MAX_AUTO_WIDTH_PX = 420;

const MATRIX_COLUMN_CHARACTER_WIDTH_PX = 7.25;
const MATRIX_COLUMN_HORIZONTAL_CHROME_PX = 34;

function getLongestLineLength(value: string | null | undefined) {
  return (value ?? "")
    .split(/\r?\n/)
    .reduce((longest, line) => Math.max(longest, line.trim().length), 0);
}

export function estimateMatrixColumnTextWidth(value: string | null | undefined) {
  const contentWidth = Math.ceil(
    (getLongestLineLength(value) * MATRIX_COLUMN_CHARACTER_WIDTH_PX) + MATRIX_COLUMN_HORIZONTAL_CHROME_PX,
  );

  return Math.min(
    MATRIX_COLUMN_MAX_AUTO_WIDTH_PX,
    Math.max(MATRIX_COLUMN_MIN_WIDTH_PX, contentWidth),
  );
}

export function buildMatrixAutoColumnWidths({
  columns,
  cellMap,
  visualHintMap = {},
}: {
  columns: GraphMatrixColumn[];
  cellMap: GraphMatrixCellMap;
  visualHintMap?: GraphMatrixCellMap;
}) {
  const activeEmployeeIds = new Set(columns.map(column => column.employeeId));
  const widthByEmployeeId = columns.reduce<Record<number, number>>((result, column) => {
    result[column.employeeId] = Math.max(
      estimateMatrixColumnTextWidth(column.label),
      estimateMatrixColumnTextWidth(column.totalText),
    );
    return result;
  }, {});

  const includeCellMap = (values: GraphMatrixCellMap) => {
    Object.entries(values).forEach(([cellKey, value]) => {
      const employeeId = Number(cellKey.split(":", 1)[0]);
      if (!activeEmployeeIds.has(employeeId)) {
        return;
      }

      widthByEmployeeId[employeeId] = Math.max(
        widthByEmployeeId[employeeId] ?? MATRIX_COLUMN_MIN_WIDTH_PX,
        estimateMatrixColumnTextWidth(value),
      );
    });
  };

  includeCellMap(cellMap);
  includeCellMap(Object.entries(visualHintMap).reduce<GraphMatrixCellMap>((result, [cellKey, visualHint]) => {
    const currentValue = cellMap[cellKey]?.trim() ?? "";
    result[cellKey] = currentValue && currentValue !== "-"
      ? `${currentValue}, ${visualHint}`
      : visualHint;
    return result;
  }, {}));

  return widthByEmployeeId;
}
