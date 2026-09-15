import { t } from "@shared/i18n";
import type { ChangeEvent, FormEvent } from "react";
import type { AvailabilityGroup } from "@entities/availability-groups";
import type { Shop } from "@entities/shops";
import type {
  ContainerGraphFormErrors,
  ContainerGraphFormFieldElement,
  ContainerGraphFormState,
} from "@entities/containers/model/graphForm";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { RecordDetailsFormCard } from "@shared/ui/components/RecordDetailsFormCard";
import { LabeledField, TextArea, TextInput } from "@shared/ui/forms/Field";
import { FormRow } from "@shared/ui/forms/FormLayout";
import styles from "./ContainerGraphDetailsForm.module.css";

const MONTH_OPTIONS = [
  { value: "1", get label() { return t("January"); } },
  { value: "2", get label() { return t("February"); } },
  { value: "3", get label() { return t("March"); } },
  { value: "4", get label() { return t("April"); } },
  { value: "5", get label() { return t("May"); } },
  { value: "6", get label() { return t("June"); } },
  { value: "7", get label() { return t("July"); } },
  { value: "8", get label() { return t("August"); } },
  { value: "9", get label() { return t("September"); } },
  { value: "10", get label() { return t("October"); } },
  { value: "11", get label() { return t("November"); } },
  { value: "12", get label() { return t("December"); } },
] as const;

type ContainerGraphDetailsFormProps = {
  form: ContainerGraphFormState;
  errors: ContainerGraphFormErrors;
  shops: Shop[];
  availabilityGroups: AvailabilityGroup[];
  isOptionsLoading: boolean;
  optionsError?: string | null;
  isSaving: boolean;
  submitError?: string | null;
  onFieldChange: (field: keyof ContainerGraphFormState) => (event: ChangeEvent<ContainerGraphFormFieldElement>) => void;
  onCancel: () => void;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
};

