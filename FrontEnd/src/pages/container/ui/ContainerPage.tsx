import { PageHeader } from "@shared/ui/PageHeader";
import styles from "@pages/shared/PageStub.module.css";

export function ContainerPage() {
  return (
    <div>
      <PageHeader title="Container" subtitle="Container matrix and bindings" backTo={-1} />
      <section className={styles.card}>TODO</section>
    </div>
  );
}
