import { dateTimeFormat } from "@shared/i18n";
import { t } from "@shared/i18n";
import type { Employee } from "./types";

type EmployeeIdentity = Pick<Employee, "firstName" | "lastName">;
type EmployeeContact = Pick<
  Employee,
  "email" | "phone" | "username" | "hasLoginAccount" | "isOnline" | "lastLoginAtUtc"
>;
export type EmployeePresenceTone = "online" | "offline" | "inactive";
type PresenceLabelMode = "default" | "compact";

const employeeLastLoginFormatter = dateTimeFormat("en-GB", {
  day: "2-digit",
  month: "short",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
  hour12: false,
});

export function getEmployeeFullName(employee?: EmployeeIdentity | null, fallback = t("Employee")) {
  const fullName = [employee?.firstName, employee?.lastName].filter(Boolean).join(" ").trim();
  return fullName || fallback;
}

export function getEmployeeInitials(employee?: EmployeeIdentity | null, fallback = "EM") {
  const initials = `${employee?.firstName?.[0] ?? ""}${employee?.lastName?.[0] ?? ""}`.trim().toUpperCase();
  return initials || fallback;
}

export function getEmployeePresenceTone(employee?: EmployeeContact | null): EmployeePresenceTone {
  if (employee?.hasLoginAccount) {
    return employee.isOnline ? "online" : "offline";
  }

  return "inactive";
}

export function getEmployeeContactState(
  employee?: EmployeeContact | null,
  mode: PresenceLabelMode = "default",
) {
  switch (getEmployeePresenceTone(employee)) {
    case "online":
      return mode === "compact" ? t("Online") : t("Online now");
    case "offline":
      return t("Offline");
    default:
      return mode === "compact" ? t("No login") : t("No login account");
  }
}

export function formatEmployeeLastLogin(lastLoginAtUtc?: string | null) {
  if (!lastLoginAtUtc) {
    return t("Never");
  }

  const parsedValue = new Date(lastLoginAtUtc);
  if (Number.isNaN(parsedValue.getTime())) {
    return lastLoginAtUtc;
  }

  return employeeLastLoginFormatter.format(parsedValue).replace(",", "");
}

export function getEmployeeContactDetails(employee?: EmployeeContact | null) {
  return [
    {
      key: "last-login",
      label: t("Last Login"),
      value: formatEmployeeLastLogin(employee?.lastLoginAtUtc),
      href: undefined,
    },
    {
      key: "username",
      label: t("Username"),
      value: employee?.username ?? null,
      href: undefined,
    },
    {
      key: "email",
      label: t("Recovery Email"),
      value: employee?.email ?? null,
      href: employee?.email ? `mailto:${employee.email}` : undefined,
    },
    {
      key: "phone",
      label: t("Phone"),
      value: employee?.phone ?? null,
      href: employee?.phone ? `tel:${employee.phone}` : undefined,
    },
  ] as const;
}
