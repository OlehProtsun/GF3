import { fireEvent, render, screen, waitFor } from "@testing-library/react";
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

  test("legal links are ordinary navigation and never submit credentials", () => {
    renderPage();
    for (const [name, href] of [
      ["Legal documents", "/legal/index.html"], ["Terms", "/legal/regulamin.html"],
      ["Privacy policy", "/legal/polityka-prywatnosci.html"], ["Cookies", "/legal/pliki-cookies.html"],
    ]) {
      const link = screen.getByRole("link", { name });
      expect(link).toHaveAttribute("href", href);
      fireEvent.click(link);
    }
    expect(mocks.login).not.toHaveBeenCalled();
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

  test("handles rapid phone pointer presses without losing digits", async () => {
    window.localStorage.setItem("gf3.auth.last-username", "manager");
    window.localStorage.setItem("gf3.auth.password-mode", "phone");
    renderPage();

    for (const digit of ["1", "2", "3", "4", "5", "6"]) {
      fireEvent.pointerDown(screen.getByRole("button", { name: digit }), { pointerType: "touch", button: 0 });
    }

    await waitFor(() => expect(mocks.login).toHaveBeenCalledWith({ username: "manager", password: "123456" }));
  });

  test("preserves zoom and restores the page viewport in phone mode", () => {
    const viewportMeta = document.querySelector<HTMLMetaElement>('meta[name="viewport"]') ?? document.createElement("meta");
    viewportMeta.name = "viewport";
    viewportMeta.content = "width=device-width, initial-scale=1.0, maximum-scale=5.0, user-scalable=yes";
    if (!viewportMeta.isConnected) document.head.appendChild(viewportMeta);
    window.localStorage.setItem("gf3.auth.password-mode", "phone");

    const page = renderPage();

    expect(document.documentElement).toHaveClass("login-phone-mode");
    expect(document.body).toHaveClass("login-phone-mode");
    expect(viewportMeta.content).not.toContain("user-scalable=no");
    expect(viewportMeta.content).not.toContain("maximum-scale=1.0");

    page.unmount();

    expect(document.documentElement).not.toHaveClass("login-phone-mode");
    expect(document.body).not.toHaveClass("login-phone-mode");
    expect(viewportMeta.content).toContain("user-scalable=yes");
  });
});
