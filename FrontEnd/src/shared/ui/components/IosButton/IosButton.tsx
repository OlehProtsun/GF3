import type { CSSProperties, ReactNode } from "react";
import styles from "./IosButton.module.css";

type IosButtonVariant = "primary" | "secondary";

type IosButtonProps = {
  label: string;
  onClick: () => void;
  icon?: ReactNode;
  variant?: IosButtonVariant;
  disabled?: boolean;
  className?: string;
  customColor?: string;
  customBorderColor?: string;
};

export function IosButton({
  label,
  onClick,
  icon,
  variant = "primary",
  disabled = false,
  className,
  customColor,
  customBorderColor,
}: IosButtonProps) {
  const classes = [styles.button, styles[variant], className].filter(Boolean).join(" ");
  const style = {
    ...(customColor ? { "--btn-bg": customColor } : {}),
    ...(customBorderColor ? { "--btn-border": customBorderColor } : {}),
  } as CSSProperties;

  return (
    <button type="button" className={classes} onClick={onClick} disabled={disabled} style={style}>
      {icon ? <span className={styles.icon}>{icon}</span> : null}
      <span>{label}</span>
    </button>
  );
}
