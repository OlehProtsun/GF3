import type { ChangeEvent, FormEvent } from "react";
import type { ShopFormErrors, ShopFormFieldElement, ShopFormState } from "@entities/shops/model/form";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import { LabeledField, TextArea, TextInput } from "@shared/ui/forms/Field";
import { FormActions, FormGrid, FormRow } from "@shared/ui/forms/FormLayout";
import { CheckIcon, CloseIcon, InformationIcon } from "@shared/ui/icons";
import { CardSection } from "@shared/ui/sections/CardSection";
import styles from "./ShopDetailsForm.module.css";

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
    <CardSection title="Details" icon={<InformationIcon size={18} className={styles.infoIcon} />}>
      {isLoading ? <div className={styles.loading}>Loading...</div> : null}

      {hasLoadError ? <ErrorBanner className={styles.errorBanner}>Could not load shop.</ErrorBanner> : null}

      <form onSubmit={onSubmit}>
        <FormGrid>
          <FormRow>
            <LabeledField id="name" label="Name" error={errors.name}>
              <TextInput
                id="name"
                value={form.name}
                placeholder="Example: Central Store"
                onChange={onFieldChange("name")}
                aria-invalid={Boolean(errors.name)}
                aria-describedby={errors.name ? "name-error" : undefined}
              />
            </LabeledField>

            <LabeledField id="address" label="Address" error={errors.address}>
              <TextInput
                id="address"
                value={form.address}
                placeholder="Example: 123 Main Street"
                onChange={onFieldChange("address")}
                aria-invalid={Boolean(errors.address)}
                aria-describedby={errors.address ? "address-error" : undefined}
              />
            </LabeledField>
          </FormRow>

          <LabeledField id="description" label="Description">
            <TextArea
              id="description"
              rows={5}
              placeholder="Example: Main retail location with warehouse pickup"
              value={form.description}
              onChange={onFieldChange("description")}
            />
          </LabeledField>

          <FormActions>
            <IosButton
              label="Cancel"
              variant="secondary"
              icon={<CloseIcon size={18} />}
              onClick={onCancel}
              disabled={isSaving}
              className={styles.cancelBtn}
            />

            <IosButton
              label={isSaving ? "Saving..." : "Save"}
              variant="primary"
              icon={<CheckIcon size={18} />}
              type="submit"
              disabled={isSaving}
            />
          </FormActions>
        </FormGrid>
      </form>
    </CardSection>
  );
}
