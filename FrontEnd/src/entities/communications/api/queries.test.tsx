import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, test, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import {
  useCreateCommunicationMutation,
  useDeleteCommunicationMutation,
  useDismissEmployeeCommunicationMutation,
  useUpdateCommunicationMutation,
} from "./queries";

const apiMocks = vi.hoisted(() => ({
  listForManager: vi.fn(),
  create: vi.fn(),
  update: vi.fn(),
  delete: vi.fn(),
  pendingForEmployee: vi.fn(),
  dismissForEmployee: vi.fn(),
}));

vi.mock("./communicationsApi", () => ({
  communicationsApi: apiMocks,
}));

function MutationHarness() {
  const createCommunication = useCreateCommunicationMutation();
  const updateCommunication = useUpdateCommunicationMutation();
  const deleteCommunication = useDeleteCommunicationMutation();
  const dismissCommunication = useDismissEmployeeCommunicationMutation();

  return (
    <section>
      <button
        type="button"
        onClick={() => createCommunication.mutate({
          title: " Meeting ",
          body: " Read before shift ",
          visibleFromUtc: "2026-06-25T12:00:00.000Z",
          deadlineAtUtc: "2026-06-26T12:00:00.000Z",
        })}
      >
        create communication
      </button>
      <button type="button" onClick={() => dismissCommunication.mutate(7)}>
        dismiss communication
      </button>
      <button
        type="button"
        onClick={() => updateCommunication.mutate({
          id: 7,
          title: "Updated",
          body: "Updated body",
          visibleFromUtc: "2026-06-25T12:00:00.000Z",
          deadlineAtUtc: "2026-06-27T12:00:00.000Z",
        })}
      >
        update communication
      </button>
      <button type="button" onClick={() => deleteCommunication.mutate(7)}>
        delete communication
      </button>
    </section>
  );
}

function renderHarness(client: QueryClient) {
  return render(
    <QueryClientProvider client={client}>
      <MutationHarness />
    </QueryClientProvider>,
  );
}

beforeEach(() => {
  Object.values(apiMocks).forEach(mock => mock.mockReset());
  apiMocks.create.mockResolvedValue({
    id: 7,
    title: "Meeting",
    body: "Read before shift",
    visibleFromUtc: "2026-06-25T12:00:00.000Z",
    deadlineAtUtc: "2026-06-26T12:00:00.000Z",
    createdAtUtc: "2026-06-25T12:00:00.000Z",
    createdByManagerName: "Manager",
    isActive: true,
  });
  apiMocks.dismissForEmployee.mockResolvedValue(undefined);
  apiMocks.update.mockResolvedValue({
    id: 7,
    title: "Updated",
    body: "Updated body",
    visibleFromUtc: "2026-06-25T12:00:00.000Z",
    deadlineAtUtc: "2026-06-27T12:00:00.000Z",
    createdAtUtc: "2026-06-25T12:00:00.000Z",
    createdByManagerName: "Manager",
    isActive: true,
  });
  apiMocks.delete.mockResolvedValue(undefined);
});

describe("communication query mutations", () => {
  test("invalidates manager and employee communication caches after mutations", async () => {
    const user = userEvent.setup();
    const client = new QueryClient();
    const invalidateSpy = vi.spyOn(client, "invalidateQueries");
    renderHarness(client);

    await user.click(screen.getByRole("button", { name: "create communication" }));
    await waitFor(() => {
      expect(apiMocks.create).toHaveBeenCalledWith({
        title: " Meeting ",
        body: " Read before shift ",
        visibleFromUtc: "2026-06-25T12:00:00.000Z",
        deadlineAtUtc: "2026-06-26T12:00:00.000Z",
      });
    });

    await user.click(screen.getByRole("button", { name: "dismiss communication" }));
    await waitFor(() => {
      expect(apiMocks.dismissForEmployee).toHaveBeenCalledWith(7);
    });

    await user.click(screen.getByRole("button", { name: "update communication" }));
    await user.click(screen.getByRole("button", { name: "delete communication" }));

    await waitFor(() => {
      expect(apiMocks.update).toHaveBeenCalledWith(expect.objectContaining({ id: 7, title: "Updated" }));
      expect(apiMocks.delete).toHaveBeenCalledWith(7);
    });

    expect(invalidateSpy.mock.calls.map(([filters]) => filters)).toEqual([
      { queryKey: queryKeys.communications.managerList() },
      { queryKey: queryKeys.communications.employeePendingAll },
      { queryKey: queryKeys.communications.managerList() },
      { queryKey: queryKeys.communications.employeePendingAll },
      { queryKey: queryKeys.communications.managerList() },
      { queryKey: queryKeys.communications.employeePendingAll },
    ]);
  });
});
