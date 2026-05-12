import type { WorkflowLog } from "@entities/workflow-logs";

export type WorkflowLogDay = {
  key: string;
  label: string;
  fullLabel: string;
  logs: WorkflowLog[];
  sortValue: number;
};

const dayTabFormatter = new Intl.DateTimeFormat("en-US", {
  day: "2-digit",
  month: "short",
});

const fullDayFormatter = new Intl.DateTimeFormat("en-US", {
  weekday: "long",
  day: "2-digit",
  month: "long",
  year: "numeric",
});

const logTimeFormatter = new Intl.DateTimeFormat("en-US", {
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

export function formatWorkflowLogTime(value: string) {
  const date = parseWorkflowLogDate(value);
  if (!date) {
    return value;
  }

  return logTimeFormatter.format(date);
}

export function getRoleLabel(role: string) {
  return role.toLowerCase() === "employee" ? "Employee" : "Manager";
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
      label: date ? dayTabFormatter.format(date) : "Unknown",
      fullLabel: date ? fullDayFormatter.format(date) : "Unknown date",
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
