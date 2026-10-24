import { beforeEach, describe, expect, test, vi } from "vitest";
import { fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { setLanguage } from "@shared/i18n";
import type { ShiftSwap } from "@entities/shift-swaps";
import { ContainerGraphManualColumnsCard } from "./ContainerGraphManualColumnsCard";

beforeEach(() => setLanguage("en"));
const employees = [{ id: 2, firstName: "Ada", lastName: "Lovelace", hasLoginAccount: true, isOnline: false }];
const base = { columns: [], employees, year: 2026, month: 2, onAddColumn: vi.fn(), onDeleteColumn: vi.fn(), onPublishShift: vi.fn() };
const column = { columnId: 4, label: "Do obsadzenia", cells: { "1": "08:00 - 12:00", "3": "Notes", "31": "08:00 - 12:00" } };
const offer = { id: 20, manualColumnId: 4, dayOfMonth: 1, year: 2026, month: 2, fromTime: "08:00:00", toTime: "12:00:00", status: "open", isManagerCreated: true, visibility: "public" } as ShiftSwap;
const pending = { allowPartialAcceptance: false, clientId: "pending-1", manualColumnId: 4, dayOfMonth: 2, fromTime: "08:00", toTime: "12:00" };

describe("manual shifts card", () => {
  test("empty, add, delete and provenance uses source rather than name", () => {
    const onAddColumn = vi.fn(); const onDeleteColumn = vi.fn();
    const { rerender } = render(<ContainerGraphManualColumnsCard {...base} onAddColumn={onAddColumn} />);
    expect(screen.getByText("No manual columns yet.")).toBeVisible();
    expect(screen.getByRole("button", { name: "Publish shift" })).toBeDisabled();
    fireEvent.click(screen.getByRole("button", { name: "Add" })); expect(onAddColumn).toHaveBeenCalledOnce();
    rerender(<ContainerGraphManualColumnsCard {...base} columns={[column, { ...column, columnId: 5, source: "generated-unfilled" }]} onDeleteColumn={onDeleteColumn} />);
    expect(screen.getAllByText("Auto")).toHaveLength(1);
    fireEvent.click(screen.getAllByRole("button", { name: "Delete Do obsadzenia" })[0]);
    expect(onDeleteColumn).toHaveBeenCalledWith(4);
  });
  test("renders every shift chronologically and preserves selection when scrolling", () => {
    const onPublishShift = vi.fn();
    render(<ContainerGraphManualColumnsCard {...base} onPublishShift={onPublishShift} columns={[
      { columnId: 7, label: "Later", cells: { "2": "09:00-13:00", "4": "09:00-13:00", "5": "09:00-13:00", "6": "09:00-13:00", "7": "09:00-13:00" } }, column,
    ]} />);
    expect(screen.getByRole("button", { name: /01 Feb/ })).toHaveAttribute("aria-pressed", "true");
    expect(screen.getByRole("button", { name: /07 Feb/ })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Show all|Show less/ })).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Publish shift" }));
    expect(onPublishShift).toHaveBeenLastCalledWith({ manualColumnId: 4, dayOfMonth: 1, fromTime: "08:00", toTime: "12:00", targetEmployeeId: null, allowPartialAcceptance: false });
    fireEvent.scroll(screen.getByRole("region", { name: "Available shifts" }), { target: { scrollTop: 200 } });
    fireEvent.click(screen.getByRole("button", { name: /07 Feb/ }));
    fireEvent.click(screen.getByRole("button", { name: "Publish shift" }));
    expect(onPublishShift).toHaveBeenLastCalledWith({ manualColumnId: 7, dayOfMonth: 7, fromTime: "09:00", toTime: "13:00", targetEmployeeId: null, allowPartialAcceptance: false });
    fireEvent.scroll(screen.getByRole("region", { name: "Available shifts" }), { target: { scrollTop: 0 } });
    expect(screen.getByRole("button", { name: /07 Feb/ })).toHaveAttribute("aria-pressed", "true");
  });
  test.each([0, 1, 5, 30, 62])("all %i shifts are accessible without pagination", count => {
    const columns = Array.from({ length: Math.ceil(count / 28) }, (_, index) => ({
      columnId: index + 1, label: `Custom ${index}`, cells: Object.fromEntries(
        Array.from({ length: Math.min(28, count - index * 28) }, (_, day) => [String(day + 1), "08:00 - 12:00"]),
      ),
    }));
    render(<ContainerGraphManualColumnsCard {...base} columns={columns} />);
    const list = screen.queryByRole("region", { name: "Available shifts" });
    if (count) {
      expect(within(list!).getAllByRole("button")).toHaveLength(count);
      expect(list).toHaveAttribute("tabindex", "0");
    } else expect(list).toBeNull();
    expect(screen.queryByRole("button", { name: /Show all|Show less|Load more/ })).toBeNull();
  });
  test("specific employee requires selection and uses unchanged payload", async () => {
    const onPublishShift = vi.fn(); const user = userEvent.setup();
    render(<ContainerGraphManualColumnsCard {...base} columns={[column]} onPublishShift={onPublishShift} />);
    await user.click(screen.getByRole("button", { name: "Specific employee" }));
    expect(screen.getByRole("button", { name: "Publish shift" })).toBeDisabled();
    await user.click(screen.getByRole("button", { name: "employee list" }));
    await user.click(screen.getByRole("option", { name: /Ada Lovelace/ }));
    await user.click(screen.getByRole("button", { name: "Publish shift" }));
    expect(onPublishShift).toHaveBeenCalledWith({ manualColumnId: 4, dayOfMonth: 1, fromTime: "08:00", toTime: "12:00", targetEmployeeId: 2, allowPartialAcceptance: false });
  });
  test.each([{ allowSwap: false }, { isSchedulePublished: false }, { isPublishingShift: true }])("publication blocked %j", patch => {
    const onPublishShift = vi.fn();
    render(<ContainerGraphManualColumnsCard {...base} columns={[column]} {...patch} onPublishShift={onPublishShift} />);
    const button = screen.getByRole("button", { name: /Publish shift|Publishing/ });
    expect(button).toBeDisabled(); fireEvent.click(button); expect(onPublishShift).not.toHaveBeenCalled();
    if (patch.isSchedulePublished === false) expect(screen.getByText("Publish the schedule first.")).toBeVisible();
  });
  test("no employees, invalid cells or no selection cannot publish", () => {
    const onPublishShift = vi.fn();
    const { rerender } = render(<ContainerGraphManualColumnsCard {...base} columns={[column]} employees={[]} onPublishShift={onPublishShift} />);
    fireEvent.click(screen.getByRole("button", { name: "Specific employee" }));
    expect(screen.getByText("No employees available")).toBeVisible();
    expect(screen.getByRole("button", { name: "Publish shift" })).toBeDisabled();
    rerender(<ContainerGraphManualColumnsCard {...base} columns={[{ ...column, cells: { "1": "Text", "2": "12:00-12:00", "3": "25:00-26:00" } }]} onPublishShift={onPublishShift} />);
    fireEvent.click(screen.getByRole("button", { name: "Everyone" }));
    fireEvent.click(screen.getByRole("button", { name: "Publish shift" }));
    expect(onPublishShift).not.toHaveBeenCalled();
  });
  test("pending and open excluded, cancellations keep IDs, new pending expands and errors remain", () => {
    const onCancelPublishedShift = vi.fn(); const onCancelPendingPublishedShift = vi.fn();
    const props = { ...base, columns: [{ ...column, cells: { "1": "08:00-12:00", "2": "08:00-12:00", "3": "08:00-12:00" } }],
      publishedShifts: [offer], onCancelPublishedShift, onCancelPendingPublishedShift, publishError: "Original API error" };
    const { rerender } = render(<ContainerGraphManualColumnsCard {...props} />);
    expect(screen.queryByRole("button", { name: /01 Feb/ })).toBeNull();
    rerender(<ContainerGraphManualColumnsCard {...props} pendingPublishedShifts={[pending]} />);
    expect(screen.getByRole("button", { name: /Offered \(2\)/ })).toHaveAttribute("aria-expanded", "true");
    expect(screen.getByText("Pending save", { exact: false })).toBeVisible();
    expect(screen.queryByRole("button", { name: /^02 Feb/ })).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Remove shift on 02 Feb" })); expect(onCancelPendingPublishedShift).toHaveBeenCalledWith("pending-1");
    fireEvent.click(screen.getByRole("button", { name: "Cancel shift on 01 Feb" })); expect(onCancelPublishedShift).toHaveBeenCalledWith(20);
    expect(screen.getByRole("alert")).toHaveTextContent("Original API error");
    fireEvent.click(screen.getByRole("button", { name: /Offered \(2\)/ }));
    expect(screen.queryByText("Pending save", { exact: false })).toBeNull();
    expect(within(screen.getByRole("button", { name: /03 Feb/ })).getByText("Do obsadzenia")).toBeVisible();
  });
});

