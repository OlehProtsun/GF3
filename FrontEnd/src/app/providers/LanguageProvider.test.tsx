import { act, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useState } from "react";
import { LanguageProvider } from "./LanguageProvider";
import { LanguageSelector } from "@shared/i18n/LanguageSelector";
import { useLanguageRevision } from "@shared/i18n/useLanguageRevision";
import { getLanguage, setLanguage, t } from "@shared/i18n";

const mocks = vi.hoisted(() => ({ request: vi.fn(), session: { role: "manager", managerId: 1, employeeId: undefined as number | undefined } }));
vi.mock("./AuthProvider", () => ({ useAuth: () => ({ session: mocks.session }) }));
vi.mock("@shared/api/httpClient", () => ({ request: (...args: unknown[]) => mocks.request(...args) }));

function Profile() {
  useLanguageRevision();
  const [draft, setDraft] = useState("");
  return <><h1>{t("Profile access")}</h1><input aria-label="Draft" value={draft} onChange={event => setDraft(event.target.value)} /><LanguageSelector /></>;
}
const view = () => <LanguageProvider><Profile /></LanguageProvider>;

beforeEach(() => {
  mocks.request.mockReset();
  mocks.session = { role: "manager", managerId: 1, employeeId: undefined };
  setLanguage("en");
});
afterEach(() => { cleanup(); setLanguage("en"); });

describe("language preference lifecycle", () => {
  it("loads and saves the preference without losing an unsaved profile draft", async () => {
    mocks.request.mockResolvedValueOnce({ language: "en" }).mockResolvedValueOnce({ language: "pl" }).mockResolvedValueOnce({ language: "en" });
    render(view());
    await screen.findByText("Profile access");
    fireEvent.change(screen.getByLabelText("Draft"), { target: { value: "Uncommitted name" } });
    fireEvent.change(screen.getByLabelText("Application language"), { target: { value: "pl" } });
    await screen.findByText("Dostęp do profilu");
    expect(screen.getByLabelText("Draft")).toHaveValue("Uncommitted name");
    expect(mocks.request).toHaveBeenCalledWith("account-language", expect.objectContaining({ method: "PUT", body: { language: "pl" } }));
    fireEvent.change(screen.getByLabelText("Język aplikacji"), { target: { value: "en" } });
    await screen.findByText("Profile access");
    expect(screen.getByLabelText("Draft")).toHaveValue("Uncommitted name");
  });

  it("keeps the selected language when saving fails and allows retry", async () => {
    mocks.request.mockResolvedValueOnce({ language: "en" }).mockRejectedValueOnce(new Error("offline")).mockResolvedValueOnce({ language: "pl" });
    render(view());
    await screen.findByText("Profile access");
    fireEvent.change(screen.getByLabelText("Application language"), { target: { value: "pl" } });
    await screen.findByRole("alert");
    expect(getLanguage()).toBe("en");
    expect(screen.getByLabelText("Application language")).toHaveValue("en");
    fireEvent.change(screen.getByLabelText("Application language"), { target: { value: "pl" } });
    await screen.findByText("Dostęp do profilu");
  });

  it("ignores a stale load when switching between a manager and employee with the same ID", async () => {
    let resolveFirst!: (value: { language: string }) => void;
    mocks.request.mockImplementationOnce(() => new Promise(resolve => { resolveFirst = resolve; })).mockResolvedValueOnce({ language: "en" });
    const screenView = render(view());
    mocks.session = { role: "employee", managerId: undefined as unknown as number, employeeId: 1 };
    screenView.rerender(view());
    await screen.findByText("Profile access");
    await act(async () => resolveFirst({ language: "pl" }));
    expect(getLanguage()).toBe("en");
  });

  it("restores the account preference on a fresh mount", async () => {
    mocks.request.mockResolvedValue({ language: "pl" });
    const first = render(view());
    await screen.findByText("Dostęp do profilu");
    first.unmount();
    setLanguage("en");
    render(view());
    await waitFor(() => expect(getLanguage()).toBe("pl"));
    expect(screen.getByLabelText("Język aplikacji")).toHaveValue("pl");
  });
});
