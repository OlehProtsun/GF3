import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { BrowserRouter } from "react-router-dom";
import { beforeEach, describe, expect, test, vi } from "vitest";
import { PasswordRecoveryPage } from "./PasswordRecoveryPage";

const mocks = vi.hoisted(() => ({
  sendPasswordResetCode: vi.fn(),
  confirmPasswordReset: vi.fn(),
}));

vi.mock("@entities/auth", () => ({
  authApi: {
    sendPasswordResetCode: mocks.sendPasswordResetCode,
    confirmPasswordReset: mocks.confirmPasswordReset,
  },
}));

function renderPage() {
  return render(
    <BrowserRouter>
      <PasswordRecoveryPage />
    </BrowserRouter>,
  );
}

describe("PasswordRecoveryPage numeric password policy", () => {
  beforeEach(() => {
    window.history.replaceState({}, "", "/password-recovery");
    mocks.sendPasswordResetCode.mockReset();
    mocks.confirmPasswordReset.mockReset().mockResolvedValue(undefined);
  });

  test("filters non-digits and submits only a valid numeric password", async () => {
    const user = userEvent.setup();
    renderPage();

    await user.type(screen.getByRole("textbox", { name: "Username" }), "worker.one");
    await user.type(screen.getByRole("textbox", { name: "Code" }), "123456");
    await user.type(screen.getByLabelText("New password"), "12a34b567");

    expect(screen.getByLabelText("New password")).toHaveValue("123456");
    const changePasswordButton = screen.getByRole("button", { name: "Change password" });
    expect(changePasswordButton).toBeEnabled();
    await user.click(changePasswordButton);

    await waitFor(() => expect(mocks.confirmPasswordReset).toHaveBeenCalledWith({
      username: "worker.one",
      code: "123456",
      newPassword: "123456",
    }));
  });

  test("keeps confirmation disabled for fewer than six digits", async () => {
    const user = userEvent.setup();
    renderPage();

    await user.type(screen.getByRole("textbox", { name: "Username" }), "worker.one");
    await user.type(screen.getByRole("textbox", { name: "Code" }), "123456");
    await user.type(screen.getByLabelText("New password"), "12345");

    expect(screen.getByRole("button", { name: "Change password" })).toBeDisabled();
    expect(mocks.confirmPasswordReset).not.toHaveBeenCalled();
  });
});
