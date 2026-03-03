import { PageHeader } from "@shared/ui/PageHeader";
import styles from "@pages/shared/PageStub.module.css";

export function ShopPage() {
  return (
    <div>
      <PageHeader title="Shop" subtitle="Manage shops and relations" backTo={-1} />
      <section className={styles.card}>TODO</section>
    </div>
  );
}
