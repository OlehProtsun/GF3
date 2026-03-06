import type { ReactNode } from "react";
import styles from "./ErrorBanner.module.css";

type ErrorBannerProps = {
  children: ReactNode;
  className?: string;
  textClassName?: string;
};

function withOptionalClass(baseClassName: string, className?: string) {
  return className ? `${baseClassName} ${className}` : baseClassName;
}

export function ErrorBanner({ children, className, textClassName }: ErrorBannerProps) {
  return (
    <div className={withOptionalClass(styles.wrap, className)}>
      <div className={styles.banner} role="alert" aria-live="polite">
        <svg className={styles.icon} viewBox="0 0 24 24" aria-hidden="true" focusable="false">
          <path d="M12 9v4M12 17h.01" />
          <path d="M10.29 3.86 2.17 17.92A2 2 0 0 0 3.9 21h16.2a2 2 0 0 0 1.73-3.08L13.71 3.86a2 2 0 0 0-3.42 0Z" />
        </svg>
        <span className={withOptionalClass(styles.text, textClassName)}>{children}</span>
      </div>
    </div>
  );
}
