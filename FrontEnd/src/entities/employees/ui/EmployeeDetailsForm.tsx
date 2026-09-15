import { t } from "@shared/i18n";
import type { ChangeEvent, FormEvent } from "react";
import type { EmployeeFormErrors, EmployeeFormState } from "@entities/employees/model/form";
import { RecordDetailsFormCard } from "@shared/ui/components/RecordDetailsFormCard";
import { LabeledField, TextInput } from "@shared/ui/forms/Field";
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
    <RecordDetailsFormCard
      isLoading={isLoading}
      hasLoadError={hasLoadError}
      isSaving={isSaving}
      loadingMessage="Loading..."
      errorMessage="Could not load employee."
      onCancel={onCancel}
      onSubmit={onSubmit}
    >
      <div className={styles.fields}>
        <div className={styles.compactRow}>
          <LabeledField id="firstName" label={t("First Name")} error={errors.firstName}>
            <TextInput
              id="firstName"
              value={form.firstName}
              placeholder={t("Example: John")}
              onChange={onFieldChange("firstName")}
              aria-invalid={Boolean(errors.firstName)}
              aria-describedby={errors.firstName ? "firstName-error" : undefined}
            />
          </LabeledField>

          <LabeledField id="lastName" label={t("Last Name")} error={errors.lastName}>
            <TextInput
              id="lastName"
              value={form.lastName}
              placeholder={t("Example: Doe")}
              onChange={onFieldChange("lastName")}
              aria-invalid={Boolean(errors.lastName)}
              aria-describedby={errors.lastName ? "lastName-error" : undefined}
            />
          </LabeledField>
        </div>

        <div className={styles.compactRow}>
          <LabeledField id="email" label={t("Email")} error={errors.email}>
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

          <LabeledField id="phone" label={t("Phone")}>
            <TextInput
              id="phone"
              type="tel"
              placeholder={t("Example: +1 (555) 123-4567")}
              value={form.phone}
              onChange={onFieldChange("phone")}
            />
          </LabeledField>
        </div>

        <div className={styles.compactRow}>
          <LabeledField id="username" label={t("Username")} error={errors.username}>
            <TextInput
              id="username"
              placeholder={t("Example: john.doe")}
              value={form.username}
              onChange={onFieldChange("username")}
              aria-invalid={Boolean(errors.username)}
              aria-describedby={errors.username ? "username-error" : undefined}
              autoCapitalize="none"
              autoCorrect="off"
            />
          </LabeledField>

          <LabeledField id="password" label={t("Password")} error={errors.password}>
            <TextInput
              id="password"
              type="password"
              inputMode="numeric"
              pattern="[0-9]*"
              minLength={6}
              maxLength={6}
              placeholder={t("Exactly 6 digits")}
              value={form.password}
              onChange={onFieldChange("password")}
              aria-invalid={Boolean(errors.password)}
              aria-describedby={errors.password ? "password-error" : undefined}
              autoComplete="654321"
            />
          </LabeledField>
        </div>
      </div>
    </RecordDetailsFormCard>
  );
}
