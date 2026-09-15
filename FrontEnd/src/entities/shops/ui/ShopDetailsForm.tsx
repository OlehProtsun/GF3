import { t } from "@shared/i18n";
import type { ChangeEvent, FormEvent } from "react";
import type { ShopFormErrors, ShopFormFieldElement, ShopFormState } from "@entities/shops/model/form";
import { RecordDetailsFormCard } from "@shared/ui/components/RecordDetailsFormCard";
import { LabeledField, TextArea, TextInput } from "@shared/ui/forms/Field";
import { FormRow } from "@shared/ui/forms/FormLayout";

type ShopDetailsFormProps = {
  form: ShopFormState;
  errors: ShopFormErrors;
  isLoading: boolean;
  hasLoadError: boolean;
  isSaving: boolean;
  onFieldChange: (field: keyof ShopFormState) => (event: ChangeEvent<ShopFormFieldElement>) => void;
  onCancel: () => void;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
};

export function ShopDetailsForm({
  form,
  errors,
  isLoading,
  hasLoadError,
  isSaving,
  onFieldChange,
  onCancel,
  onSubmit,
}: ShopDetailsFormProps) {
  return (
    <RecordDetailsFormCard
      isLoading={isLoading}
      hasLoadError={hasLoadError}
      isSaving={isSaving}
      loadingMessage="Loading..."
      errorMessage="Could not load shop."
      onCancel={onCancel}
      onSubmit={onSubmit}
    >
      <FormRow>
        <LabeledField id="name" label={t("Name")} error={errors.name}>
          <TextInput
            id="name"
            value={form.name}
            placeholder={t("Example: Central Store")}
            onChange={onFieldChange("name")}
            aria-invalid={Boolean(errors.name)}
            aria-describedby={errors.name ? "name-error" : undefined}
          />
        </LabeledField>

        <LabeledField id="address" label={t("Address")} error={errors.address}>
          <TextInput
            id="address"
            value={form.address}
            placeholder={t("Example: 123 Main Street")}
            onChange={onFieldChange("address")}
            aria-invalid={Boolean(errors.address)}
            aria-describedby={errors.address ? "address-error" : undefined}
          />
        </LabeledField>
      </FormRow>

      <LabeledField id="description" label={t("Description")}>
        <TextArea
          id="description"
          rows={5}
          placeholder={t("Example: Main retail location with warehouse pickup")}
          value={form.description}
          onChange={onFieldChange("description")}
        />
      </LabeledField>
    </RecordDetailsFormCard>
  );
}
