import { dateTimeFormat } from "@shared/i18n";
import { t } from "@shared/i18n";
const scheduleLastUpdateFormatter = dateTimeFormat("en-GB", {
  day: "2-digit",
  month: "short",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
  hourCycle: "h23",
});

export function formatScheduleLastUpdate(value?: string | null) {
  if (!value) {
    return t("Not recorded yet");
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return t("Not recorded yet");
  }

  return scheduleLastUpdateFormatter.format(date);
}
