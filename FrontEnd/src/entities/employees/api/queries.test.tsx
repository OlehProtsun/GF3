import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, test, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import {
  useCreateEmployeeMutation,
  useDeleteEmployeeMutation,
  useUpdateEmployeeMutation,
} from "./queries";

const apiMocks = vi.hoisted(() => ({
  list: vi.fn(),
  byId: vi.fn(),
  create: vi.fn(),
  update: vi.fn(),
  remove: vi.fn(),
}));

vi.mock("./employeesApi", () => ({
  employeesApi: apiMocks,
}));

function MutationHarness() {
  const createEmployee = useCreateEmployeeMutation();
  const updateEmployee = useUpdateEmployeeMutation();
  const deleteEmployee = useDeleteEmployeeMutation();

  return (
    <section>
      <button
        type="button"
        onClick={() => createEmployee.mutate({
          firstName: " Ada ",
          lastName: " Lovelace ",
          email: "ada@example.com",
        })}
      >
        create employee
      </button>
      <button
        type="button"
        onClick={() => updateEmployee.mutate({
          id: 7,
          payload: {
            firstName: "Grace",
            lastName: "Hopper",
            phone: "+48 123",
          },
        })}
      >
        update employee
      </button>
      <button type="button" onClick={() => deleteEmployee.mutate(7)}>
        delete employee
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
  apiMocks.create.mockResolvedValue({ id: 7 });
  apiMocks.update.mockResolvedValue(undefined);
  apiMocks.remove.mockResolvedValue(undefined);
});

describe("employee query mutations", () => {
  test("calls employee API mutations and invalidates list/detail caches", async () => {
    const user = userEvent.setup();
    const client = new QueryClient();
    const invalidateSpy = vi.spyOn(client, "invalidateQueries");
    renderHarness(client);

    await user.click(screen.getByRole("button", { name: "create employee" }));
    await waitFor(() => {
      expect(apiMocks.create).toHaveBeenCalledWith({
        firstName: " Ada ",
        lastName: " Lovelace ",
        email: "ada@example.com",
      });
    });

    await user.click(screen.getByRole("button", { name: "update employee" }));
    await waitFor(() => {
      expect(apiMocks.update).toHaveBeenCalledWith(7, {
        firstName: "Grace",
        lastName: "Hopper",
        phone: "+48 123",
      });
    });

    await user.click(screen.getByRole("button", { name: "delete employee" }));
    await waitFor(() => {
      expect(apiMocks.remove).toHaveBeenCalledWith(7);
    });

    expect(invalidateSpy.mock.calls.map(([filters]) => filters)).toEqual([
      { queryKey: queryKeys.employees.all },
      { queryKey: queryKeys.employees.all },
      { queryKey: queryKeys.employees.byId(7) },
      { queryKey: queryKeys.employees.all },
    ]);
  });
});
