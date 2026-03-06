import type { ReactNode } from "react";
import styles from "./RecordGrid.module.css";

type RecordGridProps = {
  children: ReactNode;
  className?: string;
};

export function RecordGrid({ children, className }: RecordGridProps) {
  const gridClassName = [styles.grid, className ?? ""].filter(Boolean).join(" ");

  return <div className={gridClassName}>{children}</div>;
}
