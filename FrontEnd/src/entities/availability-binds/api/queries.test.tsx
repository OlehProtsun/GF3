import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, test, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import {
  useCreateAvailabilityBindMutation,
  useDeleteAvailabilityBindMutation,
  useUpdateAvailabilityBindMutation,
} from "./queries";

const apiMocks = vi.hoisted(() => ({
  list: vi.fn(),
  active: vi.fn(),
  byId: vi.fn(),
  create: vi.fn(),
  update: vi.fn(),
  remove: vi.fn(),
}));

vi.mock("./availabilityBindsApi", () => ({
  availabilityBindsApi: apiMocks,
}));

function MutationHarness() {
  const createBind = useCreateAvailabilityBindMutation();
  const updateBind = useUpdateAvailabilityBindMutation();
  const deleteBind = useDeleteAvailabilityBindMutation();

  return (
    <section>
      <button
        type="button"
        onClick={() => createBind.mutate({ key: " A ", value: " + ", isActive: true })}
      >
        create bind
      </button>
      <button
        type="button"
        onClick={() => updateBind.mutate({ id: 7, payload: { key: "A", value: "09:00 - 13:00", isActive: false } })}
      >
        update bind
      </button>
      <button type="button" onClick={() => deleteBind.mutate(7)}>
        delete bind
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
  apiMocks.create.mockResolvedValue({ id: 7, key: "A", value: "+", isActive: true });
  apiMocks.update.mockResolvedValue(undefined);
  apiMocks.remove.mockResolvedValue(undefined);
});

describe("availability bind query mutations", () => {
  test("calls the bind API and invalidates list plus detail caches after create, update, and delete", async () => {
    const user = userEvent.setup();
    const client = new QueryClient();
    const invalidateSpy = vi.spyOn(client, "invalidateQueries");
    renderHarness(client);

    await user.click(screen.getByRole("button", { name: "create bind" }));
    await waitFor(() => {
      expect(apiMocks.create).toHaveBeenCalledWith({ key: " A ", value: " + ", isActive: true });
    });

    await user.click(screen.getByRole("button", { name: "update bind" }));
    await waitFor(() => {
      expect(apiMocks.update).toHaveBeenCalledWith(7, { key: "A", value: "09:00 - 13:00", isActive: false });
    });

    await user.click(screen.getByRole("button", { name: "delete bind" }));
    await waitFor(() => {
      expect(apiMocks.remove).toHaveBeenCalledWith(7);
    });

    expect(invalidateSpy.mock.calls.map(([filters]) => filters)).toEqual([
      { queryKey: queryKeys.availabilityBinds.all },
      { queryKey: queryKeys.availabilityBinds.byId(7) },
      { queryKey: queryKeys.availabilityBinds.all },
      { queryKey: queryKeys.availabilityBinds.byId(7) },
      { queryKey: queryKeys.availabilityBinds.all },
      { queryKey: queryKeys.availabilityBinds.byId(7) },
    ]);
  });
});
