import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { BrowserRouter } from "react-router-dom";
import { beforeEach, describe, expect, test, vi } from "vitest";
import { LoginPage } from "./LoginPage";

const mocks = vi.hoisted(() => ({
  login: vi.fn(),
}));

vi.mock("@app/providers/AuthProvider", () => ({
  useAuth: () => ({ bootstrapError: null, login: mocks.login }),
}));

function renderPage() {
  return render(
    <BrowserRouter>
      <LoginPage />
    </BrowserRouter>,
  );
}

describe("LoginPage password modes", () => {
  beforeEach(() => {
    window.localStorage.clear();
    mocks.login.mockReset().mockResolvedValue({ role: "manager" });
  });

  test("restores the last username and password mode", () => {
    window.localStorage.setItem("gf3.auth.last-username", "saved.manager");
    window.localStorage.setItem("gf3.auth.password-mode", "phone");

    renderPage();

    expect(screen.getByRole("textbox", { name: "Username" })).toHaveValue("saved.manager");
    expect(screen.getByRole("button", { name: "Phone" })).toHaveAttribute("aria-pressed", "true");
    expect(screen.getByLabelText("Numeric keypad")).toBeInTheDocument();
  });

  test("persists username and mode while keeping PC input numeric", async () => {
    const user = userEvent.setup();
    renderPage();

    await user.type(screen.getByRole("textbox", { name: "Username" }), "manager.two");
    await user.type(screen.getByLabelText("Password"), "12a34b567");
    await user.click(screen.getByRole("button", { name: "Phone" }));

    expect(window.localStorage.getItem("gf3.auth.last-username")).toBe("manager.two");
    expect(window.localStorage.getItem("gf3.auth.password-mode")).toBe("phone");
  });

  test("submits automatically after six phone digits", async () => {
    const user = userEvent.setup();
    window.localStorage.setItem("gf3.auth.last-username", "manager");
    window.localStorage.setItem("gf3.auth.password-mode", "phone");
    renderPage();

    for (const digit of ["1", "2", "3", "4", "5", "6"]) {
      await user.click(screen.getByRole("button", { name: digit }));
    }

    await waitFor(() => expect(mocks.login).toHaveBeenCalledWith({ username: "manager", password: "123456" }));
  });
});
