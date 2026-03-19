import { PageHeader } from "@shared/ui/PageHeader";
import styles from "@pages/shared/PageStub.module.css";

export function HomePage() {
  return (
    <div>
      <PageHeader title="Home" subtitle="Main dashboard overview" backTo={-1} />
      <section className={styles.card}>TODO</section>
    </div>
  );
}
