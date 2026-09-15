import { dateTimeFormat } from "@shared/i18n";
import { t } from "@shared/i18n";
import type { WorkflowLog } from "@entities/workflow-logs";

export type WorkflowLogDay = {
  key: string;
  label: string;
  fullLabel: string;
  logs: WorkflowLog[];
  sortValue: number;
};

const dayTabFormatter = dateTimeFormat("en-US", {
  day: "2-digit",
  month: "short",
});

const fullDayFormatter = dateTimeFormat("en-US", {
  weekday: "long",
  day: "2-digit",
  month: "long",
  year: "numeric",
});

const logTimeFormatter = dateTimeFormat("en-US", {
  hour: "2-digit",
  minute: "2-digit",
  second: "2-digit",
  hour12: false,
});

export function parseWorkflowLogDate(value: string) {
  const date = new Date(value);

  return Number.isNaN(date.getTime()) ? null : date;
}

export function getWorkflowDayKey(date: Date | null) {
  if (!date) {
    return "unknown";
  }

  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");

  return `${year}-${month}-${day}`;
}

function parseWorkflowDayKey(value: string) {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (!match) {
    return null;
  }

  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  const date = new Date(year, month - 1, day);

  return date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day ? date : null;
}

export function getWorkflowLogRangeBounds(fromDayKey: string, toDayKey: string) {
  const from = parseWorkflowDayKey(fromDayKey);
  const lastDay = parseWorkflowDayKey(toDayKey);
  if (!from || !lastDay || lastDay < from) {
    return null;
  }

  const to = new Date(lastDay.getFullYear(), lastDay.getMonth(), lastDay.getDate() + 1);
  return { fromUtc: from.toISOString(), toUtc: to.toISOString() };
}

export function getWorkflowLogDayBounds(dayKey: string) {
  return getWorkflowLogRangeBounds(dayKey, dayKey);
}

export function formatWorkflowLogTime(value: string) {
  const date = parseWorkflowLogDate(value);
  if (!date) {
    return value;
  }

  return logTimeFormatter.format(date);
}

export function getRoleLabel(role: string) {
  return role.toLowerCase() === "employee" ? t("Employee") : t("Manager");
}

export function groupWorkflowLogsByDay(logs: WorkflowLog[]) {
  const groups = new Map<string, WorkflowLogDay>();

  logs.forEach(log => {
    const date = parseWorkflowLogDate(log.occurredAtUtc);
    const key = getWorkflowDayKey(date);
    const existing = groups.get(key);

    if (existing) {
      existing.logs.push(log);
      return;
    }

    groups.set(key, {
      key,
      label: date ? dayTabFormatter.format(date) : t("Unknown"),
      fullLabel: date ? fullDayFormatter.format(date) : t("Unknown date"),
      logs: [log],
      sortValue: date ? new Date(date.getFullYear(), date.getMonth(), date.getDate()).getTime() : 0,
    });
  });

  return [...groups.values()]
    .map(group => ({
      ...group,
      logs: [...group.logs].sort(
        (left, right) =>
          (parseWorkflowLogDate(right.occurredAtUtc)?.getTime() ?? 0) -
          (parseWorkflowLogDate(left.occurredAtUtc)?.getTime() ?? 0),
      ),
    }))
    .sort((left, right) => right.sortValue - left.sortValue);
}
