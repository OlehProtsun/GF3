import type {
  AvailabilityGroup,
  AvailabilityGroupMember,
  AvailabilityKind,
  AvailabilitySlot,
} from "./types";

const longMonthFormatter = new Intl.DateTimeFormat("en-US", { month: "long" });
const shortMonthFormatter = new Intl.DateTimeFormat("en-US", { month: "short" });

export const availabilityMonthOptions = Array.from({ length: 12 }, (_, index) => {
  const month = index + 1;

  return {
    value: month,
    label: longMonthFormatter.format(new Date(Date.UTC(2026, index, 1))),
    shortLabel: shortMonthFormatter.format(new Date(Date.UTC(2026, index, 1))),
  };
});

export function getAvailabilityMonthLabel(month: number, format: "long" | "short" = "long") {
  const option = availabilityMonthOptions.find(item => item.value === month);
  if (!option) {
    return `Month ${month}`;
  }

  return format === "short" ? option.shortLabel : option.label;
}

export function getAvailabilityGroupPeriodLabel(
  group?: Pick<AvailabilityGroup, "month" | "year"> | null,
  format: "long" | "compact" = "long"
) {
  if (!group) {
    return "Unknown period";
  }

  const monthLabel = getAvailabilityMonthLabel(group.month, format === "compact" ? "short" : "long");
  return `${monthLabel} ${group.year}`;
}

export function filterAvailabilityGroups(groups: AvailabilityGroup[], query: string) {
  const normalizedQuery = query.trim().toLowerCase();
  if (!normalizedQuery) {
    return groups;
  }

  return groups.filter(group => {
    const haystack = [
      group.name,
      String(group.id),
      String(group.year),
      String(group.month),
      getAvailabilityMonthLabel(group.month, "long"),
      getAvailabilityMonthLabel(group.month, "short"),
      getAvailabilityGroupPeriodLabel(group),
    ]
      .join(" ")
      .toLowerCase();

    return haystack.includes(normalizedQuery);
  });
}

export function sortAvailabilityGroups(groups: AvailabilityGroup[]) {
  return [...groups].sort((left, right) => {
    if (left.year !== right.year) {
      return right.year - left.year;
    }

    if (left.month !== right.month) {
      return right.month - left.month;
    }

    return left.name.localeCompare(right.name);
  });
}

function normalizeKind(kind: AvailabilityKind) {
  if (typeof kind === "number") {
    if (kind === 0) return "ANY";
    if (kind === 1) return "NONE";
    if (kind === 2) return "INT";
    return String(kind);
  }

  return kind.toUpperCase();
}

export function getAvailabilityKindLabel(kind: AvailabilityKind) {
  switch (normalizeKind(kind)) {
    case "ANY":
    case "AVAILABLE":
      return "Any shift";
    case "NONE":
    case "UNAVAILABLE":
      return "Unavailable";
    case "INT":
    case "PREFERRED":
      return "Custom interval";
    default:
      return "Unknown";
  }
}

export function summarizeAvailabilitySlots(slots: AvailabilitySlot[]) {
  return slots.reduce(
    (summary, slot) => {
      const kind = normalizeKind(slot.kind);

      if (kind === "ANY" || kind === "AVAILABLE") {
        summary.any += 1;
      } else if (kind === "NONE" || kind === "UNAVAILABLE") {
        summary.none += 1;
      } else if (kind === "INT" || kind === "PREFERRED") {
        summary.interval += 1;
      } else {
        summary.other += 1;
      }

      return summary;
    },
    { any: 0, none: 0, interval: 0, other: 0 }
  );
}

export function getAvailabilityMemberNames(
  members: AvailabilityGroupMember[],
  employeeNameById: Map<number, string>
) {
  return members
    .map(member => employeeNameById.get(member.employeeId) ?? `Employee #${member.employeeId}`)
    .sort((left, right) => left.localeCompare(right));
}