test("duplicate demand and combined selection respect capacity and send partial mode", () => {
  const publish = vi.fn(); const props = { ...base, columns: [{ ...column, cells: { "1": "09:00-15:00,09:00-15:00,15:00-21:00" } }], onPublishShift: publish };
  const view = render(<ContainerGraphManualColumnsCard {...props} />);
  const list = screen.getByRole("region", { name: "Available shifts" });
  expect(within(list).getAllByRole("button")).toHaveLength(4);
  fireEvent.click(within(list).getByRole("button", { name: /09:00–21:00/ }));
  fireEvent.click(screen.getByRole("button", { name: "Allow custom time" }));
  fireEvent.click(screen.getByRole("button", { name: "Publish shift" }));
  expect(publish).toHaveBeenCalledWith({ manualColumnId:4, dayOfMonth:1, fromTime:"09:00", toTime:"21:00", targetEmployeeId:null, allowPartialAcceptance:true });
  view.rerender(<ContainerGraphManualColumnsCard {...props} pendingPublishedShifts={[{ clientId:"reserved", manualColumnId:4, dayOfMonth:1, fromTime:"09:00", toTime:"21:00", allowPartialAcceptance:true }]} />);
  expect(within(list).getAllByRole("button")).toHaveLength(1);
  expect(within(list).getByRole("button")).toHaveTextContent("09:00–15:00");
});
