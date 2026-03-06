import type { ReactNode } from "react";
import styles from "./CardSection.module.css";

type CardSectionProps = {
  icon?: ReactNode;
  title?: ReactNode;
  headerRightSlot?: ReactNode;
  children: ReactNode;
  className?: string;
};

export function CardSection({ icon, title, headerRightSlot, children, className }: CardSectionProps) {
  const cardClassName = className ? `${styles.card} ${className}` : styles.card;

  return (
    <section className={cardClassName}>
      {icon || title || headerRightSlot ? (
        <div className={styles.sectionHeader}>
          <div className={styles.sectionTitle}>
            {icon}
            {title ? <span>{title}</span> : null}
          </div>

          {headerRightSlot ? <div className={styles.headerRight}>{headerRightSlot}</div> : null}
        </div>
      ) : null}

      {children}
    </section>
  );
}
