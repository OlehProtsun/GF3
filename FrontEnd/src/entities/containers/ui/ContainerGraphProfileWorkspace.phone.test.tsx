import { beforeEach as beforeEnglishTest } from "vitest";
import { setLanguage as setTestLanguage } from "@shared/i18n";
import { fireEvent, render, screen, within } from "@testing-library/react";
import { expect, test, vi } from "vitest";
import userEvent from "@testing-library/user-event";
import type { Graph, GraphEmployee, GraphSlot } from "../model/types";
import { buildGraphNoteContent } from "../model/graphNote";
import type { Employee } from "@entities/employees/model/types";
import { ContainerGraphProfileWorkspace } from "./ContainerGraphProfileWorkspace";

vi.mock("@app/providers/PublicHolidayVisibilityProvider", () => ({ usePublicHolidayVisibility: vi.fn(() => true) }));
import { usePublicHolidayVisibility } from "@app/providers/PublicHolidayVisibilityProvider";

beforeEnglishTest(() => { setTestLanguage("en"); vi.mocked(usePublicHolidayVisibility).mockReturnValue(true); });

const graph: Graph = { id: 3, containerId: 1, shopId: 4, name: "July Schedule", year: 2026, month: 7, publicationStatus: "public", peoplePerShift: 1, shift1Time: "08:00 - 12:00", shift2Time: "14:00 - 18:00", maxHoursPerEmpMonth: 160, maxConsecutiveDays: 5, maxConsecutiveFull: 3, maxFullPerMonth: 10, lastUpdatedAtUtc: "2026-07-01T10:00:00Z" };
const employees: Employee[] = [
  { id: 2, firstName: "Anna", lastName: "Kowalska", hasLoginAccount: false, isOnline: false },
  { id: 5, firstName: "Jan", lastName: "Nowak", hasLoginAccount: false, isOnline: false },
];
const graphEmployees: GraphEmployee[] = employees.map((employee, index) => ({ id: index + 10, scheduleId: 3, employeeId: employee.id, displayOrder: index }));
const slots: GraphSlot[] = [
  { id: 1, scheduleId: 3, employeeId: 2, dayOfMonth: 1, slotNo: 1, fromTime: "08:00", toTime: "12:00", status: "Working" },
  { id: 2, scheduleId: 3, employeeId: 2, dayOfMonth: 1, slotNo: 2, fromTime: "14:00", toTime: "18:00", status: "Working" },
  { id: 3, scheduleId: 3, employeeId: 5, dayOfMonth: 2, slotNo: 1, fromTime: "09:00", toTime: "15:00", status: "Working" },
];
const props = { graph, graphEmployees, slots, cellStyles: [], employeesById: new Map(employees.map(employee => [employee.id, employee])), compactSize: true, showManagementActions: false, isLoading: false, hasLoadError: false, isDeleting: false, onEdit: vi.fn(), onDelete: vi.fn() };
function cards(container: HTMLElement) { return Array.from(container.querySelectorAll<HTMLDetailsElement>("[data-phone-schedule-summary] > details")); }

test("legacy order pins a tagged empty generated column and displays accepted manual color", () => {
  const note = buildGraphNoteContent("", [{ id: 7, label: "Generated", source: "generated-unfilled", cells: {} },
    { id: 8, label: "do obstawienia", cells: { "1": "Text" } }], [2, -8, 5, -7], [
    { employeeId: -7, dayOfMonth: 1, backgroundColorArgb: -4528387, textColorArgb: null },
  ]);
  render(<ContainerGraphProfileWorkspace {...props} graph={{ ...graph, note }} />);
  const headers = screen.getAllByRole("columnheader");
  expect(headers[0]).toHaveTextContent("Day"); expect(headers[1]).toHaveTextContent("Generated");
  expect(headers[2]).toHaveTextContent("Anna Kowalska"); expect(headers[3]).toHaveTextContent("do obstawienia");
  expect(screen.getByRole("button", { name: "Generated day 1" }).closest("td")).toHaveStyle({ backgroundColor: "#BAE6FD" });
});

