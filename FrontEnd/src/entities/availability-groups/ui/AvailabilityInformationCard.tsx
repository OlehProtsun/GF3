import { t } from "@shared/i18n";
import type { ReactNode } from "react";
import { NumberStepperInput } from "@shared/ui/components/NumberStepperInput";
import { ErrorPill, TextInput } from "@shared/ui/forms/Field";
import { InformationIcon } from "@shared/ui/icons";
import { CardSection } from "@shared/ui/sections/CardSection";
import styles from "./AvailabilityInformationCard.module.css";

export type AvailabilityInformationErrors = {
  name?: string;
  month?: string;
  year?: string;
};

type AvailabilityInformationCardProps = {
  name: string;
  month: number;
  year: number;
  errors?: AvailabilityInformationErrors;
  headerRightSlot?: ReactNode;
  onNameChange: (value: string) => void;
  onMonthChange: (value: number) => void;
  onYearChange: (value: number) => void;
};

export function AvailabilityInformationCard({
  name,
  month,
  year,
  errors = {},
  headerRightSlot,
  onNameChange,
  onMonthChange,
  onYearChange,
}: AvailabilityInformationCardProps) {
  return (
    <CardSection
      className={styles.card}
      title={t("Information")}
      icon={<InformationIcon size={18} />}
      headerRightSlot={headerRightSlot}
    >
      <div className={styles.layout}>
        <label className={styles.field}>
          <span className={styles.label}>{t("Availability Name")}</span>
          <TextInput
            className={styles.input}
            value={name}
            onChange={event => onNameChange(event.target.value)}
            placeholder={t("Write availability name...")}
            aria-invalid={Boolean(errors.name)}
            aria-describedby={errors.name ? "availability-name-error" : undefined}
          />
          {errors.name ? <ErrorPill id="availability-name-error">{errors.name}</ErrorPill> : null}
        </label>

        <div className={styles.inlineFields}>
          <label className={styles.field}>
            <span className={styles.label}>{t("Availability Month")}</span>
            <NumberStepperInput
              className={`${styles.numberInput} ${errors.month ? styles.numberInputInvalid : ""}`}
              value={month}
              min={1}
              max={12}
              onChange={onMonthChange}
              ariaLabel={t("availability month")}
            />
            {errors.month ? <ErrorPill id="availability-month-error">{errors.month}</ErrorPill> : null}
          </label>

          <label className={styles.field}>
            <span className={styles.label}>{t("Availability Year")}</span>
            <NumberStepperInput
              className={`${styles.numberInput} ${errors.year ? styles.numberInputInvalid : ""}`}
              value={year}
              min={2026}
              max={4000}
              onChange={onYearChange}
              ariaLabel={t("availability year")}
            />
            {errors.year ? <ErrorPill id="availability-year-error">{errors.year}</ErrorPill> : null}
          </label>
        </div>
      </div>
    </CardSection>
  );
}
