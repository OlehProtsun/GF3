import { useMemo } from "react";
import type { ChangeEvent } from "react";
import { useSyncedDraft } from "@shared/lib/useSyncedDraft";
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
  const sourceKey = useMemo(
    () =>
      isCreate
        ? "create"
        : `container:${container?.id ?? "new"}:${container?.name ?? ""}:${container?.note ?? ""}`,
    [container?.id, container?.name, container?.note, isCreate],
  );
  const initialDraft = useMemo(
    () => ({
      form: buildInitialState(container),
      errors: {} as ContainerFormErrors,
    }),
    [container],
  );
  const { value: draft, setValue: setDraft } = useSyncedDraft(sourceKey, initialDraft);
  const { form, errors } = draft;

  const handleFieldChange =
    (field: keyof ContainerFormState) => (event: ChangeEvent<ContainerFormFieldElement>) => {
      const value = event.target.value;

      setDraft((current) => {
        const nextErrors = { ...current.errors };
        delete nextErrors[field];

        return {
          form: {
            ...current.form,
            [field]: value,
          },
          errors: nextErrors,
        };
      });
    };

  const validate = () => {
    const nextErrors = buildValidationErrors(form);

    setDraft((current) => ({
      ...current,
      errors: nextErrors,
    }));

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
      setDraft((current) => ({
        ...current,
        errors: nextErrors,
      }));
    }
  };

  const reset = (nextContainer?: Container | null) => {
    setDraft({
      form: buildInitialState(nextContainer),
      errors: {},
    });
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