test("phone sections start collapsed independently around a visible read-only matrix", async () => {
  const { container } = render(<ContainerGraphProfileWorkspace {...props} cellStyles={[{ id: 9, scheduleId: 3, dayOfMonth: 1, employeeId: 2, backgroundColorArgb: -65536 }]} />);
  const sections = ["Schedule Information", "Schedule Matrix", "Schedule Summary"].map(title => screen.getByText(title).closest("section")!);
  expect(sections[0].compareDocumentPosition(sections[1]) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  expect(sections[1].compareDocumentPosition(sections[2]) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  const infoToggle = screen.getByRole("button", { name: "Schedule Information" });
  const summaryToggle = screen.getByRole("button", { name: /Schedule Summary/ });
  expect(infoToggle).toHaveAttribute("aria-expanded", "false");
  expect(summaryToggle).toHaveAttribute("aria-expanded", "false");
  expect(screen.queryByRole("searchbox")).not.toBeInTheDocument();
  await userEvent.click(infoToggle);
  expect(infoToggle).toHaveAttribute("aria-expanded", "true");
  expect(summaryToggle).toHaveAttribute("aria-expanded", "false");
  expect(screen.getByRole("group", { name: "Schedule details" })).toBeVisible();
  await userEvent.click(summaryToggle);
  expect(summaryToggle).toHaveAttribute("aria-expanded", "true");
  expect(infoToggle).toHaveAttribute("aria-expanded", "true");
  expect(screen.getByRole("searchbox")).toBeVisible();
  const rows = cards(container);
  expect(rows).toHaveLength(2);
  expect(within(rows[0]).getByText("AK")).toHaveAttribute("aria-hidden", "true");
  expect(Array.from(rows[0].querySelectorAll("summary dd")).map(node => node.textContent)).toEqual(["1", "30", "0", "8", "0"]);
  expect(Array.from(rows[1].querySelectorAll("summary dd")).map(node => node.textContent)).toEqual(["1", "30", "0", "6", "0"]);
  const matrix = container.querySelector("[data-phone-matrix-scroll]")!;
  expect(matrix).toHaveAttribute("tabindex", "0");
  expect(within(matrix as HTMLElement).getByText("Anna Kowalska")).toBeInTheDocument();
  expect(matrix.querySelector('[style*="background-color"]')).toBeTruthy();
  expect(screen.getByText("This schedule is read-only.")).toBeInTheDocument();
  expect(screen.queryByRole("button", { name: /Edit Schedule|Delete Schedule/ })).not.toBeInTheDocument();
});

test("search handles names, surnames, multiword case, no matches and clearing", () => {
  const { container } = render(<ContainerGraphProfileWorkspace {...props} />);
  fireEvent.click(screen.getByRole("button", { name: /Schedule Summary/ }));
  const input = screen.getByRole("searchbox");
  for (const value of ["anna", "KOWALSKA", "kOwAlSkA aNnA"]) {
    fireEvent.change(input, { target: { value } });
    expect(cards(container)).toHaveLength(1);
    expect(within(cards(container)[0]).getByText("Anna Kowalska")).toBeInTheDocument();
  }
  fireEvent.change(input, { target: { value: "missing" } });
  expect(screen.getByRole("status")).toHaveTextContent('No employees found for "missing".');
  expect(cards(container)).toHaveLength(0);
  fireEvent.change(input, { target: { value: "" } });
  expect(cards(container)).toHaveLength(2);
});

test("native employee disclosure toggles and retains multiple daily shifts", async () => {
  const user = userEvent.setup();
  const { container } = render(<ContainerGraphProfileWorkspace {...props} />);
  await user.click(screen.getByRole("button", { name: /Schedule Summary/ }));
  const card = cards(container)[0];
  const summary = card.querySelector("summary")!;
  expect(card.open).toBe(false);
  await user.click(summary);
  expect(card.open).toBe(true);
  expect(within(card).getByText("08:00")).toBeVisible();
  expect(within(card).getByText("14:00")).toBeVisible();
  expect(within(card).getByText("18:00")).toBeVisible();
  expect(Array.from(card.querySelectorAll("dd > div > span:last-child")).filter(node => node.textContent === "Hours4")).toHaveLength(2);
  await user.click(summary);
  expect(card.open).toBe(false);
  // Native keyboard activation is verified in the browser: jsdom does not implement it.
  expect(summary).toBe(card.firstElementChild);
  expect(summary.querySelector("button, a")).toBeNull();
});

test.each(["public", "private"] as const)("schedule information expands with all fields and %s status", async status => {
  const { container } = render(<ContainerGraphProfileWorkspace {...props} graph={{ ...graph, publicationStatus: status }} />);
  const information = container.querySelector<HTMLElement>("[data-phone-schedule-information]")!;
  expect(information).toBeVisible();
  const toggle = within(information).getByRole("button", { name: "Schedule Information" });
  expect(toggle).toHaveAttribute("aria-expanded", "false");
  await userEvent.click(toggle);
  expect(toggle).toHaveAttribute("aria-expanded", "true");
  const fields = within(information).getByRole("group", { name: "Schedule details" });
  expect(fields.children).toHaveLength(11);
  for (const label of ["Month", "Year", "Shop", "Status", "People on Shift", "Shift1", "Shift2", "Max Consecutive Days", "Max Consecutive Full", "Max Full", "Availability"]) expect(within(fields).getByText(label)).toBeInTheDocument();
  expect(within(information).getByText("No notes yet.")).toBeInTheDocument();
  expect(within(information).getByText(status === "public" ? "Public" : "Private")).toBeInTheDocument();
  expect(within(information).getByText("None").parentElement?.className).toContain("phoneAvailabilityTile");
  expect(information.querySelector("time")).toHaveAttribute("datetime", graph.lastUpdatedAtUtc);
  expect(within(information).getByText("Last Update")).toBeInTheDocument();
});

test.each([false, true])("desktop compact=%s puts actions in the matrix and details in a dialog", async compactSize => {
  const user = userEvent.setup();
  const onEdit = vi.fn();
  const onDelete = vi.fn();
  const { container, rerender } = render(<ContainerGraphProfileWorkspace {...props} compactSize={compactSize} showManagementActions onEdit={onEdit} onDelete={onDelete} />);
  expect(container.querySelector('[data-phone-schedule-summary]')).toBeNull();
  expect(container.querySelector("aside")).toBeNull();
  expect(screen.getByRole("columnheader", { name: "Work Days" })).toBeInTheDocument();
  const matrix = within(screen.getByText("Schedule Matrix", { exact: true }).closest("section")!);
  expect(matrix.getByText("Last Update")).toBeVisible();
  expect(matrix.getByText("Total Employees: 2")).toBeVisible();
  await user.click(matrix.getByRole("button", { name: "Edit Schedule" }));
  await user.click(matrix.getByRole("button", { name: "Delete Schedule" }));
  expect(onEdit).toHaveBeenCalledTimes(1);
  expect(onDelete).toHaveBeenCalledTimes(1);

  expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  await user.click(matrix.getByRole("button", { name: "Schedule Information" }));
  const dialog = within(screen.getByRole("dialog", { name: "Schedule Information" }));
  expect(dialog.getByRole("heading", { name: graph.name })).toBeVisible();
  expect(dialog.getByText("ID 3")).toBeVisible();
  expect(dialog.getByText("No notes yet.")).toBeVisible();
  const fields = dialog.getByRole("group", { name: "Schedule details" });
  expect(Array.from(fields.children, tile => tile.textContent)).toEqual([
    "MonthJuly", "Year2026", "ShopShop 4", "StatusPublic", "People on Shift1", "Shift108:00 - 12:00",
    "Shift214:00 - 18:00", "Max Consecutive Days5", "Max Consecutive Full3", "Max Full10", "AvailabilityNone",
  ]);
  expect(dialog.queryByRole("button", { name: /Edit|Delete/ })).not.toBeInTheDocument();
  await user.click(dialog.getByRole("button", { name: "Close" }));
  expect(screen.queryByRole("dialog")).not.toBeInTheDocument();

  rerender(<ContainerGraphProfileWorkspace {...props} compactSize={compactSize} showManagementActions showEditAction={false} isDeleting />);
  expect(matrix.queryByRole("button", { name: "Edit Schedule" })).not.toBeInTheDocument();
  expect(matrix.getByRole("button", { name: "Deleting..." })).toBeDisabled();
});

test("empty and optional data use existing fallbacks", () => {
  render(<ContainerGraphProfileWorkspace {...props} graph={{ ...graph, lastUpdatedAtUtc: null, note: null }} graphEmployees={[]} slots={[]} employeesById={new Map()} />);
  expect(screen.getByText("No results")).toBeInTheDocument();
  expect(screen.getByText("Shop 4")).toBeInTheDocument();
  expect(screen.getByText("None")).toBeInTheDocument();
  expect(screen.getByText("No notes yet.")).toBeInTheDocument();
  expect(screen.queryByRole("searchbox")).not.toBeInTheDocument();
  expect(document.body.textContent).not.toMatch(/undefined|NaN/);
});


test("phone holiday metric follows visibility while preserving original metrics and disclosure", () => {
  const { container, rerender } = render(<ContainerGraphProfileWorkspace {...props} graph={{ ...graph, month: 5 }} />);
  fireEvent.click(screen.getByRole("button", { name: /Schedule Summary/ }));
  const summary = cards(container)[0].querySelector("summary")!;
  expect(within(summary).getByText("Holiday Days").nextElementSibling).toHaveTextContent("1");
  expect(Array.from(summary.querySelectorAll("dd")).map(node => node.textContent)).toEqual(["1", "30", "0", "8", "1"]);
  vi.mocked(usePublicHolidayVisibility).mockReturnValue(false);
  rerender(<ContainerGraphProfileWorkspace {...props} graph={{ ...graph, month: 5 }} />);
  expect(within(summary).queryByText("Holiday Days")).not.toBeInTheDocument();
  expect(Array.from(summary.querySelectorAll("dd")).map(node => node.textContent)).toEqual(["1", "30", "0", "8"]);
  expect(cards(container)[0].firstElementChild).toBe(summary);
});
