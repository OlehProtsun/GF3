import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, expect, test, vi } from "vitest";
import type { GraphMatrixColumn } from "@entities/containers/model/graphWorkspace";
import { setLanguage } from "@shared/i18n";
import { EmployeeScheduleColumnOrderDialog } from "./EmployeeScheduleColumnOrderDialog";

const columns: GraphMatrixColumn[] = [5, -7, 21].map((employeeId, index) => ({
  employeeId, label: ["Ada Able", "Manual column", "Zoe Young"][index],
  kind: employeeId < 0 ? "manual" : "employee", manualColumnId: employeeId < 0 ? -employeeId : null,
  graphEmployeeId: null, minHoursMonth: null, totalMinutes: 0, totalText: "0:00",
}));

beforeEach(() => setLanguage("en"));

test.each([{ initial: [] }, { initial: columns.slice(0, 1) }])("keeps an unchanged empty or single-column order unapplied ($initial.length)", ({ initial }) => {
  render(<EmployeeScheduleColumnOrderDialog open columns={initial} defaultColumns={initial} activeEmployeeId={null}
    onCancel={vi.fn()} onSave={vi.fn()} />);
  expect(screen.getByRole("button", { name: "Apply" })).toBeDisabled();
  expect(screen.getByRole("button", { name: "Reset default" })).toBeDisabled();
  expect(within(screen.getByRole("list", { name: "Schedule columns" })).queryAllByRole("listitem")).toHaveLength(initial.length);
});

test("reorders a manual column by keyboard and applies each ID once", async () => {
  const user = userEvent.setup();
  const onSave = vi.fn();
  render(<EmployeeScheduleColumnOrderDialog open columns={columns} defaultColumns={columns} activeEmployeeId={5}
    onCancel={vi.fn()} onSave={onSave} />);
  screen.getAllByRole("listitem").forEach((row, index) => {
    vi.spyOn(row, "getBoundingClientRect").mockReturnValue(DOMRect.fromRect({ x: 10, y: 20 + index * 60, width: 280, height: 52 }));
  });
  screen.getByRole("button", { name: "Reorder Manual column" }).focus();
  await user.keyboard("[Space]");
  await user.keyboard("[ArrowUp]");
  expect(screen.getByText("Drop here")).toBeInTheDocument();
  await user.keyboard("[Space]");
  expect(onSave).not.toHaveBeenCalled();
  await user.click(screen.getByRole("button", { name: "Apply" }));
  expect(onSave).toHaveBeenCalledExactlyOnceWith([-7, 5, 21]);
});

test("Escape cancels the drag first and the dialog second, with Reset remaining a draft", async () => {
  const user = userEvent.setup();
  const onSave = vi.fn(), onCancel = vi.fn();
  const savedColumns = [columns[1], columns[0], columns[2]];
  const openDialog = (open: boolean) => <EmployeeScheduleColumnOrderDialog open={open} columns={savedColumns} defaultColumns={columns}
    activeEmployeeId={5} onCancel={onCancel} onSave={onSave} />;
  const { rerender } = render(openDialog(true));
  await user.click(screen.getByRole("button", { name: "Reset default" }));
  expect(screen.getByRole("button", { name: "Apply" })).toBeEnabled();
  expect(onSave).not.toHaveBeenCalled();
  screen.getByRole("button", { name: "Reorder Manual column" }).focus();
  await user.keyboard("[Space]");
  await user.keyboard("[Escape]");
  expect(onCancel).not.toHaveBeenCalled();
  expect(screen.getByRole("button", { name: "Apply" })).toBeEnabled();
  await user.keyboard("[Escape]");
  expect(onCancel).toHaveBeenCalledOnce();
  expect(onSave).not.toHaveBeenCalled();
  rerender(openDialog(false));
  rerender(openDialog(true));
  expect(screen.getByRole("button", { name: "Apply" })).toBeDisabled();
  expect(screen.getAllByRole("listitem")[0]).toHaveTextContent("Manual column");
});
