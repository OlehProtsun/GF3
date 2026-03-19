import { useEffect, useState } from "react";
import type { ChangeEvent } from "react";
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
  const [form, setForm] = useState<EmployeeFormState>(EMPTY_FORM);
  const [errors, setErrors] = useState<EmployeeFormErrors>({});

  useEffect(() => {
    if (employee) {
      setForm(createEmployeeFormState(employee));
      setErrors({});
      return;
    }

    if (isCreate) {
      setForm(EMPTY_FORM);
      setErrors({});
    }
  }, [employee, isCreate]);

  const handleFieldChange =
    (field: keyof EmployeeFormState) =>
    (event: ChangeEvent<HTMLInputElement>): void => {
      setForm((prev) => ({ ...prev, [field]: event.target.value }));
    };

  const validate = () => {
    const nextErrors = validateEmployeeForm(form);
    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  return {
    form,
    errors,
    handleFieldChange,
    validate,
  };
}
