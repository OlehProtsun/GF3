import { days, employees, shifts, workplace } from "../fixtures";
import styles from "./DemoPreviews.module.css";

export function DemoEmployeeSchedule() {
  const employee = employees[0];
  return <section className={`${styles.card} ${styles.employee}`} data-promo-employee-card aria-label="Przykładowy grafik Kasi">
    <header className={styles.header}><span className={styles.brand}>GF3</span><div><strong>Mój grafik</strong><small>{employee.name} · Październik</small></div><span className={styles.badge}>Opublikowany</span></header>
    <div className={styles.employeeIntro}>Twoje najbliższe zmiany</div>
    {shifts.filter(shift => shift.employeeId === employee.id).map(shift => {
      const day = days.find(day => day.id === shift.dayId)!;
      return <div className={styles.employeeShift} key={shift.id} data-promo-published={shift.id}>
        <div className={styles.date}><strong>{day.date}</strong>{day.label}</div>
        <div><strong>{shift.start}–{shift.end}</strong><small>{workplace}</small></div><span className={styles.shiftArrow}>↗</span>
      </div>;
    })}
    <footer className={styles.footer}>Wszystko, czego potrzebujesz na swój dzień.</footer>
  </section>;
}
