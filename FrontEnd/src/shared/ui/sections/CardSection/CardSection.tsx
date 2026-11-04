import type { CSSProperties, ReactNode, Ref } from "react";
import styles from "./CardSection.module.css";

type CardSectionProps = {
  icon?: ReactNode;
  title?: ReactNode;
  headerCenterSlot?: ReactNode;
  headerRightSlot?: ReactNode;
  children: ReactNode;
  className?: string;
  headerClassName?: string;
  titleClassName?: string;
  headerRightClassName?: string;
  style?: CSSProperties;
  elementRef?: Ref<HTMLElement>;
};

export function CardSection({
  icon,
  title,
  headerCenterSlot,
  headerRightSlot,
  children,
  className,
  headerClassName,
  titleClassName,
  headerRightClassName,
  style,
  elementRef,
}: CardSectionProps) {
  const cardClassName = className ? `${styles.card} ${className}` : styles.card;
  const resolvedHeaderClassName = [
    styles.sectionHeader,
    headerCenterSlot ? styles.sectionHeaderCentered : "",
    headerClassName ?? "",
  ].filter(Boolean).join(" ");
  const resolvedTitleClassName = titleClassName ? `${styles.sectionTitle} ${titleClassName}` : styles.sectionTitle;
  const resolvedHeaderRightClassName = headerRightClassName
    ? `${styles.headerRight} ${headerRightClassName}`
    : styles.headerRight;

  return (
    <section ref={elementRef} className={cardClassName} style={style}>
      {icon || title || headerCenterSlot || headerRightSlot ? (
        <div className={resolvedHeaderClassName}>
          <div className={resolvedTitleClassName}>
            {icon}
            {title ? <span>{title}</span> : null}
          </div>

          {headerCenterSlot ? <div className={styles.headerCenter}>{headerCenterSlot}</div> : null}
          {headerRightSlot ? <div className={resolvedHeaderRightClassName}>{headerRightSlot}</div> : null}
        </div>
      ) : null}

      {children}
    </section>
  );
}
