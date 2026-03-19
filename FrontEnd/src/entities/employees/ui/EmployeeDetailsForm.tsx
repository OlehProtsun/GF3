import type { ChangeEvent, FormEvent } from "react";
import type { EmployeeFormErrors, EmployeeFormState } from "@entities/employees/model/form";
import { RecordDetailsFormCard } from "@shared/ui/components/RecordDetailsFormCard";
import { LabeledField, TextInput } from "@shared/ui/forms/Field";
import { FormRow } from "@shared/ui/forms/FormLayout";

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
    <RecordDetailsFormCard
      isLoading={isLoading}
      hasLoadError={hasLoadError}
      isSaving={isSaving}
      loadingMessage="Loading..."
      errorMessage="Could not load employee."
      onCancel={onCancel}
      onSubmit={onSubmit}
    >
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
    </RecordDetailsFormCard>
  );
}
