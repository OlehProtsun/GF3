import type { Employee } from "./types";

type EmployeeIdentity = Pick<Employee, "firstName" | "lastName">;
type EmployeeContact = Pick<Employee, "email" | "phone">;

export function getEmployeeFullName(employee?: EmployeeIdentity | null, fallback = "Employee") {
  const fullName = [employee?.firstName, employee?.lastName].filter(Boolean).join(" ").trim();
  return fullName || fallback;
}

export function getEmployeeInitials(employee?: EmployeeIdentity | null, fallback = "EM") {
  const initials = `${employee?.firstName?.[0] ?? ""}${employee?.lastName?.[0] ?? ""}`.trim().toUpperCase();
  return initials || fallback;
}

export function getEmployeeContactState(employee?: EmployeeContact | null) {
  if (employee?.email && employee.phone) {
    return "Fully reachable";
  }

  if (employee?.email || employee?.phone) {
    return "Partial contact data";
  }

  return "Contact details missing";
}

export function getEmployeeContactDetails(employee?: EmployeeContact | null) {
  return [
    {
      key: "email",
      label: "Email",
      value: employee?.email ?? null,
      href: employee?.email ? `mailto:${employee.email}` : undefined,
    },
    {
      key: "phone",
      label: "Phone",
      value: employee?.phone ?? null,
      href: employee?.phone ? `tel:${employee.phone}` : undefined,
    },
  ] as const;
}
