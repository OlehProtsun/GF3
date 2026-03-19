import { useEffect, useMemo, useState } from "react";
import type { KeyboardEvent } from "react";
import type { SaveAvailabilityGroupDto } from "@entities/availability-groups/api/dto";
import type { AvailabilityGroup } from "@entities/availability-groups/model/types";
import { availabilityMonthOptions } from "@entities/availability-groups/model/presentation";
import { IosButton } from "@shared/ui/components/IosButton";
import { LabeledField, TextInput } from "@shared/ui/forms";
import styles from "./AvailabilityGroupFormDialog.module.css";

type AvailabilityGroupFormDialogProps = {
  open: boolean;
  mode: "create" | "edit";
  initialGroup?: AvailabilityGroup | null;
  isSubmitting: boolean;
  submitError?: string;
  onCancel: () => void;
  onSave: (payload: SaveAvailabilityGroupDto) => void;
};

type FormState = {
  name: string;
  year: string;
  month: string;
};

type FormErrors = Partial<Record<keyof FormState, string>>;

function createFormState(group?: AvailabilityGroup | null): FormState {
  const now = new Date();

  return {
    name: group?.name ?? "",
    year: String(group?.year ?? now.getFullYear()),
    month: String(group?.month ?? now.getMonth() + 1),
  };
}

function validateFormState(formState: FormState) {
  const errors: FormErrors = {};
  const trimmedName = formState.name.trim();
  const year = Number(formState.year);
  const month = Number(formState.month);

  if (!trimmedName) {
    errors.name = "Name is required.";
  }

  if (!Number.isInteger(year) || year < 1 || year > 9999) {
    errors.year = "Year must be between 1 and 9999.";
  }

  if (!Number.isInteger(month) || month < 1 || month > 12) {
    errors.month = "Month must be between 1 and 12.";
  }

  return { errors, payload: { name: trimmedName, year, month } };
}

export function AvailabilityGroupFormDialog({
  open,
  mode,
  initialGroup,
  isSubmitting,
  submitError,
  onCancel,
  onSave,
}: AvailabilityGroupFormDialogProps) {
  const [formState, setFormState] = useState<FormState>(() => createFormState(initialGroup));
  const [errors, setErrors] = useState<FormErrors>({});

  useEffect(() => {
    if (!open) {
      return;
    }

    setFormState(createFormState(initialGroup));
    setErrors({});
  }, [initialGroup, open]);

  useEffect(() => {
    if (!open) {
      return;
    }

    const handleKeyDown = (event: globalThis.KeyboardEvent) => {
      if (event.key === "Escape" && !isSubmitting) {
        onCancel();
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [isSubmitting, onCancel, open]);

  const title = mode === "create" ? "Create Availability Group" : "Edit Availability Group";
  const saveLabel = isSubmitting ? "Saving..." : mode === "create" ? "Create Group" : "Save Changes";
  const describedById = useMemo(() => `${mode}-availability-group-dialog-description`, [mode]);

  if (!open) {
    return null;
  }

  const submit = () => {
    const result = validateFormState(formState);
    setErrors(result.errors);

    if (Object.keys(result.errors).length > 0) {
      return;
    }

    onSave(result.payload);
  };

  const handleDialogKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if (event.key === "Enter" && !(event.target instanceof HTMLSelectElement)) {
      event.preventDefault();
      submit();
    }
  };

  return (
    <div className={styles.overlay} role="dialog" aria-modal="true" aria-labelledby="availability-group-dialog-title" aria-describedby={describedById}>
      <div className={styles.dialog} onKeyDown={handleDialogKeyDown}>
        <div className={styles.header}>
          <div>
            <h3 id="availability-group-dialog-title">{title}</h3>
            <p id={describedById}>Define the group name and the target month for the availability schedule.</p>
          </div>
        </div>

        <div className={styles.formBody}>
          <LabeledField id="availability-group-name" label="Group Name" error={errors.name}>
            <TextInput
              id="availability-group-name"
              value={formState.name}
              onChange={event => setFormState(current => ({ ...current, name: event.target.value }))}
              placeholder="For example: Main Team"
              aria-invalid={errors.name ? true : undefined}
            />
          </LabeledField>

          <div className={styles.splitRow}>
            <LabeledField id="availability-group-month" label="Month" error={errors.month}>
              <select
                id="availability-group-month"
                className={styles.select}
                value={formState.month}
                onChange={event => setFormState(current => ({ ...current, month: event.target.value }))}
                aria-invalid={errors.month ? true : undefined}
              >
                {availabilityMonthOptions.map(option => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </LabeledField>

            <LabeledField id="availability-group-year" label="Year" error={errors.year}>
              <TextInput
                id="availability-group-year"
                type="number"
                inputMode="numeric"
                min={1}
                max={9999}
                value={formState.year}
                onChange={event => setFormState(current => ({ ...current, year: event.target.value }))}
                placeholder="2026"
                aria-invalid={errors.year ? true : undefined}
              />
            </LabeledField>
          </div>

          {submitError ? <p className={styles.submitError}>{submitError}</p> : null}
        </div>

        <div className={styles.footer}>
          <IosButton label="Cancel" variant="secondary" onClick={onCancel} disabled={isSubmitting} />
          <IosButton label={saveLabel} onClick={submit} disabled={isSubmitting} />
        </div>
      </div>
    </div>
  );
}
