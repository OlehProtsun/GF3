import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, test, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import {
  useConfirmEmployeePasswordResetMutation,
  useSendEmployeePasswordResetCodeMutation,
  useUpdateEmployeeProfileMutation,
} from "./queries";

const apiMocks = vi.hoisted(() => ({
  current: vi.fn(),
  update: vi.fn(),
  sendPasswordResetCode: vi.fn(),
  confirmPasswordReset: vi.fn(),
}));

vi.mock("./employeeProfileApi", () => ({
  employeeProfileApi: apiMocks,
}));

const updatedProfile = {
  employeeId: 7,
  username: "alice",
  displayName: "Alice Brown",
  recoveryEmail: "alice@example.com",
  phone: "+48 123",
};

function MutationHarness() {
  const updateProfile = useUpdateEmployeeProfileMutation();
  const sendCode = useSendEmployeePasswordResetCodeMutation();
  const confirmReset = useConfirmEmployeePasswordResetMutation();

  return (
    <section>
      <button
        type="button"
        onClick={() => updateProfile.mutate({ recoveryEmail: " alice@example.com ", phone: " +48 123 " })}
      >
        update profile
      </button>
      <button type="button" onClick={() => sendCode.mutate(undefined)}>
        send code
      </button>
      <button
        type="button"
        onClick={() => confirmReset.mutate({ code: " 123456 ", newPassword: "secret123" })}
      >
        confirm reset
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
  apiMocks.update.mockResolvedValue(updatedProfile);
  apiMocks.sendPasswordResetCode.mockResolvedValue({
    deliveryHint: "a***@example.com",
    expiresAtUtc: "2026-05-01T10:15:00.000Z",
  });
  apiMocks.confirmPasswordReset.mockResolvedValue(undefined);
});

describe("employee profile query mutations", () => {
  test("hydrates the current employee profile cache and delegates password reset actions", async () => {
    const user = userEvent.setup();
    const client = new QueryClient();
    renderHarness(client);

    await user.click(screen.getByRole("button", { name: "update profile" }));
    await waitFor(() => {
      expect(apiMocks.update).toHaveBeenCalledWith({ recoveryEmail: " alice@example.com ", phone: " +48 123 " });
      expect(client.getQueryState(queryKeys.employeeProfile.me()).data).toEqual(updatedProfile);
    });

    await user.click(screen.getByRole("button", { name: "send code" }));
    await waitFor(() => {
      expect(apiMocks.sendPasswordResetCode).toHaveBeenCalledTimes(1);
    });

    await user.click(screen.getByRole("button", { name: "confirm reset" }));
    await waitFor(() => {
      expect(apiMocks.confirmPasswordReset).toHaveBeenCalledWith({ code: " 123456 ", newPassword: "secret123" });
    });
  });
});
