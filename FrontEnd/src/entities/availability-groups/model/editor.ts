import type { Employee } from "@entities/employees/model/types";
import { getEmployeeFullName } from "@entities/employees/model/presentation";
import { clampAvailabilityMonth, clampAvailabilityYear } from "./matrix";

export * from "./matrix";

export function filterAvailabilityEmployees(employees: Employee[], searchText: string) {
  const normalizedSearchText = searchText.trim().toLowerCase();
  if (!normalizedSearchText) {
    return [...employees].sort((left, right) => getEmployeeFullName(left).localeCompare(getEmployeeFullName(right)));
  }

  return employees
    .filter(employee => {
      const haystack = [
        String(employee.id),
        employee.firstName,
        employee.lastName,
        employee.email ?? "",
        employee.phone ?? "",
        getEmployeeFullName(employee),
      ]
        .join(" ")
        .toLowerCase();

      return haystack.includes(normalizedSearchText);
    })
    .sort((left, right) => getEmployeeFullName(left).localeCompare(getEmployeeFullName(right)));
}

export function getAvailabilityGroupNameForSave(name: string, month: number, year: number, isCreate: boolean) {
  const trimmedName = name.trim();
  if (!trimmedName) {
    return "";
  }

  if (!isCreate) {
    return trimmedName;
  }

  const suffix = `${String(clampAvailabilityMonth(month)).padStart(2, "0")}.${clampAvailabilityYear(year)}`;
  if (trimmedName.endsWith(suffix)) {
    return trimmedName;
  }

  return `${trimmedName} : ${suffix}`;
}
