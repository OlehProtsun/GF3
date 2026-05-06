import { NavLink } from "react-router-dom";
import { useAuth } from "@app/providers/AuthProvider";
import { useEmployeeByIdQuery } from "@entities/employees/api/queries";
import { formatEmployeeLastLogin, getEmployeeFullName } from "@entities/employees/model/presentation";
import { ScheduleIcon } from "@shared/ui/icons";
import styles from "@pages/shared/EmployeeWorkspacePage.module.css";

export function EmployeeNotificationsPage() {
  const { session } = useAuth();
  const employeeQuery = useEmployeeByIdQuery(session?.employeeId ?? null);
  const employee = employeeQuery.data;
  const displayName = employee
    ? getEmployeeFullName(employee, session?.displayName ?? "Employee")
    : session?.displayName ?? session?.userName ?? "Employee";

  return (
    <div className={styles.page}>
      <section className={`${styles.hero} ${styles.heroMuted}`}>
        <div className={styles.heroContent}>
          <span className={styles.eyebrow}>Notifications</span>

          <div className={styles.heroRow}>
            <div className={styles.heroContent}>
              <h1 className={styles.heroTitle}>Updates will land here.</h1>
              <p className={styles.heroDescription}>
                This tab is reserved for shift changes, reminders and account notices, optimized for quick phone-first
                reading.
              </p>
            </div>

            <div className={styles.inlineStatus}>
              <span className={styles.miniBadge}>No alerts yet</span>
            </div>
          </div>
        </div>
      </section>

      <div className={`${styles.panelGrid} ${styles.panelGridTwo}`}>
        <section className={styles.panel}>
          <span className={styles.panelEyebrow}>What will appear here</span>
          <ul className={styles.list}>
            <li className={styles.listItem}>
              <span className={styles.listTitle}>Schedule changes</span>
              <span className={styles.listText}>Shift edits, newly published plans and urgent updates from the manager side.</span>
            </li>
            <li className={styles.listItem}>
              <span className={styles.listTitle}>Availability reminders</span>
              <span className={styles.listText}>A simple prompt when the manager opens or closes the submission window.</span>
            </li>
            <li className={styles.listItem}>
              <span className={styles.listTitle}>Account notices</span>
              <span className={styles.listText}>Future password recovery, security and profile-related messages in one place.</span>
            </li>
          </ul>
        </section>

        <section className={`${styles.panel} ${styles.panelAccent}`}>
          <span className={styles.panelEyebrow}>Current context</span>
          <div className={styles.detailsList}>
            <div className={styles.detailRow}>
              <span className={styles.detailLabel}>Employee</span>
              <span className={styles.detailValue}>{displayName}</span>
            </div>
            <div className={styles.detailRow}>
              <span className={styles.detailLabel}>Login</span>
              <span className={styles.detailValue}>@{session?.userName ?? "employee"}</span>
            </div>
            <div className={styles.detailRow}>
              <span className={styles.detailLabel}>Last login</span>
              <span className={styles.detailValue}>{formatEmployeeLastLogin(employee?.lastLoginAtUtc)}</span>
            </div>
          </div>
        </section>
      </div>

      <section className={styles.panel}>
        <span className={styles.panelEyebrow}>Next check</span>
        <NavLink to="/schedule" className={styles.linkCard}>
          <span className={styles.linkIcon}>
            <ScheduleIcon size={18} />
          </span>
          <span className={styles.linkTitle}>Open Schedule</span>
          <span className={styles.linkText}>When something new is published, this is where the ready-made plan will show up.</span>
        </NavLink>
      </section>
    </div>
  );
}
