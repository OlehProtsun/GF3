import type { ShiftSwap } from "./types";

function normalizeSearchValue(value: string) {
  return value.trim().toLocaleLowerCase();
}

function formatDateSearchValues(date: Date) {
  const day = String(date.getUTCDate()).padStart(2, "0");
  const month = String(date.getUTCMonth() + 1).padStart(2, "0");
  const year = date.getUTCFullYear();
  const monthName = new Intl.DateTimeFormat("en-GB", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    timeZone: "UTC",
  }).format(date);

  return [
    `${year}-${month}-${day}`,
    `${day}/${month}/${year}`,
    `${day}.${month}.${year}`,
    monthName,
  ];
}

function buildDateSearchValues(item: ShiftSwap) {
  const shiftDate = new Date(Date.UTC(item.year, item.month - 1, item.dayOfMonth));
  const acceptedDate = item.acceptedAtUtc ? new Date(item.acceptedAtUtc) : null;

  return [
    ...formatDateSearchValues(shiftDate),
    ...(acceptedDate && !Number.isNaN(acceptedDate.getTime()) ? formatDateSearchValues(acceptedDate) : []),
  ];
}

export function filterAcceptedShiftSwapHistory(items: ShiftSwap[], query: string) {
  const terms = normalizeSearchValue(query).split(/\s+/).filter(Boolean);
  if (terms.length === 0) {
    return items;
  }

  return items.filter(item => {
    const searchableValue = normalizeSearchValue([
      item.fromEmployeeName,
      item.acceptedByEmployeeName ?? "",
      item.targetEmployeeName ?? "",
      item.scheduleName,
      item.containerName,
      item.shopName,
      item.acceptedAtUtc ?? "",
      ...buildDateSearchValues(item),
    ].join(" "));

    return terms.every(term => searchableValue.includes(term));
  });
}
