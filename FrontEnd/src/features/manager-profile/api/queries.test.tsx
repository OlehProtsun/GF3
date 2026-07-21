import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, test, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import {
  useCreateManagerMutation,
  useDeleteManagerMutation,
  useUpdateManagerProfileMutation,
} from "./queries";

const apiMocks = vi.hoisted(() => ({
  current: vi.fn(),
  update: vi.fn(),
  listManagers: vi.fn(),
  createManager: vi.fn(),
  deleteManager: vi.fn(),
}));

vi.mock("./managerProfileApi", () => ({
  managerProfileApi: apiMocks,
}));

const updatedProfile = {
  id: 3,
  userName: "chief",
  displayName: "Chief Manager",
  recoveryEmail: "chief@example.com",
  lastLoginAtUtc: "2026-05-01T10:00:00.000Z",
  isOnline: true,
  isSystem: false,
  createdAtUtc: "2026-04-01T10:00:00.000Z",
};

function MutationHarness() {
  const updateProfile = useUpdateManagerProfileMutation();
  const createManager = useCreateManagerMutation();
  const deleteManager = useDeleteManagerMutation();

  return (
    <section>
      <button
        type="button"
        onClick={() => updateProfile.mutate({
          userName: " chief ",
          displayName: " Chief Manager ",
          recoveryEmail: " chief@example.com ",
          newPassword: "654321",
        })}
      >
        update profile
      </button>
      <button
        type="button"
        onClick={() => createManager.mutate({
          userName: "second",
          displayName: "Second Manager",
          recoveryEmail: "second@example.com",
          password: "654321",
        })}
      >
        create manager
      </button>
      <button type="button" onClick={() => deleteManager.mutate(4)}>
        delete manager
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
  apiMocks.update.mockResolvedValue({ profile: updatedProfile });
  apiMocks.createManager.mockResolvedValue({
    id: 4,
    userName: "second",
    displayName: "Second Manager",
    isOnline: false,
    isSystem: false,
    createdAtUtc: "2026-05-01T10:00:00.000Z",
  });
  apiMocks.deleteManager.mockResolvedValue(undefined);
});

describe("manager profile query mutations", () => {
  test("hydrates the current profile cache and invalidates the manager list after manager mutations", async () => {
    const user = userEvent.setup();
    const client = new QueryClient();
    const invalidateSpy = vi.spyOn(client, "invalidateQueries");
    renderHarness(client);

    await user.click(screen.getByRole("button", { name: "update profile" }));
    await waitFor(() => {
      expect(apiMocks.update).toHaveBeenCalledWith({
        userName: " chief ",
        displayName: " Chief Manager ",
        recoveryEmail: " chief@example.com ",
        newPassword: "654321",
      });
      expect(client.getQueryState(queryKeys.managerProfile.me()).data).toEqual(updatedProfile);
    });

    await user.click(screen.getByRole("button", { name: "create manager" }));
    await waitFor(() => {
      expect(apiMocks.createManager).toHaveBeenCalledWith({
        userName: "second",
        displayName: "Second Manager",
        recoveryEmail: "second@example.com",
        password: "654321",
      });
    });

    await user.click(screen.getByRole("button", { name: "delete manager" }));
    await waitFor(() => {
      expect(apiMocks.deleteManager).toHaveBeenCalledWith(4);
    });

    expect(invalidateSpy.mock.calls.map(([filters]) => filters)).toEqual([
      { queryKey: queryKeys.managerProfile.list() },
      { queryKey: queryKeys.managerProfile.list() },
      { queryKey: queryKeys.managerProfile.list() },
    ]);
  });
});
