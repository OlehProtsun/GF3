import { ReactNode, useState } from "react";
import styles from "./OverlaySidebarLayout.module.css";

type OverlaySidebarLayoutProps = {
  children: ReactNode;
};

export function OverlaySidebarLayout({ children }: OverlaySidebarLayoutProps) {
  const [isCollapsed, setIsCollapsed] = useState(false);

  return (
    <div className={styles.layout}>
      <aside className={`${styles.sidebar} ${isCollapsed ? styles.collapsed : styles.expanded}`}>
        <div className={styles.handle}>
          <button
            type="button"
            className={styles.controlButton}
            onClick={() => setIsCollapsed((value) => !value)}
          >
            {isCollapsed ? "Show" : "Hide"}
          </button>
        </div>

        <div className={styles.buttonList}>
          <button type="button" className={styles.controlButton}>
            Button 1
          </button>
          <button type="button" className={styles.controlButton}>
            Button 2
          </button>
          <button type="button" className={styles.controlButton}>
            Button 3
          </button>
          <button type="button" className={styles.controlButton}>
            Button 4
          </button>
        </div>

        {!isCollapsed && (
          <button
            type="button"
            className={`${styles.controlButton} ${styles.hideButton}`}
            onClick={() => setIsCollapsed(true)}
          >
            Hide
          </button>
        )}
      </aside>

      <main className={styles.content}>{children}</main>
    </div>
  );
}
