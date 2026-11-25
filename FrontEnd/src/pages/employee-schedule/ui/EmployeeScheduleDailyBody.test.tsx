import { render, screen, within } from "@testing-library/react";
import { expect, test, vi } from "vitest";
import type { EmployeeSchedule } from "@entities/employee-schedule";
import { setLanguage } from "@shared/i18n";
import { buildDailyScheduleDays, buildDailyScheduleWorkers, EmployeeScheduleDailyBody, parseTimeMinutes } from "./EmployeeScheduleDailyBody";

const graph = { id: 5, year: 2026, month: 8, slots: [
  { id: 1, slotNo: 1, employeeId: 1, dayOfMonth: 15, fromTime: "00:00", toTime: "23:59", status: "ASSIGNED" },
  { id: 2, slotNo: 1, employeeId: 2, dayOfMonth: 16, fromTime: "09:00", toTime: "13:00", status: "ASSIGNED" },
], relatedScheduleAssignments: [{ employeeId: 3, dayOfMonth: 15, scheduleId: 4, scheduleName: "Other shop" }] } as EmployeeSchedule;

test.each([[2026, 1, 31], [2026, 4, 30], [2026, 2, 28], [2028, 2, 29]])("lists days in %s-%s", (year, month, count) => {
  expect(buildDailyScheduleDays({ ...graph, year, month }, 1)).toHaveLength(count);
  expect(parseTimeMinutes("unavailable")).toBeNull();
});

test("renders an all-day shift, a related-only worker and a holiday for the selected day", () => {
  setLanguage("en");
  render(<EmployeeScheduleDailyBody selectedSchedule={graph} activeDailyScheduleDay={15} activeDailyScheduleDayRef={null}
    dailyScheduleDays={buildDailyScheduleDays(graph, 1)} dailyScheduleWorkers={buildDailyScheduleWorkers(graph, 15, 1, "Fixture")}
    showPublicHolidays onSelectDay={vi.fn()} />);
  expect(screen.getByRole("tab", { name: /15, working day, Holiday/ })).toHaveAttribute("aria-selected", "true");
  expect(screen.getByRole("tab", { name: /16, day off/ })).toBeVisible();
  const workers = within(screen.getByRole("tabpanel")).getAllByRole("article");
  expect(workers).toHaveLength(2);
  expect(workers[0]).toHaveTextContent("All day");
  expect(workers[1]).toHaveTextContent("Other shop");
});
