import { useContext, useEffect, useId, useRef, useState, type PropsWithChildren } from "react";
import { NavLink, useLocation } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import { ManagerPhoneNotepadContext } from "@app/layouts/manager-phone-layout/ManagerPhoneNotepadContext";
import { NotepadIcon } from "@features/manager-notepad/ui/NotepadIcon";
import { ApiError } from "@shared/api/httpClient";
import { t } from "@shared/i18n";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { ChevronLeftIcon, SearchIcon, CloseIcon } from "@shared/ui/icons";
import styles from "./ManagerPhonePage.module.css";

export function ManagerPhonePage({ children, backTo = "/", title, query, onQueryChange, onOpenNotepad, valid = true, missing = false, queries = [], queryKeys = [], fill = false }: PropsWithChildren<{
  backTo?: "/" | "/container" | `/container/${number}` | "/availability" | "/employee" | "/shop" | "/more";
  title?: string;
  fill?: boolean;
  query?: string;
  onQueryChange?: (value: string) => void;
  onOpenNotepad?: () => void;
  valid?: boolean;
  missing?: boolean;
  queries?: Array<{ isLoading: boolean; isError: boolean; error?: unknown }>;
  queryKeys?: Array<readonly unknown[]>;
}>) {
  const { pathname } = useLocation();
  const atBackDestination = pathname.replace(/\/+$/, "") === backTo.replace(/\/+$/, "");
  const [searchOpen, setSearchOpen] = useState(false);
  const searchId = useId();
  const searchButton = useRef<HTMLButtonElement>(null);
  const searchInput = useRef<HTMLInputElement>(null);
  const openPhoneNotepad = useContext(ManagerPhoneNotepadContext);
  const handleOpenNotepad = onOpenNotepad ?? openPhoneNotepad ?? undefined;
  useEffect(() => {
    if (searchOpen) searchInput.current?.focus();
  }, [searchOpen]);

  const client = useQueryClient();
  const loading = valid && queries.some(item => item.isLoading);
  const failed = queries.some(item => item.isError || item.error);
  const notFound = missing && queries.some(item => item.error instanceof ApiError && item.error.status === 404);
  return <div className={fill ? `${styles.page} ${styles.fill}` : styles.page}>
    {(title || onQueryChange || backTo !== "/") && (
      <header className={styles.toolbar}>
        <div className={styles.toolbarRow}>
          {atBackDestination ? (
            <button type="button" className={`${styles.toolbarButton} ${styles.backButton}`} disabled>
              <ChevronLeftIcon size={17} />{t("Back")}
            </button>
          ) : (
            <NavLink className={`${styles.toolbarButton} ${styles.backButton}`} to={backTo} replace>
              <ChevronLeftIcon size={17} />{t("Back")}
            </NavLink>
          )}
          {title && <h1 className={styles.toolbarTitle} title={title}>{title}</h1>}
          {onQueryChange && (
            <button ref={searchButton} type="button" className={`${styles.toolbarButton} ${styles.searchToggle}`}
              aria-label={t("Search")} title={t("Search")} aria-controls={searchId} aria-expanded={searchOpen}
              data-filtered={Boolean(query)} onClick={() => setSearchOpen(open => !open)}>
              <SearchIcon size={18} />
              <span className={styles.actionLabel}>{t("Search")}</span>
            </button>
          )}
          <div className={styles.notepadAction}>
            <button type="button" className={`${styles.toolbarButton} ${styles.notepadButton}`}
              aria-label={t("Notepad")} title={t("Notepad")} aria-haspopup="dialog"
              disabled={!handleOpenNotepad} onClick={event => {
                if (onOpenNotepad) onOpenNotepad();
                else openPhoneNotepad?.(event.currentTarget);
              }}>
              <NotepadIcon name="note" size={18} />
              <span className={styles.actionLabel}>{t("Notepad")}</span>
            </button>
          </div>
        </div>
        {onQueryChange && (
          <div id={searchId} className={styles.search} hidden={!searchOpen} onKeyDown={event => {
            if (event.key !== "Escape") return;
            event.preventDefault();
            setSearchOpen(false);
            searchButton.current?.focus();
          }}>
            <SearchIcon size={18} />
            <input ref={searchInput} aria-label={t("Search")} placeholder={t("Search")} type="search" value={query ?? ""}
              onChange={event => onQueryChange(event.target.value)} />
            {query && <button type="button" className={styles.clear} aria-label={t("Clear Search")} onClick={() => {
              onQueryChange("");
              searchInput.current?.focus();
            }}><CloseIcon size={18} /></button>}
          </div>
        )}
      </header>
    )}
    {!valid ? <ErrorBanner dismissible={false}>{t("Invalid record ID.")}</ErrorBanner>
      : notFound ? <p role="status">{t("Record not found.")}</p>
      : failed ? <><ErrorBanner dismissible={false}>{t("Could not load records. Please try again.")}</ErrorBanner><button className={styles.retry} onClick={() => queryKeys.forEach(queryKey => client.invalidateQueries({ queryKey }))}>{t("Retry")}</button></>
      : loading ? <p role="status">{t("Loading...")}</p>
      : missing ? <p role="status">{t("Record not found.")}</p>
      : children}
  </div>;
}
