import { dateTimeFormat, t } from "@shared/i18n";
import type { Container, Graph } from "./types";

type ContainerIdentity = Pick<Container, "name">;
type ContainerDetails = Pick<Container, "note">;

export function getContainerDisplayName(container?: ContainerIdentity | null, fallback = t("Container")) {
  const name = container?.name?.trim();
  return name || fallback;
}

export function getContainerInitials(container?: ContainerIdentity | null, fallback = "CT") {
  const initials =
    container?.name
      ?.trim()
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map(part => part[0])
      .join("")
      .toUpperCase() ?? "";

  return initials || fallback;
}

export function getContainerState(scheduleCount: number, totalHoursText: string, note?: string | null) {
  if (scheduleCount > 0 && note?.trim()) {
    return t("{0} schedules tracked, note available", scheduleCount);
  }

  if (scheduleCount > 0) {
    return t("{0} schedules tracked, {1} assigned", scheduleCount, totalHoursText);
  }

  if (note?.trim()) {
    return t("Profile note available");
  }

  return t("No schedules yet");
}

export function getContainerProfileDetails(
  container?: ContainerDetails | null,
  metrics?: {
    scheduleCount: number;
    totalEmployees: number;
    totalShops: number;
    totalHoursText: string;
  },
) {
  return [
    {
      key: "note",
      label: t("Note"),
      value: container?.note?.trim() || null,
    },
    {
      key: "schedules",
      label: t("Schedules"),
      value: metrics ? String(metrics.scheduleCount) : null,
    },
    {
      key: "employees",
      label: t("Employees"),
      value: metrics ? String(metrics.totalEmployees) : null,
    },
    {
      key: "shops",
      label: t("Shops"),
      value: metrics ? String(metrics.totalShops) : null,
    },
    {
      key: "hours",
      label: t("Total hours"),
      value: metrics?.totalHoursText ?? null,
    },
  ] as const;
}

export function getGraphMonthYearLabel(graph: Pick<Graph, "year" | "month">) {
  const monthIndex = Math.min(Math.max(graph.month - 1, 0), 11);
  const date = new Date(Date.UTC(graph.year, monthIndex, 1));

  return dateTimeFormat("en-US", {
    month: "long",
    year: "numeric",
    timeZone: "UTC",
  }).format(date);
}

export function matchesContainerSearch(container: Container, query: string) {
  const normalizedQuery = query.trim().toLowerCase();
  if (!normalizedQuery) {
    return true;
  }

  return [container.name, container.note ?? ""]
    .join(" ")
    .toLowerCase()
    .includes(normalizedQuery);
}
