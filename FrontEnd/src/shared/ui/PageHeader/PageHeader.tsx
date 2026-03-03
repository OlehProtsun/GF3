import type { ReactNode } from "react";
import { useNavigate } from "react-router-dom";
import styles from "./PageHeader.module.css";

type PageHeaderProps = {
  title: string;
  subtitle: string;
  backTo?: string | number;
  rightSlot?: ReactNode;
};

export function PageHeader({ title, subtitle, backTo = -1, rightSlot }: PageHeaderProps) {
  const navigate = useNavigate();

  return (
    <header className={styles.header}>
      <div>
        <button type="button" className={styles.backButton} onClick={() => navigate(backTo)}>
          Back
        </button>
        <h1 className={styles.title}>{title}</h1>
        <p className={styles.subtitle}>{subtitle}</p>
      </div>
      {rightSlot ? <div>{rightSlot}</div> : null}
    </header>
  );
}
