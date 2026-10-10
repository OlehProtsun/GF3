import { fireEvent, render, screen, within } from "@testing-library/react";
import { expect, test, vi } from "vitest";
import userEvent from "@testing-library/user-event";
import type { Graph, GraphEmployee, GraphSlot } from "../model/types";
import type { Employee } from "@entities/employees/model/types";
import { ContainerGraphProfileWorkspace } from "./ContainerGraphProfileWorkspace";

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

test("phone sections have reading order, real metrics and a preserved read-only matrix", () => {
  const { container } = render(<ContainerGraphProfileWorkspace {...props} cellStyles={[{ id: 9, scheduleId: 3, dayOfMonth: 1, employeeId: 2, backgroundColorArgb: -65536 }]} />);
  const sections = ["Schedule Information", "Schedule Matrix", "Schedule Summary"].map(title => screen.getByText(title).closest("section")!);
  expect(sections[0].compareDocumentPosition(sections[1]) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  expect(sections[1].compareDocumentPosition(sections[2]) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  expect(screen.getAllByRole("searchbox")).toHaveLength(1);
  const rows = cards(container);
  expect(rows).toHaveLength(2);
  expect(within(rows[0]).getByText("AK")).toHaveAttribute("aria-hidden", "true");
  expect(Array.from(rows[0].querySelectorAll("summary dd")).map(node => node.textContent)).toEqual(["1", "30", "8"]);
  expect(Array.from(rows[1].querySelectorAll("summary dd")).map(node => node.textContent)).toEqual(["1", "30", "6"]);
  const matrix = container.querySelector("[data-phone-matrix-scroll]")!;
  expect(matrix).toHaveAttribute("tabindex", "0");
  expect(within(matrix as HTMLElement).getByText("Anna Kowalska")).toBeInTheDocument();
  expect(matrix.querySelector('[style*="background-color"]')).toBeTruthy();
  expect(screen.getByText("This schedule is read-only.")).toBeInTheDocument();
  expect(screen.queryByRole("button", { name: /Edit Schedule|Delete Schedule/ })).not.toBeInTheDocument();
});

test("search handles names, surnames, multiword case, no matches and clearing", () => {
  const { container } = render(<ContainerGraphProfileWorkspace {...props} />);
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

test.each(["public", "private"] as const)("information is initially visible with all fields and %s status", status => {
  const { container } = render(<ContainerGraphProfileWorkspace {...props} graph={{ ...graph, publicationStatus: status }} />);
  const information = container.querySelector<HTMLElement>("[data-phone-schedule-information]")!;
  expect(information).toBeVisible();
  expect(screen.queryByRole("button", { name: /Collapse Schedule Information|Expand Schedule Information/ })).not.toBeInTheDocument();
  const fields = within(information).getByRole("group", { name: "Schedule details" });
  expect(fields.children).toHaveLength(11);
  for (const label of ["Month", "Year", "Shop", "Status", "People on Shift", "Shift1", "Shift2", "Max Consecutive Days", "Max Consecutive Full", "Max Full", "Availability"]) expect(within(fields).getByText(label)).toBeInTheDocument();
  expect(within(information).getByText("No notes yet.")).toBeInTheDocument();
  expect(within(information).getByText(status === "public" ? "Public" : "Private")).toBeInTheDocument();
  expect(within(information).getByText("None").parentElement?.className).toContain("phoneAvailabilityTile");
  expect(information.querySelector("time")).toHaveAttribute("datetime", graph.lastUpdatedAtUtc);
  expect(within(information).getByText("Last Update")).toBeInTheDocument();
});

test.each([false, true])("desktop compact=%s retains summary table and information controls", async compactSize => {
  const user = userEvent.setup();
  const { container } = render(<ContainerGraphProfileWorkspace {...props} compactSize={compactSize} showManagementActions />);
  expect(container.querySelector('[data-phone-schedule-summary]')).toBeNull();
  expect(screen.getByRole("columnheader", { name: "Work Days" })).toBeInTheDocument();
  await user.click(screen.getByRole("button", { name: "Expand Schedule Information" }));
  expect(screen.getByRole("button", { name: "Collapse Schedule Information" })).toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Edit Schedule" })).toBeInTheDocument();
  await user.click(screen.getByRole("button", { name: "Collapse Schedule Information" }));
  expect(screen.getByRole("button", { name: "Expand Schedule Information" })).toBeInTheDocument();
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
