import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { StrictMode } from "react";
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
    <StrictMode>
      <BrowserRouter>
        <LoginPage />
      </BrowserRouter>
    </StrictMode>,
  );
}

describe("LoginPage password modes", () => {
  beforeEach(() => {
    Object.defineProperty(HTMLDialogElement.prototype, "showModal", { configurable: true, value: function (this: HTMLDialogElement) { this.open = true; } });
    Object.defineProperty(HTMLDialogElement.prototype, "close", { configurable: true, value: function (this: HTMLDialogElement) {
      this.open = false;
      this.dispatchEvent(new Event("close"));
    } });
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
    expect(screen.getByRole("button", { name: "Enter password" })).toBeEnabled();
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(screen.getByLabelText("Numeric keypad")).not.toBeVisible();
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

    fireEvent.click(screen.getByRole("button", { name: "Enter password" }));
    for (const digit of ["1", "2", "3", "4", "5", "6"]) {
      await user.click(within(screen.getByRole("dialog")).getByRole("button", { name: digit }));
    }

    await waitFor(() => expect(mocks.login).toHaveBeenCalledWith({ username: "manager", password: "123456" }));
    expect(mocks.login).toHaveBeenCalledTimes(1);
  });

  test("handles rapid phone pointer presses without losing digits", async () => {
    window.localStorage.setItem("gf3.auth.last-username", "manager");
    window.localStorage.setItem("gf3.auth.password-mode", "phone");
    renderPage();

    fireEvent.click(screen.getByRole("button", { name: "Enter password" }));
    for (const digit of ["1", "2", "3", "4", "5", "6"]) {
      fireEvent.pointerDown(within(screen.getByRole("dialog")).getByRole("button", { name: digit }), { pointerType: "touch", button: 0 });
    }

    await waitFor(() => expect(mocks.login).toHaveBeenCalledWith({ username: "manager", password: "123456" }));
    expect(mocks.login).toHaveBeenCalledTimes(1);
  });

  test("requires username before opening and preserves storage", async () => {
    const user = userEvent.setup();
    renderPage();
    await user.click(screen.getByRole("button", { name: "Phone" }));
    const launch = screen.getByRole("button", { name: "Enter password" });
    expect(launch).toBeDisabled();
    await user.type(screen.getByRole("textbox", { name: "Username" }), "manager");
    expect(launch).toBeEnabled();
    expect(localStorage.getItem("gf3.auth.last-username")).toBe("manager");
    expect(localStorage.getItem("gf3.auth.password-mode")).toBe("phone");
  });

  function openPhone() {
    localStorage.setItem("gf3.auth.last-username", "manager");
    localStorage.setItem("gf3.auth.password-mode", "phone");
    const page = renderPage();
    fireEvent.click(screen.getByRole("button", { name: "Enter password" }));
    return page;
  }

  test("close and native Escape clear partial PIN and mode changes remove the dialog", () => {
    openPhone();
    fireEvent.keyDown(screen.getByRole("dialog"), { key: "1" });
    fireEvent.click(screen.getByRole("button", { name: "Close" }));
    expect(screen.queryByRole("dialog")).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Enter password" }));
    expect(screen.getByRole("status", { name: "0 of 6 digits entered" })).toBeInTheDocument();
    const dialog = screen.getByRole("dialog") as HTMLDialogElement;
    expect(fireEvent(dialog, new Event("cancel", { cancelable: true }))).toBe(true);
    dialog.close();
    expect(screen.queryByRole("dialog")).toBeNull();
    expect(screen.getByRole("textbox", { name: "Username" })).toHaveValue("manager");
    fireEvent.click(screen.getByRole("button", { name: "PC" }));
    expect(screen.queryByLabelText("Numeric keypad")).toBeNull();
  });

  test("delete, keyboard, pending controls and pointer-click dedup submit once", async () => {
    let finish!: () => void;
    mocks.login.mockImplementation(() => new Promise<void>(resolve => { finish = resolve; }));
    openPhone();
    const dialog = screen.getByRole("dialog");
    fireEvent.keyDown(dialog, { key: "Backspace" });
    const one = within(dialog).getByRole("button", { name: "1" });
    fireEvent.pointerDown(one, { pointerType: "touch", button: 0 });
    fireEvent.click(one);
    expect(screen.getByRole("status", { name: "1 of 6 digits entered" })).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Delete last digit" }));
    expect(screen.getByRole("status", { name: "0 of 6 digits entered" })).toBeInTheDocument();
    for (const key of "12345") fireEvent.keyDown(dialog, { key });
    expect(mocks.login).not.toHaveBeenCalled();
    fireEvent.keyDown(dialog, { key: "9" });
    fireEvent.keyDown(dialog, { key: "7" });
    expect(mocks.login).toHaveBeenCalledExactlyOnceWith({ username: "manager", password: "123459" });
    for (const name of ["1", "Delete last digit", "Close"]) expect(within(dialog).getByRole("button", { name })).toBeDisabled();
    expect(within(dialog).getByText(/^Checking/)).toBeInTheDocument();
    expect(fireEvent(dialog, new Event("cancel", { cancelable: true }))).toBe(false);
    expect(dialog).toHaveAttribute("open");
    finish();
    await waitFor(() => expect(screen.getByRole("button", { name: "Close" })).toBeEnabled());
  });

  test("invalid credentials remain inside dialog and retry succeeds", async () => {
    mocks.login.mockRejectedValueOnce(new Error("Invalid credentials"));
    openPhone();
    const dialog = screen.getByRole("dialog");
    for (const key of "123456") fireEvent.keyDown(dialog, { key });
    await waitFor(() => expect(within(dialog).getByRole("alert")).toHaveTextContent("Invalid credentials"));
    expect(screen.getAllByRole("alert")).toHaveLength(1);
    expect(dialog).toHaveAttribute("open");
    expect(screen.getByRole("status", { name: "0 of 6 digits entered" })).toBeInTheDocument();
    for (const key of "654321") fireEvent.keyDown(dialog, { key });
    await waitFor(() => expect(mocks.login).toHaveBeenCalledTimes(2));
    expect(mocks.login).toHaveBeenLastCalledWith({ username: "manager", password: "654321" });
    expect(localStorage.getItem("gf3.auth.password-mode")).toBe("phone");
    expect(localStorage.getItem("gf3.auth.last-username")).toBe("manager");
  });

  test("unmount closes native modal and removes phone body lock", () => {
    const page = openPhone();
    const dialog = screen.getByRole("dialog") as HTMLDialogElement;
    expect(dialog.open).toBe(true);
    page.unmount();
    expect(dialog.open).toBe(false);
    expect(document.body).not.toHaveClass("login-phone-mode");
    expect(document.body.style.position).not.toBe("fixed");
  });

  test("PC keeps sanitized input and manual submission", async () => {
    const user = userEvent.setup();
    renderPage();
    await user.type(screen.getByRole("textbox", { name: "Username" }), "manager");
    const input = screen.getByLabelText("Password");
    expect(input).toHaveAttribute("inputmode", "numeric");
    fireEvent.change(input, { target: { value: "12a34b567" } });
    expect(input).toHaveValue("123456");
    expect(mocks.login).not.toHaveBeenCalled();
    await user.click(screen.getByRole("button", { name: "Sign in", exact: true }));
    expect(mocks.login).toHaveBeenCalledExactlyOnceWith({ username: "manager", password: "123456" });
    expect(screen.queryByRole("dialog")).toBeNull();
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
