import type { ButtonHTMLAttributes, CSSProperties, ReactNode } from "react";
import styles from "./IosButton.module.css";

type IosButtonVariant = "primary" | "secondary";

type IosButtonProps = {
  label: string;
  icon?: ReactNode;
  variant?: IosButtonVariant;
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
  variant = "primary",
  disabled = false,
  className,
  customColor,
  customBorderColor,
  type = "button",
}: IosButtonProps) {
  const classes = [styles.button, styles[variant], className].filter(Boolean).join(" ");
  const style = {
    ...(customColor ? { "--btn-bg": customColor } : {}),
    ...(customBorderColor ? { "--btn-border": customBorderColor } : {}),
  } as CSSProperties;

  return (
    <button type={type} className={classes} onClick={onClick} disabled={disabled} style={style}>
      {icon ? <span className={styles.icon}>{icon}</span> : null}
      <span>{label}</span>
    </button>
  );
}