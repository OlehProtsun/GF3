import { useEffect, useId, useState, type MouseEvent } from "react";
import type {
  ContainerGraphFormErrors,
  ContainerGraphFormState,
} from "@entities/containers/model/graphForm";
import { applyGraphApiErrors, buildGraphFormErrors, type SaveSchedulePresetDto } from "@entities/containers";
import type { Shop } from "@entities/shops/model/types";
import { ApiError } from "@shared/api/httpClient";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import { LabeledField, TextInput } from "@shared/ui/forms/Field";
import { CheckIcon, CloseIcon, ScheduleDetailsIcon } from "@shared/ui/icons";
import { ContainerGraphDetailsFields } from "./ContainerGraphDetailsFields";
import styles from "./ContainerGraphPresetDialog.module.css";

type ContainerGraphPresetDialogProps = {
  open: boolean;
  initialForm: ContainerGraphFormState;
  shops: Shop[];
  onCancel: () => void;
  onSave: (payload: SaveSchedulePresetDto) => Promise<void>;
};

function buildDialogErrors(validationErrors?: Record<string, string[]>) {
  if (!validationErrors) {
    return { presetNameError: undefined, formErrors: {} as ContainerGraphFormErrors };
  }

  const scheduleNameError =
    validationErrors.ScheduleName?.[0]
    ?? validationErrors.scheduleName?.[0]
    ?? validationErrors["schedule_name"]?.[0];
  const presetNameError = validationErrors.Name?.[0] ?? validationErrors.name?.[0];
  const mappedErrors = applyGraphApiErrors(validationErrors);
  if (scheduleNameError) {
    mappedErrors.name = scheduleNameError;
  }

  return {
    presetNameError,
    formErrors: mappedErrors,
  };
}

function toPresetPayload(
  presetName: string,
  form: ContainerGraphFormState,
): SaveSchedulePresetDto {
  return {
    name: presetName.trim(),
    scheduleName: form.name.trim(),
    shopId: Number(form.shopId),
    year: Number(form.year),
    month: Number(form.month),
    peoplePerShift: Number(form.peoplePerShift),
    shift1Time: form.shift1Time.trim(),
    shift2Time: form.shift2Time.trim(),
    maxHoursPerEmpMonth: Number(form.maxHoursPerEmpMonth),
    maxConsecutiveDays: Number(form.maxConsecutiveDays),
    maxConsecutiveFull: Number(form.maxConsecutiveFull),
    maxFullPerMonth: Number(form.maxFullPerMonth),
    availabilityGroupId: null,
    employees: [],
  };
}

