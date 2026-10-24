import { t } from "@shared/i18n";
import { useAuth } from "@app/providers/AuthProvider";
import { useEmployeeByIdQuery } from "@entities/employees/api/queries";
import { getEmployeeFullName } from "@entities/employees/model/presentation";
import styles from "@pages/shared/EmployeeWorkspacePage.module.css";

export function EmployeeHomePage() {
  const { session } = useAuth();
  const employeeQuery = useEmployeeByIdQuery(session?.employeeId ?? null);
  const employee = employeeQuery.data;
  const displayName = employee
    ? getEmployeeFullName(employee, session?.displayName ?? t("Employee"))
    : session?.displayName ?? session?.userName ?? t("Employee");
  const firstName = employee?.firstName ?? displayName.split(/\s+/)[0] ?? "there";

  return (
    <div className={styles.page}>
      <section className={styles.hero}>
        <div className={styles.heroContent}>
          <h1 className={styles.heroTitle}>{t("Hello,")} {firstName}.</h1>
        </div>
      </section>
    </div>
  );
}
