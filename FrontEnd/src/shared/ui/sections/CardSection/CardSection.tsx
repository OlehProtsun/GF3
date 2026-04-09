import type { CSSProperties, ReactNode, Ref } from "react";
import styles from "./CardSection.module.css";

type CardSectionProps = {
  icon?: ReactNode;
  title?: ReactNode;
  headerCenterSlot?: ReactNode;
  headerRightSlot?: ReactNode;
  children: ReactNode;
  className?: string;
  style?: CSSProperties;
  elementRef?: Ref<HTMLElement>;
};

export function CardSection({ icon, title, headerCenterSlot, headerRightSlot, children, className, style, elementRef }: CardSectionProps) {
  const cardClassName = className ? `${styles.card} ${className}` : styles.card;
  const headerClassName = headerCenterSlot ? `${styles.sectionHeader} ${styles.sectionHeaderCentered}` : styles.sectionHeader;

  return (
    <section ref={elementRef} className={cardClassName} style={style}>
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
