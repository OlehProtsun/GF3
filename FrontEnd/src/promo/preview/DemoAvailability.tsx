import { Fragment } from "react";
import { availability, days, employees } from "../fixtures";
import styles from "./DemoPreviews.module.css";

export function DemoAvailability() {
  return <section className={styles.card} aria-label="Przykładowa dostępność zespołu">
    <header className={styles.header}><span className={styles.brand}>GF3</span><div><strong>Dostępność zespołu</strong><small>Październik · 12–18</small></div></header>
    <div className={styles.grid} data-promo-availability>
      <div className={styles.columnLabel}>Zespół</div>
      {days.map(day => <div key={day.id} className={styles.columnLabel}>{day.label}</div>)}
      {employees.map(employee => <Fragment key={employee.id}>
        <div className={styles.person}>{employee.name}</div>
        {days.map(day => <div className={styles.cell} key={day.id} data-promo-day={employee.id === "kasia" && day.id === "mon" ? "selected" : undefined}>
          {availability.find(entry => entry.employeeId === employee.id && entry.dayId === day.id)?.available
            ? <span className={styles.available} aria-label="Dostępność">✓</span>
            : <span className={styles.unavailable} aria-label="Niedostępność">—</span>}
        </div>)}
      </Fragment>)}
    </div>
    <footer className={styles.footer}><span className={styles.available}>✓</span>Dostępność<span className={styles.unavailable}>—</span>Niedostępność</footer>
  </section>;
}
