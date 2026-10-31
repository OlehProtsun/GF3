import { t } from "@shared/i18n";
import type { FormEvent, ReactNode } from "react";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import { FormActions, FormGrid } from "@shared/ui/forms/FormLayout";
import { CheckIcon, CloseIcon, InformationIcon } from "@shared/ui/icons";
import { CardSection } from "@shared/ui/sections/CardSection";
import styles from "./RecordDetailsFormCard.module.css";

type RecordDetailsFormCardProps = {
  isLoading: boolean;
  hasLoadError: boolean;
  isSaving: boolean;
  loadingMessage: string;
  errorMessage: string;
  onCancel: () => void;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  children: ReactNode;
};

export function RecordDetailsFormCard({
  isLoading,
  hasLoadError,
  isSaving,
  loadingMessage,
  errorMessage,
  onCancel,
  onSubmit,
  children,
}: RecordDetailsFormCardProps) {
  return (
    <CardSection title={t("Details")} icon={<InformationIcon size={18} className={styles.infoIcon} />}>
      {isLoading ? <div className={styles.loading}>{loadingMessage}</div> : null}
      {hasLoadError ? <ErrorBanner className={styles.errorBanner}>{errorMessage}</ErrorBanner> : null}

      <form onSubmit={onSubmit}>
        <FormGrid>
          {children}

          <FormActions>
            <IosButton
              label={t("Cancel")}
              variant="secondary"
              icon={<CloseIcon size={18} />}
              onClick={onCancel}
              disabled={isSaving}
              className={styles.cancelBtn}
            />

            <IosButton
              label={isSaving ? t("Saving...") : t("Save")}
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
