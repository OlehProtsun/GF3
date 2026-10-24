import { Fragment } from "react";
import { days, employees, month, shifts, workplace } from "../fixtures";
import styles from "./DemoPreviews.module.css";

export function DemoWorkspace() {
  return <section className={styles.card} aria-label="Przykładowy grafik zespołu">
    <header className={styles.header}><span className={styles.brand}>GF3</span><div><strong>Grafik zespołu</strong><small>{workplace} · {month}</small></div><span className={styles.badge}>Tydzień</span></header>
    <div className={styles.grid} data-promo-grid>
      <div className={styles.columnLabel}>Zespół</div>
      {days.map(day => <div className={styles.columnLabel} key={day.id}>{day.label}<small>{day.date}</small></div>)}
      {employees.map(employee => <Fragment key={employee.id}>
        <div className={styles.person}><span className={styles.avatar}>{employee.initials}</span>{employee.name}</div>
        {days.map(day => <div className={styles.cell} key={day.id}>
          {shifts.filter(shift => shift.employeeId === employee.id && shift.dayId === day.id).map(shift =>
            <span className={styles.shift} key={shift.id} data-promo-shift={shift.id}>{shift.start}<br />{shift.end}</span>)}
        </div>)}
      </Fragment>)}
    </div>
    <footer className={styles.footer}><span className={styles.dot} />Plan zmian w jednym widoku<span>12–18 października</span></footer>
  </section>;
}
