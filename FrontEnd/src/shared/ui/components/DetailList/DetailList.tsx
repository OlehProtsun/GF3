import type { ReactNode } from "react";
import styles from "./DetailList.module.css";

type DetailListProps = {
  children: ReactNode;
  columns?: 1 | 2 | 3;
  className?: string;
};

type DetailItemProps = {
  label: ReactNode;
  value: ReactNode;
  className?: string;
  valueClassName?: string;
};

function withOptionalClass(baseClassName: string, className?: string) {
  return className ? `${baseClassName} ${className}` : baseClassName;
}

export function DetailList({ children, columns = 2, className }: DetailListProps) {
  const columnClassName =
    columns === 1 ? styles.columns1 : columns === 3 ? styles.columns3 : styles.columns2;

  return <div className={withOptionalClass(`${styles.list} ${columnClassName}`, className)}>{children}</div>;
}

export function DetailItem({ label, value, className, valueClassName }: DetailItemProps) {
  return (
    <article className={withOptionalClass(styles.item, className)}>
      <span className={styles.label}>{label}</span>
      <div className={withOptionalClass(styles.value, valueClassName)}>{value}</div>
    </article>
  );
}
