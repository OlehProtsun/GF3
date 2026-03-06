import { useLayoutEffect, useRef, useState } from "react";
import type { CSSProperties, ReactNode } from "react";
import { useNavigate } from "react-router-dom";
import { BackIcon, SearchIcon } from "@shared/ui/icons";
import styles from "./PageHeader.module.css";

type PageHeaderProps = {
  eyebrow?: string;
  title: string;
  subtitle?: string;
  backTo?: string | number | null;
  backLabel?: string;
  rightSlot?: ReactNode;
  searchMeta?: ReactNode;
  search?: {
    value: string;
    onChange: (value: string) => void;
    onSubmit?: () => void;
    placeholder?: string;
    ariaLabel?: string;
    buttonLabel?: string;
  };
  variant?: "card" | "plain";
  fullBleed?: boolean;
  gutter?: number;
  className?: string;
};

export function PageHeader({
  eyebrow,
  title,
  subtitle,
  backTo = null,
  backLabel = "Back",
  rightSlot,
  searchMeta,
  search,
  variant = "card",
  fullBleed = true,
  gutter = 14,
  className,
}: PageHeaderProps) {
  const navigate = useNavigate();
  const headerRef = useRef<HTMLElement | null>(null);
  const [headerHeight, setHeaderHeight] = useState(0);

  useLayoutEffect(() => {
    const headerElement = headerRef.current;
    if (!headerElement) return;

    const updateHeight = () => {
      const nextHeight = Math.ceil(headerElement.getBoundingClientRect().height);
      setHeaderHeight(prev => (prev !== nextHeight ? nextHeight : prev));
    };

    updateHeight();

    const observer = new ResizeObserver(updateHeight);
    observer.observe(headerElement);

    return () => {
      observer.disconnect();
    };
  }, [eyebrow, title, subtitle, backTo, rightSlot, searchMeta, search, variant, fullBleed, gutter]);

  const headerClassName = [
    styles.header,
    variant === "card" ? styles.variantCard : styles.variantPlain,
    fullBleed ? styles.fullBleed : "",
    className ?? "",
  ]
    .filter(Boolean)
    .join(" ");

  const hasSearchCluster = Boolean(search || searchMeta);

  return (
    <>
      <div className={styles.headerSpacer} style={{ height: `${headerHeight}px` }} aria-hidden="true" />

      <header
        ref={headerRef}
        className={styles.headerRoot}
        style={{ "--page-header-gutter": `${gutter}px` } as CSSProperties}
      >
        <div className={headerClassName}>
          <div className={styles.headerInner}>
            <div className={styles.headerRow}>
              {backTo !== null && backTo !== undefined ? (
                <div className={styles.backRow}>
                  <button
                    type="button"
                    className={styles.backButton}
                    onClick={() => navigate(backTo)}
                  >
                    <BackIcon className={styles.backIcon} />
                    <span>{backLabel}</span>
                  </button>
                </div>
              ) : null}

              <div className={styles.titleBlock}>
                {eyebrow ? <span className={styles.eyebrow}>{eyebrow}</span> : null}
                <h1 className={styles.title}>{title}</h1>
                {subtitle ? <p className={styles.subtitle}>{subtitle}</p> : null}
              </div>

              {hasSearchCluster ? (
                <div className={styles.utilityGroup}>
                  {search ? (
                    <form
                      className={styles.searchForm}
                      role="search"
                      onSubmit={event => {
                        event.preventDefault();
                        search.onSubmit?.();
                      }}
                    >
                      <div className={styles.searchField}>
                        <SearchIcon className={styles.searchIcon} />
                        <input
                          className={styles.searchInput}
                          value={search.value}
                          onChange={event => search.onChange(event.target.value)}
                          placeholder={search.placeholder ?? "Search"}
                          aria-label={search.ariaLabel ?? "Search"}
                        />
                      </div>
                    </form>
                  ) : null}

                  {searchMeta ? <div className={styles.meta}>{searchMeta}</div> : null}
                </div>
              ) : null}

              {rightSlot ? <div className={styles.actions}>{rightSlot}</div> : null}
            </div>
          </div>
        </div>
      </header>
    </>
  );
}
