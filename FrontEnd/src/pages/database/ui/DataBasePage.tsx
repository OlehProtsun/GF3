import { PageHeader } from "@shared/ui/PageHeader";
import styles from "@pages/shared/PageStub.module.css";

export function DataBasePage() {
  return (
    <div>
      <PageHeader title="DataBase" subtitle="Database and application settings" backTo={-1} />
      <section className={styles.card}>TODO</section>
    </div>
  );
}
