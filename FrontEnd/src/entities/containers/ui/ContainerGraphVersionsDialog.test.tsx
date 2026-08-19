import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, test, vi } from "vitest";
import { ContainerGraphVersionsDialog } from "./ContainerGraphVersionsDialog";

const queryMocks = vi.hoisted(() => ({
  checkout: vi.fn(),
  remove: vi.fn(),
}));

vi.mock("@entities/containers/api/queries", () => ({
  useGraphVersionsQuery: () => ({
    data: {
      currentVersionId: 3,
      versions: [
        { id: 1, parentVersionId: null, versionNumber: 1, branchName: "main", createdAtUtc: "2026-08-19T08:00:00Z", authorName: "Import", employeeCount: 2, slotCount: 10, cellStyleCount: 1, isCurrent: false },
        { id: 2, parentVersionId: 1, versionNumber: 2, branchName: "main", createdAtUtc: "2026-08-19T09:00:00Z", authorName: "Ada", employeeCount: 2, slotCount: 12, cellStyleCount: 2, isCurrent: false },
        { id: 3, parentVersionId: 1, versionNumber: 3, branchName: "branch-2", createdAtUtc: "2026-08-19T10:00:00Z", authorName: "Ada", employeeCount: 3, slotCount: 14, cellStyleCount: 3, isCurrent: true },
      ],
    },
    isLoading: false,
    isError: false,
  }),
  useCheckoutGraphVersionMutation: () => ({ mutate: queryMocks.checkout, isPending: false, error: null }),
  useDeleteGraphVersionMutation: () => ({ mutate: queryMocks.remove, isPending: false, error: null }),
}));

describe("ContainerGraphVersionsDialog", () => {
  test("renders branches, connections and selected commit controls", async () => {
    const user = userEvent.setup();
    render(
      <ContainerGraphVersionsDialog
        open
        containerId={4}
        graphId={9}
        hasUnsavedChanges
        onCancel={() => undefined}
        onCheckoutComplete={() => undefined}
      />,
    );

    expect(screen.getByText("main")).toBeVisible();
    expect(screen.getAllByText("branch-2")).toHaveLength(2);
    expect(screen.getByTestId("version-connections").querySelectorAll("path")).toHaveLength(2);
    expect(screen.getByRole("button", { name: "Commit 3, branch-2, current" })).toHaveTextContent("A#3");
    expect(screen.getByRole("button", { name: "Switch" })).toBeDisabled();

    await user.click(screen.getByRole("button", { name: "Commit 2, main" }));
    expect(screen.getByText("Commit #2")).toBeVisible();
    await user.click(screen.getByRole("button", { name: "Switch" }));
    expect(screen.getByText(/discard the unsaved editor draft/i)).toBeVisible();
  });
});
