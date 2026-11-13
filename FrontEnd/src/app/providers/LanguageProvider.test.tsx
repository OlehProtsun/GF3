import { act, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useState } from "react";
import { LanguageProvider } from "./LanguageProvider";
import { LanguageSelector } from "@shared/i18n/LanguageSelector";
import { useLanguageRevision } from "@shared/i18n/useLanguageRevision";
import { getLanguage, setLanguage, t } from "@shared/i18n";

type TestSession = { role: string; managerId?: number; employeeId?: number; workspaceMode?: "choose" | "pc" | "phone" } | null;
const mocks = vi.hoisted(() => ({ request: vi.fn(), session: { role: "manager", managerId: 1 } as TestSession }));
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
  setLanguage("pl");
});
afterEach(() => { cleanup(); setLanguage("pl"); });

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
    mocks.session = { role: "employee", managerId: undefined, employeeId: 1 };
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
    setLanguage("pl");
    render(view());
    await waitFor(() => expect(getLanguage()).toBe("pl"));
    expect(screen.getByLabelText("Język aplikacji")).toHaveValue("pl");
  });
});

it.each([null, { role: "manager", managerId: 1, workspaceMode: "choose" as const }])("uses Polish without a protected request for %j", async session => {
  mocks.session = session;
  setLanguage("en");
  render(view());
  await screen.findByText("Dost\u0119p do profilu");
  expect(getLanguage()).toBe("pl");
  expect(mocks.request).not.toHaveBeenCalled();
});
it.each([{}, null, { language: null }, { language: "de" }, { language: "pl" }])("uses Polish for a fresh or malformed preference %j without saving it", async result => {
  mocks.request.mockResolvedValue(result);
  render(view());
  await screen.findByText("Dost\u0119p do profilu");
  expect(getLanguage()).toBe("pl");
  expect(mocks.request).toHaveBeenCalledTimes(1);
});
it("shows a Polish load error and retries the saved English preference", async () => {
  mocks.request.mockRejectedValueOnce(new Error("offline")).mockResolvedValueOnce({ language: "en" });
  render(view());
  expect(await screen.findByRole("alert")).toHaveTextContent(t("Could not load your language preference."));
  fireEvent.click(screen.getByRole("button", { name: t("Retry") }));
  await screen.findByText("Profile access");
  expect(getLanguage()).toBe("en");
});

it("loads and saves Ukrainian while preserving the draft and the last confirmed language on failure", async () => {
  mocks.request.mockResolvedValueOnce({ language: "uk" }).mockRejectedValueOnce(new Error("offline")).mockResolvedValueOnce({ language: "en" }).mockResolvedValueOnce({ language: "uk" });
  const first = render(view());
  await screen.findByText("Доступ до профілю");
  fireEvent.change(screen.getByLabelText("Draft"), { target: { value: "Незбережені дані" } });
  expect(screen.getByLabelText("Мова застосунку")).toHaveValue("uk");
  fireEvent.change(screen.getByLabelText("Мова застосунку"), { target: { value: "en" } });
  await screen.findByRole("alert");
  expect(getLanguage()).toBe("uk");
  expect(screen.getByLabelText("Draft")).toHaveValue("Незбережені дані");
  fireEvent.change(screen.getByLabelText("Мова застосунку"), { target: { value: "en" } });
  await screen.findByText("Profile access");
  fireEvent.change(screen.getByLabelText("Application language"), { target: { value: "uk" } });
  await screen.findByText("Доступ до профілю");
  expect(screen.getByLabelText("Draft")).toHaveValue("Незбережені дані");
  expect(mocks.request).toHaveBeenLastCalledWith("account-language", expect.objectContaining({ method: "PUT", body: { language: "uk" } }));
  first.unmount();
  mocks.request.mockResolvedValueOnce({ language: "uk" });
  render(view());
  await screen.findByText("Доступ до профілю");
  expect(getLanguage()).toBe("uk");
});
