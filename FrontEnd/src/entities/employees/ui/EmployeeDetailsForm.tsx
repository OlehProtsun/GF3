import type { ChangeEvent, FormEvent } from "react";
import type { EmployeeFormErrors, EmployeeFormState } from "@entities/employees/model/form";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import { LabeledField, TextInput } from "@shared/ui/forms/Field";
import { FormActions, FormGrid, FormRow } from "@shared/ui/forms/FormLayout";
import { CheckIcon, CloseIcon, InformationIcon } from "@shared/ui/icons";
import { CardSection } from "@shared/ui/sections/CardSection";
import styles from "./EmployeeDetailsForm.module.css";

type EmployeeDetailsFormProps = {
  form: EmployeeFormState;
  errors: EmployeeFormErrors;
  isLoading: boolean;
  hasLoadError: boolean;
  isSaving: boolean;
  onFieldChange: (field: keyof EmployeeFormState) => (event: ChangeEvent<HTMLInputElement>) => void;
  onCancel: () => void;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
};

export function EmployeeDetailsForm({
  form,
  errors,
  isLoading,
  hasLoadError,
  isSaving,
  onFieldChange,
  onCancel,
  onSubmit,
}: EmployeeDetailsFormProps) {
  return (
    <CardSection title="Details" icon={<InformationIcon size={18} className={styles.infoIcon} />}>
      {isLoading ? <div className={styles.loading}>Loading...</div> : null}

      {hasLoadError ? <ErrorBanner className={styles.errorBanner}>Could not load employee.</ErrorBanner> : null}

      <form onSubmit={onSubmit}>
        <FormGrid>
          <FormRow>
            <LabeledField id="firstName" label="First Name" error={errors.firstName}>
              <TextInput
                id="firstName"
                value={form.firstName}
                placeholder="Example: John"
                onChange={onFieldChange("firstName")}
                aria-invalid={Boolean(errors.firstName)}
                aria-describedby={errors.firstName ? "firstName-error" : undefined}
              />
            </LabeledField>

            <LabeledField id="lastName" label="Last Name" error={errors.lastName}>
              <TextInput
                id="lastName"
                value={form.lastName}
                placeholder="Example: Doe"
                onChange={onFieldChange("lastName")}
                aria-invalid={Boolean(errors.lastName)}
                aria-describedby={errors.lastName ? "lastName-error" : undefined}
              />
            </LabeledField>
          </FormRow>

          <LabeledField id="email" label="Email" error={errors.email}>
            <TextInput
              id="email"
              type="email"
              placeholder="Example: john.doe@example.com"
              value={form.email}
              onChange={onFieldChange("email")}
              aria-invalid={Boolean(errors.email)}
              aria-describedby={errors.email ? "email-error" : undefined}
            />
          </LabeledField>

          <LabeledField id="phone" label="Phone">
            <TextInput
              id="phone"
              type="tel"
              placeholder="Example: +1 (555) 123-4567"
              value={form.phone}
              onChange={onFieldChange("phone")}
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
