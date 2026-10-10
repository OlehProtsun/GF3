import { t } from "@shared/i18n";
import styles from "./HomePage.module.css";

export function HomePage({ phoneMode = false }: { phoneMode?: boolean }) {
  return <div className={`${styles.page} ${phoneMode ? styles.pagePhone : ""}`}>
    <h1 className={styles.title}>{t("Coming Soon")}</h1>
  </div>;
}
