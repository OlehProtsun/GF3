import { useEffect, useLayoutEffect, useMemo, useRef } from "react";
import type { EmployeeAvailabilityGroup } from "@entities/employee-availability";
import { getAvailabilityCellKey, getAvailabilityWeekdayLabel, getDaysInMonth, isAvailabilityWeekend } from "@entities/availability-groups/model/editor";
import { getAvailabilityMonthLabel } from "@entities/availability-groups/model/presentation";
import { t } from "@shared/i18n";
import { getPolishPublicHoliday } from "@shared/lib/polishPublicHolidays";
import { usePublicHolidayVisibility } from "@app/providers/PublicHolidayVisibilityProvider";
import { buildEmployeeAvailabilityCells, buildEmployeeAvailabilityColumns, getContiguousAvailabilityDays, type DayDraftMap } from "../model/employeeAvailabilityMatrix";
import styles from "./EmployeeAvailabilityPage.module.css";
import { isoDate, vacationShopNames, type UrlopEmployeeCalendar } from "@entities/urlop";

type Props = {
  urlopCalendar?: UrlopEmployeeCalendar;
  group: EmployeeAvailabilityGroup;
  currentEmployeeId: number;
  ownDraft: DayDraftMap;
  canEdit: boolean;
  selectedDays: readonly number[];
  onSelectedDaysChange: (days: number[]) => void;
  onRequestEdit: (days: number[]) => void;
};

