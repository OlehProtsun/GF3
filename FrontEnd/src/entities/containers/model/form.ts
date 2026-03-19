import { useEffect, useState } from "react";
import type { ChangeEvent } from "react";
import type { Container } from "./types";

export type ContainerFormState = {
  name: string;
  note: string;
};

export type ContainerFormErrors = Partial<Record<keyof ContainerFormState, string>>;

export type ContainerFormFieldElement = HTMLInputElement | HTMLTextAreaElement;

function buildInitialState(container?: Container | null): ContainerFormState {
  return {
    name: container?.name ?? "",
    note: container?.note ?? "",
  };
}

function buildValidationErrors(form: ContainerFormState): ContainerFormErrors {
  const nextErrors: ContainerFormErrors = {};

  if (!form.name.trim()) {
    nextErrors.name = "Container name is required.";
  }

  return nextErrors;
}

export function useContainerForm(container?: Container | null, isCreate = false) {
  const [form, setForm] = useState<ContainerFormState>(() => buildInitialState(container));
  const [errors, setErrors] = useState<ContainerFormErrors>({});

  useEffect(() => {
    setForm(buildInitialState(container));
    setErrors({});
  }, [container?.id, container?.name, container?.note, isCreate]);

  const handleFieldChange =
    (field: keyof ContainerFormState) => (event: ChangeEvent<ContainerFormFieldElement>) => {
      const value = event.target.value;

      setForm(prev => ({
        ...prev,
        [field]: value,
      }));

      setErrors(prev => {
        if (!prev[field]) {
          return prev;
        }

        const nextErrors = { ...prev };
        delete nextErrors[field];
        return nextErrors;
      });
    };

  const validate = () => {
    const nextErrors = buildValidationErrors(form);
    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const applyApiErrors = (validationErrors?: Record<string, string[]>) => {
    if (!validationErrors) {
      return;
    }

    const nextErrors: ContainerFormErrors = {};
    const nameErrors = validationErrors.Name ?? validationErrors.name;
    const noteErrors = validationErrors.Note ?? validationErrors.note;

    if (nameErrors?.[0]) {
      nextErrors.name = nameErrors[0];
    }

    if (noteErrors?.[0]) {
      nextErrors.note = noteErrors[0];
    }

    if (Object.keys(nextErrors).length > 0) {
      setErrors(nextErrors);
    }
  };

  const reset = (nextContainer?: Container | null) => {
    setForm(buildInitialState(nextContainer));
    setErrors({});
  };

  return {
    form,
    errors,
    handleFieldChange,
    validate,
    applyApiErrors,
    reset,
  };
}
