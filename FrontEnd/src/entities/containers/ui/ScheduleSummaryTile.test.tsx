import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";
import { setLanguage } from "@shared/i18n";
import { formatScheduleLastUpdate } from "@shared/lib/scheduleLastUpdate";
import type { ContainerGraphSummary } from "../model/statistics";
import { ScheduleSummaryTile } from "./ScheduleSummaryTile";
import styles from "./ScheduleSummaryTile.module.css";

const summary: ContainerGraphSummary = {
  graph: {
    id: 7, containerId: 1, shopId: 4, name: "PKP K150", year: 2026, month: 10,
    publicationStatus: "public", peoplePerShift: 2, shift1Time: "08:00", shift2Time: "16:00",
    maxHoursPerEmpMonth: 160, maxConsecutiveDays: 5, maxConsecutiveFull: 2, maxFullPerMonth: 4,
    lastUpdatedAtUtc: "2026-10-25T11:39:00Z",
  },
  employeeCount: 9, assignedHoursText: "398h 0m", assignedSlotCount: 1, coverageDays: 1,
  shopName: "Central", monthYearLabel: "October 2026", availabilityLabel: "None",
};

beforeEach(() => setLanguage("en"));
afterEach(cleanup);

function setup(value = summary, multi = false, selected = false) {
  const onActivate = vi.fn();
  return { onActivate, ...render(<ScheduleSummaryTile summary={value} isMultiOpenEnabled={multi} isSelected={selected} onActivate={onActivate} />) };
}

describe("ScheduleSummaryTile", () => {
  test("shows exactly the six original metadata fields and title", () => {
    const { container } = setup();
    expect(screen.getByText("PKP K150")).toBeInTheDocument();
    const labels = Array.from(container.querySelectorAll(`.${styles.chipLabel}`), node => node.textContent);
    expect(labels).toEqual(["Shop", "Employees", "Hours", "Month Year", "Status", "Last Update"]);
    expect(container.querySelectorAll(`.${styles.chipIcon} svg`)).toHaveLength(6);
    expect(container.querySelector("[data-schedule-decoration]")).toHaveAttribute("aria-hidden", "true");
    for (const value of ["Central", "9", "398h 0m", "October 2026", "Public", formatScheduleLastUpdate(summary.graph.lastUpdatedAtUtc)]) {
      expect(screen.getByText(value)).toBeInTheDocument();
    }
    for (const label of ["State", "ID", "Note"]) expect(screen.queryByText(label)).not.toBeInTheDocument();
  });

  test.each(["public", "private"] as const)("keeps %s semantic status styling", publicationStatus => {
    setup({ ...summary, graph: { ...summary.graph, publicationStatus } });
    expect(screen.getByText(publicationStatus === "public" ? "Public" : "Private"))
      .toHaveClass(publicationStatus === "public" ? styles.statusPublic : styles.statusPrivate);
    expect(screen.getByText(formatScheduleLastUpdate(summary.graph.lastUpdatedAtUtc))).toHaveClass(styles.lastUpdate);
  });

  test.each([null, "invalid"])("uses the existing timestamp fallback for %s", lastUpdatedAtUtc => {
    setup({ ...summary, graph: { ...summary.graph, lastUpdatedAtUtc } });
    expect(screen.getByText("Not recorded yet")).toBeInTheDocument();
    expect(screen.queryByText(/Invalid Date|undefined/)).not.toBeInTheDocument();
  });

  test("normal mode activates once per mouse, Enter and Space action", async () => {
    const user = userEvent.setup();
    const { container, onActivate } = setup(summary, false, true);
    const button = screen.getByRole("button", { name: "Open schedule PKP K150" });
    expect(button).not.toHaveAttribute("aria-pressed");
    expect(button).not.toHaveClass(styles.selectedTile);
    expect(container.querySelector("[data-schedule-selection-indicator]")).toBeNull();
    await user.click(button);
    expect(onActivate).toHaveBeenCalledTimes(1);
    await user.keyboard("{Enter}");
    expect(onActivate).toHaveBeenCalledTimes(2);
    await user.keyboard(" ");
    expect(onActivate).toHaveBeenCalledTimes(3);
  });

  test("MultiOpen reflects controlled selection without nesting interactive elements", async () => {
    const user = userEvent.setup();
    const { container, onActivate, rerender } = setup(summary, true);
    const button = screen.getByRole("button", { name: "Select schedule PKP K150" });
    expect(button).toHaveAttribute("aria-pressed", "false");
    expect(container.querySelector("[data-schedule-selection-indicator] svg")).toBeNull();
    expect(container.querySelector("[data-schedule-selection-indicator]")).not.toBeNull();
    expect(container.querySelector("[data-schedule-decoration]")).toBeNull();
    await user.click(button);
    expect(onActivate).toHaveBeenCalledTimes(1);
    rerender(<ScheduleSummaryTile summary={summary} isMultiOpenEnabled isSelected onActivate={onActivate} />);
    expect(screen.getByRole("button", { name: "Deselect schedule PKP K150" })).toHaveAttribute("aria-pressed", "true");
    expect(button).toHaveClass(styles.selectedTile);
    expect(container.querySelector("[data-schedule-selection-indicator]")).toHaveClass(styles.selectionIndicatorSelected);
    expect(container.querySelector("[data-schedule-selection-indicator] svg")).not.toBeNull();
    expect(button.querySelector("button, input, a, [role=button]")).toBeNull();
  });

  test("uses all seven stable graph ID palettes independent of other fields", () => {
    const { rerender } = setup();
    const button = screen.getByRole("button");
    const initialClass = button.className;
    rerender(<ScheduleSummaryTile summary={{ ...summary, shopName: "Changed", graph: { ...summary.graph, name: "Renamed" } }} isMultiOpenEnabled={false} isSelected={false} onActivate={vi.fn()} />);
    expect(button.className).toBe(initialClass);
    const classes = new Set<string>();
    for (let id = 0; id < 7; id++) {
      rerender(<ScheduleSummaryTile summary={{ ...summary, graph: { ...summary.graph, id } }} isMultiOpenEnabled={false} isSelected={false} onActivate={vi.fn()} />);
      classes.add(button.className);
    }
    expect(classes.size).toBe(7);
  });

  test("retains complete long names and shop values", () => {
    const name = "Long schedule ".repeat(20);
    const shopName = "Extended shop ".repeat(20);
    setup({ ...summary, shopName, graph: { ...summary.graph, name } });
    expect(screen.getByRole("button")).toHaveAccessibleName(`Open schedule ${name.trim()}`);
    expect(screen.getByText(name.trim())).toHaveTextContent(name.trim());
    expect(screen.getByText(shopName.trim())).toHaveTextContent(shopName.trim());
  });
});