export function ContainerGraphDetailsForm({
  form,
  errors,
  shops,
  availabilityGroups,
  isOptionsLoading,
  optionsError,
  isSaving,
  submitError,
  onFieldChange,
  onCancel,
  onSubmit,
}: ContainerGraphDetailsFormProps) {
  return (
    <RecordDetailsFormCard
      isLoading={false}
      hasLoadError={false}
      isSaving={isSaving}
      loadingMessage="Preparing schedule form..."
      errorMessage="Could not open the schedule form."
      onCancel={onCancel}
      onSubmit={onSubmit}
    >
      {optionsError ? <ErrorBanner>{optionsError}</ErrorBanner> : null}
      {submitError ? <ErrorBanner>{submitError}</ErrorBanner> : null}

      <FormRow>
        <LabeledField id="schedule-name" label={t("Name")} error={errors.name}>
          <TextInput
            id="schedule-name"
            value={form.name}
            placeholder={t("Example: March 2026 Main Shop")}
            onChange={onFieldChange("name")}
            aria-invalid={Boolean(errors.name)}
            aria-describedby={errors.name ? "schedule-name-error" : undefined}
          />
        </LabeledField>

        <LabeledField id="schedule-shop" label={t("Shop")} error={errors.shopId}>
          <select
            id="schedule-shop"
            className={styles.select}
            value={form.shopId}
            onChange={onFieldChange("shopId")}
            aria-invalid={Boolean(errors.shopId)}
            aria-describedby={errors.shopId ? "schedule-shop-error" : undefined}
          >
            <option value="">
              {isOptionsLoading && shops.length === 0 ? t("Loading shops...") : shops.length > 0 ? t("Select shop") : t("No shops available")}
            </option>
            {shops.map(shop => (
              <option key={shop.id} value={shop.id}>
                {shop.name}
              </option>
            ))}
          </select>
        </LabeledField>
      </FormRow>

      <FormRow>
        <LabeledField id="schedule-month" label={t("Month")} error={errors.month}>
          <select
            id="schedule-month"
            className={styles.select}
            value={form.month}
            onChange={onFieldChange("month")}
            aria-invalid={Boolean(errors.month)}
            aria-describedby={errors.month ? "schedule-month-error" : undefined}
          >
            {MONTH_OPTIONS.map(option => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </LabeledField>

        <LabeledField id="schedule-year" label={t("Year")} error={errors.year}>
          <TextInput
            id="schedule-year"
            type="number"
            min={2000}
            max={2100}
            value={form.year}
            onChange={onFieldChange("year")}
            aria-invalid={Boolean(errors.year)}
            aria-describedby={errors.year ? "schedule-year-error" : undefined}
          />
        </LabeledField>
      </FormRow>

      <FormRow>
        <LabeledField id="schedule-people-per-shift" label={t("People per shift")} error={errors.peoplePerShift}>
          <TextInput
            id="schedule-people-per-shift"
            type="number"
            min={1}
            value={form.peoplePerShift}
            onChange={onFieldChange("peoplePerShift")}
            aria-invalid={Boolean(errors.peoplePerShift)}
            aria-describedby={errors.peoplePerShift ? "schedule-people-per-shift-error" : undefined}
          />
        </LabeledField>

        <LabeledField id="schedule-max-hours" label={t("Max hours / employee")} error={errors.maxHoursPerEmpMonth}>
          <TextInput
            id="schedule-max-hours"
            type="number"
            min={1}
            value={form.maxHoursPerEmpMonth}
            onChange={onFieldChange("maxHoursPerEmpMonth")}
            aria-invalid={Boolean(errors.maxHoursPerEmpMonth)}
            aria-describedby={errors.maxHoursPerEmpMonth ? "schedule-max-hours-error" : undefined}
          />
        </LabeledField>
      </FormRow>

      <FormRow>
        <LabeledField id="schedule-shift-1" label={t("Shift 1")} error={errors.shift1Time}>
          <TextInput
            id="schedule-shift-1"
            value={form.shift1Time}
            placeholder="06:00 - 14:00"
            onChange={onFieldChange("shift1Time")}
            aria-invalid={Boolean(errors.shift1Time)}
            aria-describedby={errors.shift1Time ? "schedule-shift-1-error" : undefined}
          />
        </LabeledField>

        <LabeledField id="schedule-shift-2" label={t("Shift 2")} error={errors.shift2Time}>
          <TextInput
            id="schedule-shift-2"
            value={form.shift2Time}
            placeholder="14:00 - 22:00"
            onChange={onFieldChange("shift2Time")}
            aria-invalid={Boolean(errors.shift2Time)}
            aria-describedby={errors.shift2Time ? "schedule-shift-2-error" : undefined}
          />
        </LabeledField>
      </FormRow>

      <FormRow>
        <LabeledField id="schedule-max-consecutive-days" label={t("Max consecutive days")} error={errors.maxConsecutiveDays}>
          <TextInput
            id="schedule-max-consecutive-days"
            type="number"
            min={0}
            value={form.maxConsecutiveDays}
            onChange={onFieldChange("maxConsecutiveDays")}
            aria-invalid={Boolean(errors.maxConsecutiveDays)}
            aria-describedby={errors.maxConsecutiveDays ? "schedule-max-consecutive-days-error" : undefined}
          />
        </LabeledField>

        <LabeledField id="schedule-max-consecutive-full" label={t("Max consecutive full")} error={errors.maxConsecutiveFull}>
          <TextInput
            id="schedule-max-consecutive-full"
            type="number"
            min={0}
            value={form.maxConsecutiveFull}
            onChange={onFieldChange("maxConsecutiveFull")}
            aria-invalid={Boolean(errors.maxConsecutiveFull)}
            aria-describedby={errors.maxConsecutiveFull ? "schedule-max-consecutive-full-error" : undefined}
          />
        </LabeledField>
      </FormRow>

      <FormRow>
        <LabeledField id="schedule-max-full-per-month" label={t("Max full / month")} error={errors.maxFullPerMonth}>
          <TextInput
            id="schedule-max-full-per-month"
            type="number"
            min={0}
            value={form.maxFullPerMonth}
            onChange={onFieldChange("maxFullPerMonth")}
            aria-invalid={Boolean(errors.maxFullPerMonth)}
            aria-describedby={errors.maxFullPerMonth ? "schedule-max-full-per-month-error" : undefined}
          />
        </LabeledField>

        <LabeledField id="schedule-availability-group" label={t("Availability group")} error={errors.availabilityGroupId}>
          <select
            id="schedule-availability-group"
            className={styles.select}
            value={form.availabilityGroupId}
            onChange={onFieldChange("availabilityGroupId")}
            aria-invalid={Boolean(errors.availabilityGroupId)}
            aria-describedby={errors.availabilityGroupId ? "schedule-availability-group-error" : undefined}
          >
            <option value="">
              {availabilityGroups.length > 0 ? t("No availability group") : t("No matching groups")}
            </option>
            {availabilityGroups.map(group => (
              <option key={group.id} value={group.id}>
                {group.name}
              </option>
            ))}
          </select>
          {availabilityGroups.length === 0 ? (
            <div className={styles.helperText}>{t("No availability groups match the selected month and year yet.")}</div>
          ) : null}
        </LabeledField>
      </FormRow>

      <LabeledField id="schedule-note" label={t("Note")} error={errors.note}>
        <TextArea
          id="schedule-note"
          rows={6}
          placeholder={t("Optional note for this schedule")}
          value={form.note}
          onChange={onFieldChange("note")}
          aria-invalid={Boolean(errors.note)}
          aria-describedby={errors.note ? "schedule-note-error" : undefined}
        />
      </LabeledField>
    </RecordDetailsFormCard>
  );
}
