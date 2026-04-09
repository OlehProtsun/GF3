import { useMemo } from "react";
import type { ChangeEvent } from "react";
import { useSyncedDraft } from "@shared/lib/useSyncedDraft";
import type { Employee } from "./types";

export type EmployeeFormState = {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
};

export type EmployeeFormErrors = Partial<Record<keyof EmployeeFormState, string>>;

type EmployeeFormSource = Pick<Employee, "firstName" | "lastName" | "email" | "phone">;

const EMPTY_FORM: EmployeeFormState = {
  firstName: "",
  lastName: "",
  email: "",
  phone: "",
};

export function createEmployeeFormState(employee?: EmployeeFormSource | null): EmployeeFormState {
  if (!employee) {
    return EMPTY_FORM;
  }

  return {
    firstName: employee.firstName,
    lastName: employee.lastName,
    email: employee.email ?? "",
    phone: employee.phone ?? "",
  };
}

export function validateEmployeeForm(form: EmployeeFormState): EmployeeFormErrors {
  const nextErrors: EmployeeFormErrors = {};

  if (!form.firstName.trim()) {
    nextErrors.firstName = "First name is required";
  }

  if (!form.lastName.trim()) {
    nextErrors.lastName = "Last name is required";
  }

  if (form.email && !/^\S+@\S+\.\S+$/.test(form.email)) {
    nextErrors.email = "Invalid email format";
  }

  return nextErrors;
}

export function useEmployeeForm(employee?: EmployeeFormSource | null, isCreate = false) {
  const sourceKey = useMemo(
    () =>
      employee
        ? `employee:${employee.firstName}:${employee.lastName}:${employee.email ?? ""}:${employee.phone ?? ""}`
        : isCreate
          ? "create"
          : "empty",
    [employee, isCreate],
  );
  const initialDraft = useMemo(
    () => ({
      form: createEmployeeFormState(employee),
      errors: {} as EmployeeFormErrors,
    }),
    [employee],
  );
  const { value: draft, setValue: setDraft } = useSyncedDraft(sourceKey, initialDraft);
  const { form, errors } = draft;

  const handleFieldChange =
    (field: keyof EmployeeFormState) =>
    (event: ChangeEvent<HTMLInputElement>): void => {
      setDraft((current) => ({
        form: { ...current.form, [field]: event.target.value },
        errors: current.errors,
      }));
    };

  const validate = () => {
    const nextErrors = validateEmployeeForm(form);

    setDraft((current) => ({
      ...current,
      errors: nextErrors,
    }));

    return Object.keys(nextErrors).length === 0;
  };

  return {
    form,
    errors,
    handleFieldChange,
    validate,
  };
}
