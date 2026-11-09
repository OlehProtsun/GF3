import type { ButtonHTMLAttributes, CSSProperties, ReactNode } from "react";
import styles from "./IosButton.module.css";

type IosButtonVariant = "primary" | "secondary";
type IosButtonSize = "default" | "compact";

type IosButtonProps = {
  label: string;
  icon?: ReactNode;
  busy?: boolean;
  busyLabel?: string;
  variant?: IosButtonVariant;
  size?: IosButtonSize;
  disabled?: boolean;
  className?: string;
  customColor?: string;
  customBorderColor?: string;
  type?: ButtonHTMLAttributes<HTMLButtonElement>["type"];
  onClick?: () => void;
};

export function IosButton({
  label,
  onClick,
  icon,
  busy = false,
  busyLabel,
  variant = "primary",
  size = "default",
  disabled = false,
  className,
  customColor,
  customBorderColor,
  type = "button",
}: IosButtonProps) {
  const classes = [styles.button, styles[variant], styles[size], className].filter(Boolean).join(" ");
  const style = {
    ...(customColor ? { "--btn-bg": customColor } : {}),
    ...(customBorderColor ? { "--btn-border": customBorderColor } : {}),
  } as CSSProperties;

  return (
    <button type={type} className={classes} onClick={onClick} disabled={disabled || busy} aria-busy={busy || undefined} style={{
      ...style,
      paddingTop: "24.8px",
      paddingBottom: "24.8px",
      paddingLeft: "-31.4px",
      paddingRight: "-31.4px"
    }}>
      {icon || busy ? <span className={styles.icon} aria-hidden="true">{busy ? <span className={styles.spinner} /> : icon}</span> : null}
      {busyLabel ? (
        <span className={`${styles.label} ${styles.busyLabels}`} title={busy ? busyLabel : label}>
          <span aria-hidden={busy} className={busy ? styles.hiddenLabel : undefined}>{label}</span>
          <span aria-hidden={!busy} className={!busy ? styles.hiddenLabel : undefined}>{busyLabel}</span>
        </span>
      ) : <span className={styles.label} title={label}>{label}</span>}
    </button>
  );
}
