import { t } from "@shared/i18n";
import type { ChangeEvent, FormEvent } from "react";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { RecordDetailsFormCard } from "@shared/ui/components/RecordDetailsFormCard";
import { LabeledField, TextArea, TextInput } from "@shared/ui/forms/Field";
import { FormRow } from "@shared/ui/forms/FormLayout";
import type { ContainerFormErrors, ContainerFormFieldElement, ContainerFormState } from "@entities/containers/model/form";

type ContainerDetailsFormProps = {
  form: ContainerFormState;
  errors: ContainerFormErrors;
  isLoading: boolean;
  hasLoadError: boolean;
  isSaving: boolean;
  submitError?: string | null;
  onFieldChange: (field: keyof ContainerFormState) => (event: ChangeEvent<ContainerFormFieldElement>) => void;
  onCancel: () => void;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
};

export function ContainerDetailsForm({
  form,
  errors,
  isLoading,
  hasLoadError,
  isSaving,
  submitError,
  onFieldChange,
  onCancel,
  onSubmit,
}: ContainerDetailsFormProps) {
  return (
    <RecordDetailsFormCard
      isLoading={isLoading}
      hasLoadError={hasLoadError}
      isSaving={isSaving}
      loadingMessage={t("Loading container...")}
      errorMessage={t("Could not load this container.")}
      onCancel={onCancel}
      onSubmit={onSubmit}
    >
      {submitError ? <ErrorBanner>{submitError}</ErrorBanner> : null}

      <FormRow>
        <LabeledField id="name" label={t("Name")} error={errors.name}>
          <TextInput
            id="name"
            value={form.name}
            placeholder={t("Example: Northern Cluster")}
            onChange={onFieldChange("name")}
            aria-invalid={Boolean(errors.name)}
            aria-describedby={errors.name ? "name-error" : undefined}
          />
        </LabeledField>
      </FormRow>

      <LabeledField id="note" label={t("Note")} error={errors.note}>
        <TextArea
          id="note"
          rows={8}
          placeholder={t("Optional context for this container workspace")}
          value={form.note}
          onChange={onFieldChange("note")}
          aria-invalid={Boolean(errors.note)}
          aria-describedby={errors.note ? "note-error" : undefined}
        />
      </LabeledField>
    </RecordDetailsFormCard>
  );
}
