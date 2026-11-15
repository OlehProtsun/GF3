import { fireEvent, render, screen, within } from "@testing-library/react";
import { beforeEach, expect, test, vi } from "vitest";
import { setLanguage } from "@shared/i18n";
import type { UrlopRequest } from "@entities/urlop";
import { ManagerUrlopRequestsPanel } from "./ManagerUrlopRequestsPanel";

const request = (requestId: string, status: UrlopRequest["status"] = "pending", days = [3,5,7]): UrlopRequest => ({ requestId, employeeId: 7, employeeName: `Employee ${requestId}`, shopId: 1, year: 2026, month: 11, days, status, createdAtUtc: `2026-11-0${requestId}T10:00:00Z`, reviewedAtUtc: null, createdByManagerId: null, reviewedByManagerId: null });
const callbacks = () => ({ onApprove: vi.fn(), onReject: vi.fn(), onEdit: vi.fn(), onDelete: vi.fn(), onSetMonthOpen: vi.fn() });
beforeEach(() => setLanguage("en"));
test("distinct pending request count, default filter, truthful nonconsecutive dates and All history", () => {
  const pending = request("1");
  render(<ManagerUrlopRequestsPanel requests={[pending, pending, request("2", "approved"), request("3", "rejected")]} isOpen={false} busy={false} readOnly={false} {...callbacks()} />);
  expect(screen.getByLabelText("Pending: 1")).toHaveTextContent("1");
  expect(screen.getAllByRole("article")).toHaveLength(1);
  expect(screen.getByText("3, 5, 7 November 2026")).toBeInTheDocument();
  expect(screen.getByText(/3 days/)).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "All", exact: true }));
  const cards = screen.getAllByRole("article");
  expect(cards).toHaveLength(3);
  expect(cards[0]).toHaveTextContent("Employee 3");
  expect(within(cards[0]).queryByRole("button", { name: "Edit dates" })).toBeNull();
  expect(screen.getAllByRole("button", { name: "Edit dates" })).toHaveLength(2);
  expect(screen.getAllByRole("button", { name: "Delete" })).toHaveLength(3);
});
test("callbacks use exact request and month state; Pending sorts oldest first", () => {
  const handlers = callbacks(); const first = request("1");
  render(<ManagerUrlopRequestsPanel requests={[request("2"), first]} isOpen={true} busy={false} readOnly={false} {...handlers} />);
  const card = screen.getAllByRole("article")[0];
  expect(card).toHaveTextContent("Employee 1");
  for (const action of ["Approve", "Reject", "Edit dates", "Delete"]) fireEvent.click(within(card).getByRole("button", { name: action, exact: true }));
  expect(handlers.onApprove).toHaveBeenCalledExactlyOnceWith("1");
  expect(handlers.onReject).toHaveBeenCalledExactlyOnceWith("1");
  expect(handlers.onEdit).toHaveBeenCalledExactlyOnceWith(first);
  expect(handlers.onDelete).toHaveBeenCalledExactlyOnceWith("1");
  fireEvent.click(screen.getByRole("button", { name: "Close requests" }));
  expect(handlers.onSetMonthOpen).toHaveBeenCalledExactlyOnceWith(false);
});
test.each([{ busy: true, readOnly: false }, { busy: false, readOnly: true }])("writes disabled for %j", flags => {
  const handlers = callbacks();
  render(<ManagerUrlopRequestsPanel requests={[request("1")]} isOpen={false} {...flags} {...handlers} />);
  for (const action of ["Approve", "Reject", "Edit dates", "Delete", "Open requests"]) {
    const button = screen.getByRole("button", { name: action, exact: true });
    expect(button).toBeDisabled(); fireEvent.click(button);
  }
  for (const callback of Object.values(handlers)) expect(callback).not.toHaveBeenCalled();
});
test("empty filters and unique day count", () => {
  const { rerender } = render(<ManagerUrlopRequestsPanel requests={[]} isOpen={false} busy={false} readOnly={false} {...callbacks()} />);
  expect(screen.getByText("No pending vacation requests.")).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "All", exact: true }));
  expect(screen.getByText("No vacation requests.")).toBeInTheDocument();
  rerender(<ManagerUrlopRequestsPanel requests={[request("1", "approved", [3,3,5])]} isOpen={false} busy={false} readOnly={false} {...callbacks()} />);
  expect(screen.getByText(/2 days/)).toBeInTheDocument();
});

test("compact ranges, visible status text and loading retain filter state", () => {
  const { rerender } = render(<ManagerUrlopRequestsPanel requests={[request("1", "pending", [8, 1, 2, 5, 7, 8])]} isOpen={false} busy={false} readOnly={false} {...callbacks()} />);
  expect(screen.getByRole("article")).toHaveTextContent("1–2, 5, 7–8 November 2026");
  expect(screen.getByRole("article").querySelector('[data-urlop-status="pending"]')).toHaveTextContent("Pending");
  expect(screen.getByRole("article")).toHaveTextContent("5 days");
  expect(screen.getByRole("button", { name: "Approve", exact: true })).toHaveTextContent("Approve");
  fireEvent.click(screen.getByRole("button", { name: "All", exact: true }));
  rerender(<ManagerUrlopRequestsPanel requests={[]} loading isOpen={false} busy={false} readOnly={false} {...callbacks()} />);
  expect(screen.getByRole("region", { name: "Vacation requests" })).toHaveAttribute("aria-busy", "true");
  expect(screen.getByRole("button", { name: "All", exact: true })).toHaveAttribute("aria-pressed", "true");
  expect(screen.getByRole("button", { name: "Open requests" })).toBeDisabled();
});