export function ContainerGraphPresetDialog({
  open,
  initialForm,
  shops,
  onCancel,
  onSave,
}: ContainerGraphPresetDialogProps) {
  const titleId = useId();
  const descriptionId = useId();
  const [presetName, setPresetName] = useState("");
  const [form, setForm] = useState<ContainerGraphFormState>(initialForm);
  const [formErrors, setFormErrors] = useState<ContainerGraphFormErrors>({});
  const [presetNameError, setPresetNameError] = useState<string | undefined>();
  const [submitError, setSubmitError] = useState<string | undefined>();
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    if (!open) {
      return;
    }

    const nextName = initialForm.name.trim();
    setPresetName(nextName);
    setForm({
      ...initialForm,
      name: nextName,
      availabilityGroupId: "",
    });
    setFormErrors({});
    setPresetNameError(undefined);
    setSubmitError(undefined);
    setIsSaving(false);
  }, [initialForm, open]);

  useEffect(() => {
    if (!open) {
      return;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape" && !isSaving) {
        onCancel();
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [isSaving, onCancel, open]);

  const shopIdSet = new Set(shops.map(shop => shop.id));

  if (!open) {
    return null;
  }

  const handleOverlayMouseDown = (event: MouseEvent<HTMLDivElement>) => {
    if (event.target !== event.currentTarget || isSaving) {
      return;
    }

    onCancel();
  };

  const handleFieldChange = (field: keyof ContainerGraphFormState) => (value: string) => {
    setForm(current => ({ ...current, [field]: value }));
    setSubmitError(undefined);
    setFormErrors(current => {
      if (!current[field]) {
        return current;
      }

      const nextErrors = { ...current };
      delete nextErrors[field];
      return nextErrors;
    });
  };

  const submit = async () => {
    const nextPresetNameError = presetName.trim() ? undefined : "Name is required.";
    const nextFormErrors = buildGraphFormErrors(form, shopIdSet, new Set<number>());

    setPresetNameError(nextPresetNameError);
    setFormErrors(nextFormErrors);
    setSubmitError(undefined);

    if (nextPresetNameError || Object.keys(nextFormErrors).length > 0) {
      return;
    }

    setIsSaving(true);
    try {
      await onSave(toPresetPayload(presetName, form));
      onCancel();
    } catch (error) {
      if (error instanceof ApiError) {
        const dialogErrors = buildDialogErrors(error.validationErrors);
        setPresetNameError(dialogErrors.presetNameError);
        setFormErrors(current => ({ ...current, ...dialogErrors.formErrors }));
        setSubmitError(error.message);
      } else {
        setSubmitError(error instanceof Error ? error.message : "Could not save this preset.");
      }
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div
      className={styles.overlay}
      role="dialog"
      aria-modal="true"
      aria-labelledby={titleId}
      aria-describedby={descriptionId}
      onMouseDown={handleOverlayMouseDown}
    >
      <div className={styles.dialog}>
        <div className={styles.header}>
          <div className={styles.titleBlock}>
            <div className={styles.eyebrow}>
              <ScheduleDetailsIcon size={16} />
              <span>Schedule Preset</span>
            </div>
            <h3 id={titleId} className={styles.title}>Add preset</h3>
            <p id={descriptionId} className={styles.description}>
              Save the core schedule setup so it can be reused without filling the same values from scratch each time.
            </p>
          </div>
        </div>

        <div className={styles.content}>
          {submitError ? <ErrorBanner className={styles.banner}>{submitError}</ErrorBanner> : null}

          <div className={styles.detailsShell}>
            <ContainerGraphDetailsFields
              idPrefix="preset"
              form={form}
              formErrors={formErrors}
              shops={shops}
              availabilityGroups={[]}
              employees={[]}
              graphEmployeeRows={[]}
              topSlot={(
                <div className={styles.nameGrid}>
                  <LabeledField id="preset-graph-name" label="Graph name" error={formErrors.name} className={styles.nameField}>
                    <TextInput
                      id="preset-graph-name"
                      className={styles.presetNameInput}
                      value={form.name}
                      placeholder="Example: Main shop weekday schedule"
                      aria-invalid={formErrors.name ? true : undefined}
                      onChange={event => handleFieldChange("name")(event.target.value)}
                    />
                  </LabeledField>

                  <LabeledField id="schedule-preset-name" label="Preset name" error={presetNameError} className={styles.nameField}>
                    <TextInput
                      id="schedule-preset-name"
                      className={styles.presetNameInput}
                      value={presetName}
                      placeholder="Example: Main shop weekday preset"
                      aria-invalid={presetNameError ? true : undefined}
                      onChange={event => {
                        setPresetName(event.target.value);
                        if (presetNameError) {
                          setPresetNameError(undefined);
                        }
                        if (submitError) {
                          setSubmitError(undefined);
                        }
                      }}
                      onKeyDown={event => {
                        if (event.key === "Enter" && (event.ctrlKey || event.metaKey)) {
                          event.preventDefault();
                          void submit();
                        }
                      }}
                    />
                  </LabeledField>
                </div>
              )}
              showNameField={false}
              showAvailabilityField={false}
              showEmployeeMinHoursField={false}
              selectDropdownPlacement="up"
              shopSelectShadow="soft"
              onFieldChange={handleFieldChange}
              onEmployeeMinHoursChange={() => {}}
            />
          </div>
        </div>

        <div className={styles.footer}>
          <IosButton
            label="Cancel"
            variant="secondary"
            icon={<CloseIcon size={16} />}
            disabled={isSaving}
            onClick={onCancel}
          />
          <IosButton
            label={isSaving ? "Saving..." : "Save preset"}
            icon={<CheckIcon size={16} />}
            disabled={isSaving}
            onClick={() => {
              void submit();
            }}
          />
        </div>
      </div>
    </div>
  );
}
