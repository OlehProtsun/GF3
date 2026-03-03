import { PageHeader } from "@shared/ui/PageHeader";
import styles from "@pages/shared/PageStub.module.css";

export function InformationPage() {
  return (
    <div>
      <PageHeader title="Information" subtitle="Service and diagnostics information" backTo={-1} />
      <section className={styles.card}>TODO</section>
    </div>
  );
}
