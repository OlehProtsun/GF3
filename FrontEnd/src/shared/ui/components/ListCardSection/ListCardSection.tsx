import type { ReactNode } from "react";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import styles from "./ListCardSection.module.css";

type ListCardSectionProps = {
  error?: unknown;
  errorMessage?: string;
  isFetching: boolean;
  hasData: boolean;
  loadingMessage?: string;
  searchQuery?: string;
  emptyTitle?: string;
  emptyDescription?: ReactNode;
  emptyAction?: ReactNode;
  searchEmptyTitle?: string;
  searchEmptyDescription?: ReactNode;
  searchEmptyAction?: ReactNode;
  className?: string;
  children: ReactNode;
};

export function ListCardSection({
  error,
  errorMessage = "Could not load data.",
  isFetching,
  hasData,
  loadingMessage = "Loading...",
  searchQuery,
  emptyTitle = "Nothing here yet",
  emptyDescription,
  emptyAction,
  searchEmptyTitle = "Nothing found",
  searchEmptyDescription,
  searchEmptyAction,
  className,
  children,
}: ListCardSectionProps) {
  const trimmedSearchQuery = searchQuery?.trim() ?? "";
  const showError = Boolean(error) && !isFetching && !hasData;
  const showLoading = isFetching && !hasData && !showError;
  const showEmpty = !isFetching && !hasData && !showError;
  const isSearchEmpty = showEmpty && trimmedSearchQuery.length > 0;
  const cardClassName = [styles.card, className ?? ""].filter(Boolean).join(" ");

  return (
    <section className={cardClassName}>
      {showError ? <ErrorBanner className={styles.errorBanner}>{errorMessage}</ErrorBanner> : null}

      {showLoading ? (
        <div className={styles.state}>
          <div className={styles.stateTitle}>{loadingMessage}</div>
        </div>
      ) : null}

      {showEmpty ? (
        <div className={styles.state}>
          <div className={styles.stateTitle}>
            {isSearchEmpty ? searchEmptyTitle : emptyTitle}
          </div>

          {isSearchEmpty ? (
            searchEmptyDescription ? (
              <div className={styles.stateDescription}>{searchEmptyDescription}</div>
            ) : null
          ) : emptyDescription ? (
            <div className={styles.stateDescription}>{emptyDescription}</div>
          ) : null}

          {isSearchEmpty ? (
            searchEmptyAction ? (
              <div className={styles.stateActions}>{searchEmptyAction}</div>
            ) : null
          ) : emptyAction ? (
            <div className={styles.stateActions}>{emptyAction}</div>
          ) : null}
        </div>
      ) : null}

      {!showError && !showLoading && !showEmpty ? children : null}
    </section>
  );
}
