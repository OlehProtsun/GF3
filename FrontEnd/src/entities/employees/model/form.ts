import { useMemo } from "react";
import type { ChangeEvent } from "react";
import { useSyncedDraft } from "@shared/lib/useSyncedDraft";
import type { Employee } from "./types";

export type EmployeeFormState = {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  username: string;
  password: string;
};

export type EmployeeFormErrors = Partial<Record<keyof EmployeeFormState, string>>;

type EmployeeFormSource = Pick<Employee, "firstName" | "lastName" | "email" | "phone" | "username" | "hasLoginAccount">;

const EMPTY_FORM: EmployeeFormState = {
  firstName: "",
  lastName: "",
  email: "",
  phone: "",
  username: "",
  password: "",
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
    username: employee.username ?? "",
    password: "",
  };
}

export function validateEmployeeForm(
  form: EmployeeFormState,
  options: {
    isCreate: boolean;
    hasLoginAccount: boolean;
  },
): EmployeeFormErrors {
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

  const normalizedUsername = form.username.trim();
  const normalizedPassword = form.password.trim();

  if (normalizedUsername && normalizedUsername.length < 3) {
    nextErrors.username = "Username must be at least 3 characters long";
  }

  if (normalizedUsername && !/^[A-Za-z0-9._-]+$/.test(normalizedUsername)) {
    nextErrors.username = "Username may use letters, numbers, dots, underscores, and dashes";
  }

  if (!normalizedUsername && normalizedPassword) {
    nextErrors.username = "Username is required when setting a password";
  }

  if (options.hasLoginAccount && !normalizedUsername) {
    nextErrors.username = "Username is required for employees with an existing login";
  }

  if ((options.isCreate || !options.hasLoginAccount) && normalizedUsername && !normalizedPassword) {
    nextErrors.password = "Password is required when creating a login";
  }

  if (normalizedPassword && !/^\d{6}$/.test(normalizedPassword)) {
    nextErrors.password = "Password must contain exactly 6 digits.";
  }

  return nextErrors;
}

export function useEmployeeForm(employee?: EmployeeFormSource | null, isCreate = false) {
  const sourceKey = useMemo(
    () =>
      employee
        ? `employee:${employee.firstName}:${employee.lastName}:${employee.email ?? ""}:${employee.phone ?? ""}:${employee.username ?? ""}:${employee.hasLoginAccount}`
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
  const hasLoginAccount = employee?.hasLoginAccount ?? false;

  const handleFieldChange =
    (field: keyof EmployeeFormState) =>
    (event: ChangeEvent<HTMLInputElement>): void => {
      setDraft((current) => ({
        form: { ...current.form, [field]: field === "password" ? event.target.value.replace(/\D/g, "").slice(0, 6) : event.target.value },
        errors: current.errors,
      }));
    };

  const validate = () => {
    const nextErrors = validateEmployeeForm(form, { isCreate, hasLoginAccount });

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
