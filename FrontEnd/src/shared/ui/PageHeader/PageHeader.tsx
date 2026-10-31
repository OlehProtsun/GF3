import { t } from "@shared/i18n";
import { useEffect, useLayoutEffect, useRef, useState } from "react";
import type { CSSProperties, ReactNode } from "react";
import { useNavigate } from "react-router-dom";
import { BackIcon, SearchIcon } from "@shared/ui/icons";
import styles from "./PageHeader.module.css";

const COLLAPSED_SPACER_HEIGHT = 16;

type PageHeaderProps = {
  eyebrow?: string;
  title: string;
  subtitle?: string;
  backTo?: string | number | null;
  onBack?: () => void;
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
  maxWidth?: string;
  className?: string;
  onCollapseChange?: (isCollapsed: boolean) => void;
};

export function PageHeader({
  eyebrow,
  title,
  subtitle,
  backTo = null,
  onBack,
  backLabel = t("Back"),
  rightSlot,
  searchMeta,
  search,
  variant = "card",
  fullBleed = true,
  gutter = 14,
  maxWidth = "1100px",
  className,
  onCollapseChange,
}: PageHeaderProps) {
  const navigate = useNavigate();
  const headerRef = useRef<HTMLElement | null>(null);
  const [isCollapsed, setIsCollapsed] = useState(false);
  const [headerHeight, setHeaderHeight] = useState(0);

  useLayoutEffect(() => {
    const headerElement = headerRef.current;
    if (!headerElement) {
      return;
    }

    const updateHeaderHeight = () => {
      const nextHeight = Math.ceil(headerElement.getBoundingClientRect().height);
      setHeaderHeight(prev => (prev !== nextHeight ? nextHeight : prev));
    };

    updateHeaderHeight();

    const headerObserver = new ResizeObserver(updateHeaderHeight);
    headerObserver.observe(headerElement);

    return () => {
      headerObserver.disconnect();
    };
  }, [eyebrow, title, subtitle, backTo, onBack, rightSlot, searchMeta, search, variant, fullBleed, gutter, className]);

  useEffect(() => {
    onCollapseChange?.(isCollapsed);
  }, [isCollapsed, onCollapseChange]);

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
  const spacerHeight = isCollapsed ? COLLAPSED_SPACER_HEIGHT : headerHeight;
  const hasBackAction = onBack != null || (backTo !== null && backTo !== undefined);

  const handleBackClick = () => {
    if (onBack) {
      onBack();
      return;
    }

    if (backTo !== null && backTo !== undefined) {
      navigate(backTo);
    }
  };

  return (
    <>
      <div className={styles.headerSpacer} style={{ height: `${spacerHeight}px` }} aria-hidden="true" />

      <div
        className={styles.headerRoot}
        style={{
          "--page-header-gutter": `${gutter}px`,
          "--page-header-max-width": maxWidth,
        } as CSSProperties}
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
                    aria-label={t("Collapse header")}
                    title={t("Collapse header")}
                  >
                    <BackIcon className={`${styles.toggleIcon} ${styles.toggleIconUp}`} />
                  </button>

                  {hasBackAction ? (
                    <div className={styles.backRow}>
                      <button
                        type="button"
                        className={styles.backButton}
                        onClick={handleBackClick}
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
                            placeholder={search.placeholder ?? t("Search")}
                            aria-label={search.ariaLabel ?? t("Search")}
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

          <div className={collapsedDockClassName}>
            <button
              type="button"
              className={styles.collapsedButton}
              onClick={() => setIsCollapsed(false)}
              aria-label={t("Expand header")}
              title={t("Expand header")}
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
