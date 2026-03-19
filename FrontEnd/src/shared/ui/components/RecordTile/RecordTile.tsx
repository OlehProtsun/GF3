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
  onClick?: () => void;
  isPinned?: boolean;
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
  onClick,
  isPinned = false,
  onTogglePin,
  pinLabel = "Toggle pin",
  ariaLabel,
  className,
}: RecordTileProps) {
  const isInteractive = Boolean(onClick);
  const tileClassName = [
    styles.tile,
    isInteractive ? styles.tileInteractive : "",
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

        {badge || onTogglePin ? (
          <div className={styles.headActions}>
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
        <div className={styles.metaRow}>
          {metaItems.map((item, index) => (
            <span key={item.key ?? index} className={styles.metaItem}>
              {item.label ? <span className={styles.metaLabel}>{item.label}</span> : null}
              <span className={styles.metaValue}>{item.value}</span>
            </span>
          ))}
        </div>
      ) : null}
    </article>
  );
}
