import type { ReactNode } from "react";
import styles from "./FormLayout.module.css";

type LayoutProps = {
  children: ReactNode;
  className?: string;
};

function withOptionalClass(baseClassName: string, className?: string) {
  return className ? `${baseClassName} ${className}` : baseClassName;
}

export function FormGrid({ children, className }: LayoutProps) {
  return <div className={withOptionalClass(styles.formGrid, className)}>{children}</div>;
}

export function FormRow({ children, className }: LayoutProps) {
  return <div className={withOptionalClass(styles.formRow, className)}>{children}</div>;
}

export function FormActions({ children, className }: LayoutProps) {
  return <div className={withOptionalClass(styles.actions, className)}>{children}</div>;
}