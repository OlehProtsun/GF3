const scheduleLastUpdateFormatter = new Intl.DateTimeFormat("en-GB", {
  day: "2-digit",
  month: "short",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
  hourCycle: "h23",
});

export function formatScheduleLastUpdate(value?: string | null) {
  if (!value) {
    return "Not recorded yet";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "Not recorded yet";
  }

  return scheduleLastUpdateFormatter.format(date);
}
