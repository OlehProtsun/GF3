import type { PropsWithChildren } from "react";
import { NavLink } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import { ApiError } from "@shared/api/httpClient";
import { t } from "@shared/i18n";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { BackIcon, SearchIcon, CloseIcon } from "@shared/ui/icons";
import styles from "./ManagerPhonePage.module.css";

export function ManagerPhonePage({ children, backTo, title, query, onQueryChange, valid = true, missing = false, queries = [], queryKeys = [] }: PropsWithChildren<{
  backTo?: string;
  title?: string;
  query?: string;
  onQueryChange?: (value: string) => void;
  valid?: boolean;
  missing?: boolean;
  queries?: Array<{ isLoading: boolean; isError: boolean; error?: unknown }>;
  queryKeys?: Array<readonly unknown[]>;
}>) {
  const client = useQueryClient();
  const loading = valid && queries.some(item => item.isLoading);
  const failed = queries.some(item => item.isError || item.error);
  const notFound = missing && queries.some(item => item.error instanceof ApiError && item.error.status === 404);
  return <div className={styles.page}>
    {(backTo || onQueryChange || title) && <div className={styles.toolbar}>
      {backTo && <NavLink className={styles.back} to={backTo}><BackIcon size={18} />{t("Back")}</NavLink>}
      {title && <span className={styles.toolbarTitle}>{title}</span>}
      {onQueryChange && <div className={styles.search}>
        <SearchIcon size={18} />
        <input aria-label={t("Search")} placeholder={t("Search")} type="search" value={query ?? ""} onChange={event => onQueryChange(event.target.value)} />
        {query && <button type="button" className={styles.clear} aria-label={t("Clear Search")} onClick={() => onQueryChange("")}><CloseIcon size={18} /></button>}
      </div>}
    </div>}
    {!valid ? <ErrorBanner dismissible={false}>{t("Invalid record ID.")}</ErrorBanner>
      : notFound ? <p role="status">{t("Record not found.")}</p>
      : failed ? <><ErrorBanner dismissible={false}>{t("Could not load records. Please try again.")}</ErrorBanner><button className={styles.retry} onClick={() => queryKeys.forEach(queryKey => client.invalidateQueries({ queryKey }))}>{t("Retry")}</button></>
      : loading ? <p role="status">{t("Loading...")}</p>
      : missing ? <p role="status">{t("Record not found.")}</p>
      : children}
  </div>;
}
