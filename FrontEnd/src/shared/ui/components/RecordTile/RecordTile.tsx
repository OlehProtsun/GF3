import { t } from "@shared/i18n";
import type { KeyboardEvent, MouseEvent, ReactNode } from "react";
import { PinIcon } from "@shared/ui/icons";
import styles from "./RecordTile.module.css";

export type RecordTileMetaItem = {
  key?: string;
  label?: ReactNode;
  value: ReactNode;
};

type RecordTileProps = {
  title: ReactNode;
  description?: ReactNode;
  badge?: ReactNode;
  metaItems?: RecordTileMetaItem[];
  metaLayout?: "wrap" | "stacked";
  density?: "default" | "compact";
  headerSlot?: ReactNode;
  cornerSlot?: ReactNode;
  onClick?: () => void;
  isPinned?: boolean;
  isSelected?: boolean;
  onTogglePin?: () => void;
  pinLabel?: string;
  ariaLabel?: string;
  className?: string;
};

export function RecordTile({
  title,
  description,
  badge,
  metaItems = [],
  metaLayout = "wrap",
  density = "default",
  headerSlot,
  cornerSlot,
  onClick,
  isPinned = false,
  isSelected = false,
  onTogglePin,
  pinLabel = t("Toggle pin"),
  ariaLabel,
  className,
}: RecordTileProps) {
  const isInteractive = Boolean(onClick);
  const tileClassName = [
    styles.tile,
    density === "compact" ? styles.tileCompact : "",
    isInteractive ? styles.tileInteractive : "",
    isSelected ? styles.tileSelected : "",
    cornerSlot ? styles.tileWithCornerSlot : "",
    className ?? "",
  ]
    .filter(Boolean)
    .join(" ");

  const handleKeyDown = (event: KeyboardEvent<HTMLElement>) => {
    if (!onClick) {
      return;
    }

    if (event.key === "Enter" || event.key === " ") {
      event.preventDefault();
      onClick();
    }
  };

  const handlePinClick = (event: MouseEvent<HTMLButtonElement>) => {
    event.stopPropagation();
    onTogglePin?.();
  };

  const handlePinKeyDown = (event: KeyboardEvent<HTMLButtonElement>) => {
    if (event.key === "Enter" || event.key === " ") {
      event.stopPropagation();
    }
  };

  return (
    <article
      className={tileClassName}
      role={isInteractive ? "button" : undefined}
      tabIndex={isInteractive ? 0 : undefined}
      onClick={onClick}
      onKeyDown={handleKeyDown}
      aria-label={ariaLabel}
    >
      <div className={styles.head}>
        <div className={styles.titleWrap}>
          <div className={styles.title}>{title}</div>
        </div>

        {headerSlot || badge || onTogglePin ? (
          <div className={styles.headActions}>
            {headerSlot ? <div className={styles.headerSlot}>{headerSlot}</div> : null}
            {badge ? <span className={styles.badge}>{badge}</span> : null}
            {onTogglePin ? (
              <button
                type="button"
                className={[styles.pinButton, isPinned ? styles.pinButtonActive : ""]
                  .filter(Boolean)
                  .join(" ")}
                onClick={handlePinClick}
                onKeyDown={handlePinKeyDown}
                aria-label={pinLabel}
                aria-pressed={isPinned}
              >
                <PinIcon className={styles.pinIcon} size={15} />
              </button>
            ) : null}
          </div>
        ) : null}
      </div>

      {description ? <div className={styles.description}>{description}</div> : null}

      {metaItems.length > 0 ? (
        <div
          className={[
            styles.metaRow,
            metaLayout === "stacked" ? styles.metaRowStacked : "",
          ]
            .filter(Boolean)
            .join(" ")}
        >
          {metaItems.map((item, index) => (
            <span
              key={item.key ?? index}
              className={[
                styles.metaItem,
                metaLayout === "stacked" ? styles.metaItemStacked : "",
              ]
                .filter(Boolean)
                .join(" ")}
            >
              {item.label ? <span className={styles.metaLabel}>{item.label}</span> : null}
              <span className={styles.metaValue}>{item.value}</span>
            </span>
          ))}
        </div>
      ) : null}

      {cornerSlot ? <div className={styles.cornerSlot}>{cornerSlot}</div> : null}
    </article>
  );
}
