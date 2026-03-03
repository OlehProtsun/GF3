import type { CSSProperties, ReactNode } from "react";
import { useNavigate } from "react-router-dom";
import { BackIcon, SearchIcon } from "@shared/ui/icons";
import styles from "./PageHeader.module.css";

type PageHeaderProps = {
  title: string;
  subtitle?: string;
  backTo?: string | number;
  rightSlot?: ReactNode;
  search?: {
    value: string;
    onChange: (value: string) => void;
    placeholder?: string;
    ariaLabel?: string;
  };
  variant?: "card" | "plain";
  fullBleed?: boolean;
  gutter?: number;
  className?: string;
};

export function PageHeader({
  title,
  subtitle,
  backTo = -1,
  rightSlot,
  search,
  variant = "card",
  fullBleed = true,
  gutter = 14,
  className,
}: PageHeaderProps) {
  const navigate = useNavigate();

  const headerClassName = [
    styles.header,
    variant === "card" ? styles.variantCard : styles.variantPlain,
    fullBleed ? styles.fullBleed : "",
    className ?? "",
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <header className={headerClassName} style={{ "--page-header-gutter": `${gutter}px` } as CSSProperties}>
      <div className={styles.headerRow}>
        <div className={styles.content}>
          <button type="button" className={styles.backButton} onClick={() => navigate(backTo)}>
            <BackIcon className={styles.backIcon} />
            <span>Back</span>
          </button>
          <h1 className={styles.title}>{title}</h1>
          {subtitle ? <p className={styles.subtitle}>{subtitle}</p> : null}
        </div>

        {search ? (
          <label className={styles.searchPill}>
            <SearchIcon className={styles.searchIcon} />
            <input
              className={styles.searchInput}
              value={search.value}
              onChange={(event) => search.onChange(event.target.value)}
              placeholder={search.placeholder ?? "Search"}
              aria-label={search.ariaLabel ?? "Search"}
            />
          </label>
        ) : null}

        {rightSlot ? <div>{rightSlot}</div> : null}
      </div>
    </header>
  );
}
