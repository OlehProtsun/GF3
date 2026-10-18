import type { ShiftSwapEmployee } from "@entities/shift-swaps/model/types";
import { t } from "@shared/i18n";
import { swapShiftDate, type SwapShiftSelection } from "./SwapShiftPickerDialog";
import styles from "./SwapRequestTypeDialog.module.css";

export function SwapRequestTypeDialog({ employee, kind, selection, onKindChange, onChooseShift, onSave }: {
  employee: ShiftSwapEmployee; kind: "give" | "exchange"; selection: SwapShiftSelection | null;
  onKindChange: (kind: "give" | "exchange") => void; onChooseShift: () => void; onSave: () => void; onBack: () => void;
}) {
  return <section className={styles.content} aria-label={t("Request type")}>
    <span className={styles.handle} aria-hidden="true" />
    <h2 tabIndex={-1} data-subview-heading>{t("Request type")}</h2>
    <p className={styles.description}>{t("Choose how you want to arrange the shift with {0}.", employee.displayName)}</p>
    <div className={styles.scroll}>
    <div className={styles.person}><span aria-hidden="true">{employee.firstName[0]}{employee.lastName[0]}</span><strong>{employee.displayName}</strong></div>
    <div role="radiogroup" aria-label={t("Request type")} className={styles.options} onKeyDown={event => {
      if (!["ArrowDown", "ArrowUp", "ArrowLeft", "ArrowRight"].includes(event.key)) return;
      event.preventDefault();
      const next = event.currentTarget.querySelector<HTMLButtonElement>('[aria-checked="false"]');
      next?.focus(); next?.click();
    }}>
      <button type="button" role="radio" tabIndex={kind === "give" ? 0 : -1} aria-checked={kind === "give"} aria-label={t("Give shift")} onClick={() => onKindChange("give")}><span className={styles.radio} aria-hidden="true" /><span><strong>{t("Give shift")}</strong><small>{t("You give your selected shift to this employee.")}</small></span></button>
      <button type="button" role="radio" tabIndex={kind === "exchange" ? 0 : -1} aria-checked={kind === "exchange"} aria-label={t("Swap shifts")} onClick={() => onKindChange("exchange")}><span className={styles.radio} aria-hidden="true" /><span><strong>{t("Swap shifts")}</strong><small>{t("You exchange shifts with this employee.")}</small></span></button>
    </div>
    {kind === "exchange" && <div className={styles.selection}>
      <h3>{t("Choose their shift")}</h3>
      <div className={styles.shiftCard}>
        <svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8"><rect x="4" y="5" width="16" height="16" rx="2" /><path d="M8 3v5m8-5v5M4 11h16" /></svg>
        {selection && <div><strong>{swapShiftDate(selection.shift)}</strong><span>{selection.period.fromTime} – {selection.period.toTime}</span></div>}
        <button type="button" onClick={onChooseShift}>{selection ? t("Change") : t("Choose their shift")}</button>
      </div>
      <p>{t("You can change this later if needed.")}</p>
    </div>}
    </div>
    <footer className={styles.footer}><button type="button" disabled={kind === "exchange" && !selection} onClick={onSave}>{t("Save selection")}</button></footer>
  </section>;
}
