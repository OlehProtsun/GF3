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
  const collapsedRef = useRef<HTMLDivElement | null>(null);
  const [isCollapsed, setIsCollapsed] = useState(false);
  const [headerHeight, setHeaderHeight] = useState(0);
  const [collapsedHeight, setCollapsedHeight] = useState(0);

  useLayoutEffect(() => {
    const headerElement = headerRef.current;
    const collapsedElement = collapsedRef.current;
    if (!headerElement || !collapsedElement) return;

    const updateHeaderHeight = () => {
      const nextHeight = Math.ceil(headerElement.getBoundingClientRect().height);
      setHeaderHeight(prev => (prev !== nextHeight ? nextHeight : prev));
    };

    const updateCollapsedHeight = () => {
      const nextHeight = Math.ceil(collapsedElement.getBoundingClientRect().height);
      setCollapsedHeight(prev => (prev !== nextHeight ? nextHeight : prev));
    };

    updateHeaderHeight();
    updateCollapsedHeight();

    const headerObserver = new ResizeObserver(updateHeaderHeight);
    const collapsedObserver = new ResizeObserver(updateCollapsedHeight);
    headerObserver.observe(headerElement);
    collapsedObserver.observe(collapsedElement);

    return () => {
      headerObserver.disconnect();
      collapsedObserver.disconnect();
    };
  }, [eyebrow, title, subtitle, backTo, rightSlot, searchMeta, search, variant, fullBleed, gutter, className]);

  const headerClassName = [
    styles.header,
    styles.headerPanel,
    variant === "card" ? styles.variantCard : styles.variantPlain,
    fullBleed ? styles.fullBleed : "",
    isCollapsed ? styles.headerPanelCollapsed : "",
    className ?? "",
  ]
    .filter(Boolean)
    .join(" ");

  const collapsedDockClassName = [
    styles.collapsedDock,
    isCollapsed ? styles.collapsedDockVisible : styles.collapsedDockHidden,
  ]
    .filter(Boolean)
    .join(" ");

  const hasSearchCluster = Boolean(search || searchMeta);
  const spacerHeight = isCollapsed ? (collapsedHeight || 48) : headerHeight;

  return (
    <>
      <div className={styles.headerSpacer} style={{ height: `${spacerHeight}px` }} aria-hidden="true" />

      <div
        className={styles.headerRoot}
        style={{ "--page-header-gutter": `${gutter}px` } as CSSProperties}
      >
        <div className={styles.headerStack}>
          <header ref={headerRef} className={headerClassName} aria-hidden={isCollapsed}>
            <div className={styles.headerInner}>
              <div className={styles.headerRow}>
                <div className={styles.leadingGroup}>
                  <button
                    type="button"
                    className={styles.headerToggleButton}
                    onClick={() => setIsCollapsed(true)}
                    aria-label="Collapse header"
                    title="Collapse header"
                  >
                    <BackIcon className={`${styles.toggleIcon} ${styles.toggleIconUp}`} />
                  </button>

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
                </div>

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
          </header>

          <div ref={collapsedRef} className={collapsedDockClassName}>
            <button
              type="button"
              className={styles.collapsedButton}
              onClick={() => setIsCollapsed(false)}
              aria-label="Expand header"
              title="Expand header"
            >
              <BackIcon className={`${styles.toggleIcon} ${styles.toggleIconDown}`} />
              <span className={styles.collapsedTitle}>{title}</span>
            </button>
          </div>
        </div>
      </div>
    </>
  );
}
