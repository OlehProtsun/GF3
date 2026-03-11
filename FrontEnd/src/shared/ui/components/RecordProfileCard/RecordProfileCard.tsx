import type { ReactNode } from "react";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import {
  ProfileSummaryCard,
  type ProfileSummaryDetail,
} from "@shared/ui/components/ProfileSummaryCard";
import styles from "./RecordProfileCard.module.css";

type RecordProfileCardProps = {
  sectionTitle: ReactNode;
  icon?: ReactNode;
  headerMeta?: ReactNode;
  isLoading: boolean;
  hasLoadError: boolean;
  loadingMessage: string;
  errorMessage: string;
  avatar?: ReactNode;
  name?: ReactNode;
  subtitle?: ReactNode;
  details?: ProfileSummaryDetail[];
  actions?: ReactNode;
};

export function renderRecordDetailValue(value?: string | null, href?: string) {
  if (!value) {
    return <span className={styles.mutedValue}>Not provided</span>;
  }

  if (!href) {
    return value;
  }

  return (
    <a className={styles.valueLink} href={href}>
      {value}
    </a>
  );
}

export function RecordProfileCard({
  sectionTitle,
  icon,
  headerMeta,
  isLoading,
  hasLoadError,
  loadingMessage,
  errorMessage,
  avatar,
  name,
  subtitle,
  details = [],
  actions,
}: RecordProfileCardProps) {
  return (
    <ProfileSummaryCard
      sectionTitle={sectionTitle}
      icon={icon}
      headerMeta={headerMeta}
      statusContent={
        <>
          {isLoading ? <p className={styles.loading}>{loadingMessage}</p> : null}
          {hasLoadError ? <ErrorBanner>{errorMessage}</ErrorBanner> : null}
        </>
      }
      avatar={avatar}
      name={name}
      subtitle={subtitle}
      details={details}
      actions={actions}
    />
  );
}
