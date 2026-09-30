import { useEffect, useState } from "react";
import type { EmployeeSchedule } from "@entities/employee-schedule";
import { dateTimeFormat, t } from "@shared/i18n";
import { getHeroShifts } from "./scheduleHeroShifts";
import styles from "./EmployeeScheduleHero.module.css";

type Props = {
  schedules: EmployeeSchedule[];
  employeeId: number | null;
  displayName: string;
  isLoading: boolean;
  hasError: boolean;
};

export function EmployeeScheduleHero({ schedules, employeeId, displayName, isLoading, hasError }: Props) {
  const [now, setNow] = useState(() => new Date());
  useEffect(() => {
    const refresh = () => setNow(new Date());
    const timer = window.setInterval(refresh, 1_000);
    window.addEventListener("focus", refresh);
    document.addEventListener("visibilitychange", refresh);
    return () => {
      window.clearInterval(timer);
      window.removeEventListener("focus", refresh);
      document.removeEventListener("visibilitychange", refresh);
    };
  }, []);
  const shifts = getHeroShifts(schedules, employeeId, now);
  const firstName = displayName.split(/\s+/)[0];
  const countdown = (at: number, tomorrow: boolean) => {
    const minutes = Math.max(0, Math.ceil((at - now.getTime()) / 60_000));
    if (!tomorrow && minutes === 0) return t("Shift ended");
    const duration = t("{0}h {1}m", Math.floor(minutes / 60), minutes % 60);
    return tomorrow ? t("Starts in {0}", duration) : t("Ends in {0}", duration);
  };
  return (
    <section className={styles.hero} data-employee-motion aria-label={t("Your upcoming shifts")}>
      <div className={styles.heading}>
        <div className={styles.greeting}>
          <time dateTime={now.toISOString()}>{dateTimeFormat("en-GB", { weekday: "long", month: "long", day: "numeric" }).format(now)}</time>
          <h1>{t("Hey, {0}", firstName)}</h1>
        </div>
        <time className={styles.clock} dateTime={now.toISOString()}>
          {dateTimeFormat("en-GB", { hour: "2-digit", minute: "2-digit", second: "2-digit", hourCycle: "h23" }).format(now)}
        </time>
      </div>
      <div className={styles.shifts}>
        {(["now", "tomorrow"] as const).map(day => (
          <div key={day} className={styles.day}>
            <h2>{day === "now" ? t("Now") : t("Tomorrow")}</h2>
            {isLoading || hasError ? <p className={styles.status}>{isLoading ? t("Loading schedules...") : t("Could not load published schedules.")}</p>
              : shifts[day].length === 0 ? <p className={styles.dayOff}>{t("Day off")}</p>
              : shifts[day].map(shift => (
                <div key={shift.key} className={styles.shift}>
                  <strong>{shift.name}</strong>
                  <span className={styles.range}>{shift.fromTime} – {shift.toTime}</span>
                  <span className={styles.countdown}>{countdown(day === "now" ? shift.end : shift.start, day === "tomorrow")}</span>
                </div>
              ))}
          </div>
        ))}
      </div>
    </section>
  );
}
