import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, test, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import {
  useCreateShopMutation,
  useDeleteShopMutation,
  useUpdateShopMutation,
} from "./queries";

const apiMocks = vi.hoisted(() => ({
  list: vi.fn(),
  byId: vi.fn(),
  create: vi.fn(),
  update: vi.fn(),
  remove: vi.fn(),
}));

vi.mock("./shopsApi", () => ({
  shopsApi: apiMocks,
}));

function MutationHarness() {
  const createShop = useCreateShopMutation();
  const updateShop = useUpdateShopMutation();
  const deleteShop = useDeleteShopMutation();

  return (
    <section>
      <button
        type="button"
        onClick={() => createShop.mutate({
          name: " Central ",
          address: " Main Street ",
          description: " Flagship ",
        })}
      >
        create shop
      </button>
      <button
        type="button"
        onClick={() => updateShop.mutate({
          id: 4,
          payload: {
            name: "Central Updated",
            address: "Second Street",
            description: "",
          },
        })}
      >
        update shop
      </button>
      <button type="button" onClick={() => deleteShop.mutate(4)}>
        delete shop
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
  apiMocks.create.mockResolvedValue({ id: 4 });
  apiMocks.update.mockResolvedValue(undefined);
  apiMocks.remove.mockResolvedValue(undefined);
});

describe("shop query mutations", () => {
  test("calls shop API mutations and invalidates list/detail caches", async () => {
    const user = userEvent.setup();
    const client = new QueryClient();
    const invalidateSpy = vi.spyOn(client, "invalidateQueries");
    renderHarness(client);

    await user.click(screen.getByRole("button", { name: "create shop" }));
    await waitFor(() => {
      expect(apiMocks.create).toHaveBeenCalledWith({
        name: " Central ",
        address: " Main Street ",
        description: " Flagship ",
      });
    });

    await user.click(screen.getByRole("button", { name: "update shop" }));
    await waitFor(() => {
      expect(apiMocks.update).toHaveBeenCalledWith(4, {
        name: "Central Updated",
        address: "Second Street",
        description: "",
      });
    });

    await user.click(screen.getByRole("button", { name: "delete shop" }));
    await waitFor(() => {
      expect(apiMocks.remove).toHaveBeenCalledWith(4);
    });

    expect(invalidateSpy.mock.calls.map(([filters]) => filters)).toEqual([
      { queryKey: queryKeys.shops.all },
      { queryKey: queryKeys.shops.all },
      { queryKey: queryKeys.shops.byId(4) },
      { queryKey: queryKeys.shops.all },
    ]);
  });
});
