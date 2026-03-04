import type { ReactNode } from "react";
import styles from "./ListCardSection.module.css";

type ListCardSectionProps = {
  totalCount: number;
  error?: unknown;
  errorMessage?: string;
  isFetching: boolean;
  hasData: boolean;
  actionSlot?: ReactNode;
  children: ReactNode;
};

export function ListCardSection({
  totalCount,
  error,
  errorMessage = "Could not load data.",
  isFetching,
  hasData,
  actionSlot,
  children,
}: ListCardSectionProps) {
  const showError = Boolean(error) && !isFetching && !hasData;

  return (
    <section className={styles.card}>
      <div className={styles.topBar}>
        <div>{actionSlot}</div>
        <div className={styles.totalBadge}>Total: {totalCount}</div>
      </div>

      {showError ? (
        <div className={styles.errorWrap}>
          <div className={styles.errorBanner} role="alert" aria-live="polite">
            <svg className={styles.errorIcon} viewBox="0 0 24 24" aria-hidden="true" focusable="false">
              <path d="M12 9v4M12 17h.01" />
              <path d="M10.29 3.86 2.17 17.92A2 2 0 0 0 3.9 21h16.2a2 2 0 0 0 1.73-3.08L13.71 3.86a2 2 0 0 0-3.42 0Z" />
            </svg>

            <span className={styles.errorText}>{errorMessage}</span>
          </div>
        </div>
      ) : null}

      {children}
    </section>
  );
}