export function EmployeeAvailabilityMatrix(props: Props) {
  const showPublicHolidays = usePublicHolidayVisibility();
  const { group, currentEmployeeId, ownDraft, canEdit, selectedDays } = props;
  const viewportRef = useRef<HTMLDivElement>(null);
  const callbacks = useRef(props);
  useLayoutEffect(() => { callbacks.current = props; });
  const suppressClick = useRef(false);
  const columns = useMemo(() => buildEmployeeAvailabilityColumns(group.members ?? [], currentEmployeeId), [group.members, currentEmployeeId]);
  const cells = useMemo(() => buildEmployeeAvailabilityCells(columns, group.year, group.month), [columns, group.year, group.month]);
  const daysInMonth = getDaysInMonth(group.year, group.month);
  const hasOwnColumn = columns.some(member => member.employeeId === currentEmployeeId);
  const enabled = canEdit && hasOwnColumn;

  useEffect(() => {
    const viewport = viewportRef.current;
    if (!viewport || !enabled) return;
    let press: { id: number; anchor: number; last: number; x: number; y: number; startX: number; startY: number; active: boolean; button: HTMLButtonElement } | null = null;
    let timer: ReturnType<typeof setTimeout> | undefined;
    let frame = 0;
    const clear = (clearHighlight: boolean) => {
      clearTimeout(timer);
      cancelAnimationFrame(frame);
      if (press?.button.hasPointerCapture?.(press.id)) press.button.releasePointerCapture(press.id);
      press = null;
      if (clearHighlight) callbacks.current.onSelectedDaysChange([]);
    };
    const hitDay = () => {
      if (!press) return;
      const rect = viewport.getBoundingClientRect();
      if (press.x < rect.left || press.x > rect.right) return;
      const header = viewport.querySelector("thead")?.getBoundingClientRect();
      const y = Math.max((header?.bottom ?? rect.top) + 1, Math.min(rect.bottom - 1, press.y));
      const row = document.elementFromPoint?.(press.x, y)?.closest<HTMLElement>("[data-availability-day]");
      if (!row || !viewport.contains(row)) return;
      const day = Number(row.dataset.availabilityDay);
      if (!Number.isInteger(day) || day < 1 || day > daysInMonth) return;
      if (press.last === day) return;
      press.last = day;
      callbacks.current.onSelectedDaysChange(getContiguousAvailabilityDays(press.anchor, day, daysInMonth));
    };
    const scroll = () => {
      frame = 0;
      if (!press?.active) return;
      const rect = viewport.getBoundingClientRect();
      const top = viewport.querySelector("thead")?.getBoundingClientRect().bottom ?? rect.top;
      if (press.x < rect.left || press.x > rect.right) return;
      const delta = press.y < top + 32 ? -8 : press.y > rect.bottom - 32 ? 8 : 0;
      const previousTop = viewport.scrollTop;
      if (delta) viewport.scrollTop = Math.max(0, Math.min(viewport.scrollHeight - viewport.clientHeight, previousTop + delta));
      hitDay();
      if (delta && previousTop !== viewport.scrollTop) frame = requestAnimationFrame(scroll);
    };
    const down = (event: PointerEvent) => {
      if (press) { suppressClick.current = true; clear(true); return; }
      const button = (event.target as Element).closest<HTMLButtonElement>("button[data-own-availability-cell]");
      if (!button || button.disabled || event.button !== 0 || event.isPrimary === false) return;
      suppressClick.current = false;
      const day = Number(button.dataset.ownAvailabilityCell);
      press = { id: event.pointerId, anchor: day, last: day, x: event.clientX, y: event.clientY, startX: event.clientX, startY: event.clientY, active: false, button };
      timer = setTimeout(() => {
        if (!press) return;
        press.active = true;
        suppressClick.current = true;
        press.button.setPointerCapture?.(press.id);
        callbacks.current.onSelectedDaysChange([press.anchor]);
        frame = requestAnimationFrame(scroll);
      }, 350);
    };
    const move = (event: PointerEvent) => {
      if (!press || event.pointerId !== press.id) return;
      press.x = event.clientX; press.y = event.clientY;
      if (!press.active && Math.hypot(press.x - press.startX, press.y - press.startY) > 8) {
        suppressClick.current = true;
        clear(true);
      } else if (press.active) {
        event.preventDefault();
        hitDay();
        if (!frame) frame = requestAnimationFrame(scroll);
      }
    };
    const up = (event: PointerEvent) => {
      if (!press || event.pointerId !== press.id) return;
      const range = press.active ? getContiguousAvailabilityDays(press.anchor, press.last, daysInMonth) : null;
      clear(false);
      if (range) callbacks.current.onRequestEdit(range);
    };
    const cancel = (event: PointerEvent) => {
      if (press && event.pointerId === press.id) { suppressClick.current = true; clear(true); }
    };
    const escape = (event: KeyboardEvent) => {
      if (event.key === "Escape" && press) { suppressClick.current = true; clear(true); }
    };
    viewport.addEventListener("pointerdown", down);
    window.addEventListener("pointermove", move, { passive: false });
    window.addEventListener("pointerup", up);
    window.addEventListener("pointercancel", cancel);
    window.addEventListener("keydown", escape);
    return () => {
      clear(false);
      viewport.removeEventListener("pointerdown", down);
      window.removeEventListener("pointermove", move);
      window.removeEventListener("pointerup", up);
      window.removeEventListener("pointercancel", cancel);
      window.removeEventListener("keydown", escape);
    };
  }, [group.id, enabled, daysInMonth]);

  if (!columns.length) return <p role="status">{t("Team availability could not be shown.")}</p>;
  return <div className={styles.matrixViewport} ref={viewportRef} role="region" aria-label={t("Team availability")} tabIndex={0}>
    <table className={styles.matrix}>
      <thead><tr><th scope="col" className={styles.dateColumn}>{t("Day")}</th>
        {columns.map(member => {
          const own = member.employeeId === currentEmployeeId;
          const name = [member.firstName, member.lastName].filter(Boolean).join(" ").trim() || t("Employee #{0}", member.employeeId);
          const initials = [member.firstName, member.lastName].filter(Boolean).map(part => part[0]).join("") || String(member.employeeId);
          return <th key={member.employeeId} scope="col" className={own ? styles.ownColumn : undefined} title={name} aria-label={own ? `${name} (${t("You")})` : name}>
            <span className={styles.initials} aria-hidden="true">{initials}</span>
            <span className={styles.memberName}>{name}</span>{own ? <small>{t("You")}</small> : null}
          </th>;
        })}
      </tr></thead>
      <tbody>{Array.from({ length: daysInMonth }, (_, index) => index + 1).map(day => {
        const holiday = showPublicHolidays ? getPolishPublicHoliday(group.year, group.month, day) : null;
        return <tr key={day} data-availability-day={day} className={isAvailabilityWeekend(group.year, group.month, day) ? styles.weekend : undefined}>
        <th scope="row" className={[styles.dateColumn, holiday ? styles.holidayDay : ""].join(" ")} title={holiday ? t(holiday.name) : undefined}>
          <strong>{day}</strong><small>{getAvailabilityWeekdayLabel(group.year, group.month, day)}</small>
          {holiday ? <small className={styles.holidayLabel}>{t("Holiday")}</small> : null}
        </th>
        {columns.map(member => {
          const own = member.employeeId === currentEmployeeId;
          const draft = ownDraft[day];
          const value = own ? (draft?.available ? draft.intervalStr ?? "+" : "-") : cells[getAvailabilityCellKey(member.employeeId, day)] ?? "-";
          const shops = vacationShopNames(props.urlopCalendar?.months.flatMap(x => x.approved) ?? [], props.urlopCalendar?.shops ?? [], member.employeeId, isoDate(group.year, group.month, day));
          const display = shops.length ? "UR" : value === "-" ? "−" : value;
          const title = shops.length ? `UR (${shops.join(", ")})` : display;
          const valueClass = shops.length ? styles.urlopCell : value === "+" ? styles.wholeDay : value === "-" ? styles.unavailable : styles.timeValue;
          return <td key={member.employeeId} className={[own ? styles.ownColumn : "", own && selectedDays.includes(day) ? styles.selectedCell : ""].join(" ")}>
            {own ? <button type="button" data-own-availability-cell={day} className={`${styles.ownCell} ${valueClass}`} disabled={!enabled}
              title={title} aria-label={t("{0} {1}, your availability: {2}", day, getAvailabilityMonthLabel(group.month), title)}
              onContextMenu={event => { if (enabled) event.preventDefault(); }}
              onClick={event => {
                if (!enabled) return;
                if (suppressClick.current && event.detail !== 0) { suppressClick.current = false; return; }
                callbacks.current.onSelectedDaysChange([day]);
                callbacks.current.onRequestEdit([day]);
              }}>{display}</button> : <span className={`${styles.teamCell} ${valueClass}`} title={title}>{display}</span>}
          </td>;
        })}
      </tr>;
      })}</tbody>
    </table>
  </div>;
}
