import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, test, vi } from "vitest";
import { EmployeeCommunicationDialog } from "./EmployeeCommunicationDialog";

const hookMocks = vi.hoisted(() => ({
  useEmployeePendingCommunicationsQuery: vi.fn(),
  useDismissEmployeeCommunicationMutation: vi.fn(),
  dismissMutate: vi.fn(),
}));

vi.mock("@entities/communications", () => ({
  useEmployeePendingCommunicationsQuery: hookMocks.useEmployeePendingCommunicationsQuery,
  useDismissEmployeeCommunicationMutation: hookMocks.useDismissEmployeeCommunicationMutation,
}));

const message = {
  id: 7,
  title: "Shift briefing",
  body: "Please read this before starting.",
  visibleFromUtc: "2026-06-25T12:00:00.000Z",
  deadlineAtUtc: "2026-06-26T12:00:00.000Z",
  createdAtUtc: "2026-06-25T12:00:00.000Z",
  createdByManagerName: "Manager",
  isActive: true,
};

beforeEach(() => {
  hookMocks.useEmployeePendingCommunicationsQuery.mockReset();
  hookMocks.useDismissEmployeeCommunicationMutation.mockReset();
  hookMocks.dismissMutate.mockReset();
  hookMocks.useEmployeePendingCommunicationsQuery.mockReturnValue({
    data: [message],
    isLoading: false,
    error: null,
  });
  hookMocks.dismissMutate.mockImplementation((_id: number, callbacks?: { onSuccess?: () => void }) => {
    callbacks?.onSuccess?.();
  });
  hookMocks.useDismissEmployeeCommunicationMutation.mockReturnValue({
    mutate: hookMocks.dismissMutate,
    isPending: false,
    error: null,
  });
});

describe("EmployeeCommunicationDialog", () => {
  test("hides the message for the current session when closed without persisting dismissal", async () => {
    const user = userEvent.setup();
    render(<EmployeeCommunicationDialog employeeId={11} />);

    expect(screen.getByRole("dialog", { name: "Shift briefing" })).toBeInTheDocument();
    expect(hookMocks.useEmployeePendingCommunicationsQuery).toHaveBeenCalledWith(11);

    await user.click(screen.getByRole("button", { name: "Close" }));

    expect(hookMocks.dismissMutate).not.toHaveBeenCalled();
    await waitFor(() => {
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });
  });

  test("persists dismissal when the employee chooses not to show the message again", async () => {
    const user = userEvent.setup();
    render(<EmployeeCommunicationDialog employeeId={11} />);

    await user.click(screen.getByRole("checkbox", { name: "Don't show this again" }));
    await user.click(screen.getByRole("button", { name: "Close" }));

    expect(hookMocks.dismissMutate).toHaveBeenCalledWith(7, expect.objectContaining({
      onSuccess: expect.any(Function),
      onError: expect.any(Function),
    }));
  });

  test("navigates multiple communications with arrows and pagination dots", async () => {
    const user = userEvent.setup();
    hookMocks.useEmployeePendingCommunicationsQuery.mockReturnValue({
      data: [
        message,
        {
          ...message,
          id: 8,
          title: "Second update",
          body: "The second communication.",
        },
      ],
      isLoading: false,
      error: null,
    });

    render(<EmployeeCommunicationDialog employeeId={11} />);

    await user.click(screen.getByRole("button", { name: "Next communication" }));
    expect(screen.getByRole("dialog", { name: "Second update" })).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Show communication 1 of 2" }));
    expect(screen.getByRole("dialog", { name: "Shift briefing" })).toBeInTheDocument();

    const dialog = screen.getByRole("dialog", { name: "Shift briefing" });
    fireEvent.pointerDown(dialog, { clientX: 320 });
    fireEvent.pointerUp(dialog, { clientX: 120 });
    expect(screen.getByRole("dialog", { name: "Second update" })).toBeInTheDocument();
  });
});
