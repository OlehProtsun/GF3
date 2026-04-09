import styles from "./RouteFallback.module.css";

export function RouteFallback() {
  return (
    <div className={styles.shell} role="status" aria-live="polite" aria-label="Loading page">
      <div className={styles.card}>
        <span className={styles.dots} aria-hidden="true">
          <span className={styles.dot} />
          <span className={styles.dot} />
          <span className={styles.dot} />
        </span>
        <span className={styles.label}>Loading page</span>
      </div>
    </div>
  );
}
