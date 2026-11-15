import { days, employees, shifts, swapOffer } from "../fixtures";
import styles from "./DemoPreviews.module.css";

export function DemoSwap() {
  const shift = shifts.find(shift => shift.id === swapOffer.shiftId)!;
  const owner = employees.find(employee => employee.id === shift.employeeId)!;
  const candidate = employees.find(employee => employee.id === swapOffer.candidateId)!;
  const day = days.find(day => day.id === shift.dayId)!;
  return <section className={`${styles.card} ${styles.swap}`} aria-label="Przykładowa prośba o zamianę">
    <header className={styles.header}><span className={styles.brand}>GF3</span><div><strong>Zamiany zmian</strong><small>Jeden widok. Jasny status.</small></div></header>
    <div className={styles.swapStack}>
      <div className={styles.offer} data-promo-swap-offer>
        <span className={styles.badge}>Prośba o zamianę</span><h3>{owner.name} szuka zamiany</h3>
        <p>{day.label}, {day.date} października</p><strong className={styles.time}>{shift.start}–{shift.end}</strong>
        <div className={styles.candidate} data-promo-swap-candidate><span className={styles.avatar}>{candidate.initials}</span><div><strong>{candidate.name}</strong><small>Potwierdza przyjęcie zmiany</small></div><span>→</span></div>
      </div>
      <div className={styles.accepted} data-promo-swap-accepted><span className={styles.check}>✓</span><span className={styles.badge}>Zaakceptowana</span><h3>Zamiana potwierdzona</h3><p>{owner.name} → {candidate.name}</p><strong className={styles.time}>{shift.start}–{shift.end}</strong><small>{day.label}, {day.date} października · Sklep Centrum</small></div>
    </div>
  </section>;
}
