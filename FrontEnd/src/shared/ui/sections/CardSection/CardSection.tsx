import type { CSSProperties, ReactNode } from "react";
import styles from "./CardSection.module.css";

type CardSectionProps = {
  icon?: ReactNode;
  title?: ReactNode;
  headerCenterSlot?: ReactNode;
  headerRightSlot?: ReactNode;
  children: ReactNode;
  className?: string;
  style?: CSSProperties;
};

export function CardSection({ icon, title, headerCenterSlot, headerRightSlot, children, className, style }: CardSectionProps) {
  const cardClassName = className ? `${styles.card} ${className}` : styles.card;
  const headerClassName = headerCenterSlot ? `${styles.sectionHeader} ${styles.sectionHeaderCentered}` : styles.sectionHeader;

  return (
    <section className={cardClassName} style={style}>
      {icon || title || headerCenterSlot || headerRightSlot ? (
        <div className={headerClassName}>
          <div className={styles.sectionTitle}>
            {icon}
            {title ? <span>{title}</span> : null}
          </div>

          {headerCenterSlot ? <div className={styles.headerCenter}>{headerCenterSlot}</div> : null}
          {headerRightSlot ? <div className={styles.headerRight}>{headerRightSlot}</div> : null}
        </div>
      ) : null}

      {children}
    </section>
  );
}
