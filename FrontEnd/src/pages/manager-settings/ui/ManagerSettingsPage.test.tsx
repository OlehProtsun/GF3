import { act, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter } from "react-router-dom";
import { PublicHolidayVisibilityProvider, usePublicHolidayVisibility } from "@app/providers/PublicHolidayVisibilityProvider";
import { publicHolidaySettingsApi } from "@entities/public-holidays/api/publicHolidaySettingsApi";
import { regulationsApi } from "@entities/regulations/api/regulationsApi";
import type { RegulationDocument } from "@entities/regulations/model/types";
import { queryKeys } from "@shared/api/queryKeys";
import { setLanguage } from "@shared/i18n";
import { ManagerSettingsPage } from "./ManagerSettingsPage";
import { PublicHolidayListDialog } from "./PublicHolidayListDialog";

const auth = vi.hoisted(() => ({ session: { workspaceMode: "pc" } }));
const language = vi.hoisted(() => ({ language: "en" as "en" | "pl" | "uk", ready: true, save: vi.fn() }));
vi.mock("@app/providers/AuthProvider", () => ({ useAuth: () => auth }));
vi.mock("@app/providers/LanguageProvider", () => ({ useLanguage: () => language }));
vi.mock("@entities/regulations/api/regulationsApi", () => ({ regulationsApi: { pending: vi.fn().mockResolvedValue([]) } }));
vi.mock("@entities/public-holidays/api/publicHolidaySettingsApi", () => ({
  publicHolidaySettingsApi: { get: vi.fn(), update: vi.fn() },
}));

function VisibilityProbe() {
  return <output data-testid="visibility">{String(usePublicHolidayVisibility())}</output>;
}
function mount(enabled = true) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: 0 } } });
  const view = render(<QueryClientProvider client={client}>
    <PublicHolidayVisibilityProvider enabled={enabled}><BrowserRouter><ManagerSettingsPage /></BrowserRouter><VisibilityProbe /></PublicHolidayVisibilityProvider>
  </QueryClientProvider>);
  return { ...view, client };
}
beforeEach(() => {
  setLanguage("en"); auth.session.workspaceMode = "pc";
  language.language = "en"; language.ready = true;
  language.save.mockReset().mockImplementation(async (value: "en" | "pl" | "uk") => { language.language = value; setLanguage(value); });
  vi.mocked(regulationsApi.pending).mockReset().mockResolvedValue([]);
  vi.mocked(publicHolidaySettingsApi.get).mockReset().mockResolvedValue({ showPublicHolidays: true });
  vi.mocked(publicHolidaySettingsApi.update).mockReset();
});
afterEach(() => { vi.useRealTimers(); vi.restoreAllMocks(); });

describe("ManagerSettingsPage", () => {
  test("switches all three account languages immediately without changing the global holiday setting", async () => {
    mount();
    await userEvent.click(screen.getByRole("button", { name: "Language English" }));
    expect(screen.getAllByRole("radio")).toHaveLength(3);
    expect(screen.getByRole("radio", { name: "English" })).toBeChecked();
    await userEvent.click(screen.getByRole("radio", { name: "Polski" }));
    expect(await screen.findByRole("heading", { name: "Ustawienia", level: 1 })).toBeVisible();
    expect(screen.getByRole("radio", { name: "Polski" })).toBeChecked();
    await userEvent.click(screen.getByRole("radio", { name: "Українська" }));
    expect(await screen.findByRole("heading", { name: "Налаштування", level: 1 })).toBeVisible();
    await userEvent.click(screen.getByRole("radio", { name: "English" }));
    expect(await screen.findByRole("heading", { name: "Settings", level: 1 })).toBeVisible();
    expect(language.save.mock.calls.map(call => call[0])).toEqual(["pl", "uk", "en"]);
    expect(publicHolidaySettingsApi.update).not.toHaveBeenCalled();
    await userEvent.keyboard("{Escape}");
    expect(screen.getByRole("button", { name: "Language English" })).toHaveFocus();
  });
  test("language saving disables duplicate writes and keeps the old choice on failure", async () => {
    let reject!: (error: Error) => void;
    language.save.mockReturnValue(new Promise((_resolve, fail) => { reject = fail; }));
    mount();
    await userEvent.click(screen.getByRole("button", { name: "Language English" }));
    await userEvent.click(screen.getByRole("radio", { name: "Polski" }));
    expect(screen.getByRole("radio", { name: "Українська" })).toBeDisabled();
    expect(screen.getByRole("radio", { name: "English" })).toBeChecked();
    await act(async () => reject(new Error("Language save failed")));
    expect(await screen.findByRole("alert")).toHaveTextContent("Language save failed");
    expect(screen.getByRole("radio", { name: "English" })).toBeChecked();
    expect(language.save).toHaveBeenCalledOnce();
  });
  test("language dialog traps focus around the selected radio and supports arrow keys", async () => {
    mount();
    await userEvent.click(screen.getByRole("button", { name: "Language English" }));
    expect(screen.getByRole("button", { name: "Close" })).toHaveFocus();
    await userEvent.tab();
    expect(screen.getByRole("radio", { name: "English" })).toHaveFocus();
    await userEvent.tab();
    expect(screen.getByRole("button", { name: "Close" })).toHaveFocus();
    await userEvent.tab({ shift: true });
    expect(screen.getByRole("radio", { name: "English" })).toHaveFocus();
    await userEvent.keyboard("{ArrowDown}");
    await waitFor(() => expect(screen.getByRole("radio", { name: "Українська" })).toBeChecked());
    expect(language.save).toHaveBeenCalledWith("uk");
  });
  test("phone mode cannot edit either setting", async () => {
    auth.session.workspaceMode = "phone";
    mount();
    expect(screen.getByRole("switch")).toBeDisabled();
    expect(screen.getByRole("button", { name: "Language English" })).toBeDisabled();
  });
  test("does not request settings behind required documents and loads after acceptance", async () => {
    vi.mocked(regulationsApi.pending).mockResolvedValue([{ id: 9 } as RegulationDocument]);
    const { client } = mount(false);
    await waitFor(() => expect(regulationsApi.pending).toHaveBeenCalledOnce());
    expect(publicHolidaySettingsApi.get).not.toHaveBeenCalled();
    expect(screen.getByRole("switch")).toBeDisabled();
    vi.mocked(regulationsApi.pending).mockResolvedValue([]);
    await act(async () => client.invalidateQueries({ queryKey: queryKeys.regulations.all }));
    await waitFor(() => expect(screen.getByRole("switch")).toBeEnabled());
    expect(publicHolidaySettingsApi.get).toHaveBeenCalledOnce();
  });
  test("uses the shared collapsible page header with Back navigation", async () => {
    mount();
    expect(screen.getByRole("heading", { name: "Settings", level: 1 })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Back", exact: true })).toBeVisible();
    await userEvent.click(screen.getByRole("button", { name: "Collapse header" }));
    expect(screen.getByRole("button", { name: "Expand header" })).toBeVisible();
  });
  test.each([true, false])("loads controlled state %s and saves only after response", async enabled => {
    vi.mocked(publicHolidaySettingsApi.get).mockResolvedValue({ showPublicHolidays: enabled });
    let resolve!: (data: { showPublicHolidays: boolean }) => void;
    vi.mocked(publicHolidaySettingsApi.update).mockReturnValue(new Promise(done => { resolve = done; }));
    mount();
    const toggle = screen.getByRole("switch", { name: "Show public holidays" });
    await waitFor(() => expect(toggle).toBeEnabled());
    expect(toggle).toHaveProperty("checked", enabled);
    await userEvent.click(toggle);
    expect(publicHolidaySettingsApi.update).toHaveBeenCalledExactlyOnceWith(!enabled);
    expect(toggle).toBeDisabled(); expect(toggle).toHaveProperty("checked", enabled);
    vi.mocked(publicHolidaySettingsApi.get).mockResolvedValue({ showPublicHolidays: !enabled });
    await act(async () => resolve({ showPublicHolidays: !enabled }));
    await waitFor(() => expect(toggle).toBeEnabled());
    expect(toggle).toHaveProperty("checked", !enabled);
    expect(screen.getByTestId("visibility")).toHaveTextContent(String(!enabled));
  });
  test("failed save keeps the fetched value and reports failure", async () => {
    vi.mocked(publicHolidaySettingsApi.update).mockRejectedValue(new Error("Save failed"));
    mount();
    const toggle = screen.getByRole("switch");
    await waitFor(() => expect(toggle).toBeEnabled());
    await userEvent.click(toggle);
    expect(await screen.findByRole("alert")).toHaveTextContent("Save failed");
    expect(toggle).toBeChecked(); expect(screen.getByTestId("visibility")).toHaveTextContent("true");
  });
  test("failed load defaults highlighting on, blocks writes, and retries", async () => {
    vi.mocked(publicHolidaySettingsApi.get).mockRejectedValue(new Error("Load failed"));
    mount();
    expect(await screen.findByRole("alert")).toHaveTextContent("Load failed");
    expect(screen.getByRole("switch")).toBeDisabled();
    expect(screen.getByTestId("visibility")).toHaveTextContent("true");
    vi.mocked(publicHolidaySettingsApi.get).mockResolvedValue({ showPublicHolidays: false });
    await userEvent.click(screen.getByRole("button", { name: "Retry" }));
    await waitFor(() => expect(screen.getByRole("switch")).toBeEnabled());
    expect(screen.getByRole("switch")).not.toBeChecked();
  });
  test("PC opens the dialog even when OFF, restoring focus and scroll", async () => {
    vi.mocked(publicHolidaySettingsApi.get).mockResolvedValue({ showPublicHolidays: false });
    mount();
    await waitFor(() => expect(screen.getByRole("switch")).not.toBeChecked());
    expect(screen.getByRole("switch")).toBeEnabled();
    const opener = screen.getByRole("button", { name: "View public holidays" });
    await userEvent.click(opener);
    expect(screen.getByRole("dialog")).toBeVisible();
    expect(document.body.style.overflow).toBe("hidden");
    expect(screen.getByRole("button", { name: "Close" })).toHaveFocus();
    await userEvent.keyboard("{Escape}");
    expect(screen.queryByRole("dialog")).toBeNull(); expect(opener).toHaveFocus();
    expect(document.body.style.overflow).toBe("");
    expect(publicHolidaySettingsApi.update).not.toHaveBeenCalled();
  });
  test("polls and refreshes on focus, with immediate shared context updates", async () => {
    vi.spyOn(document, "visibilityState", "get").mockReturnValue("visible");
    vi.useFakeTimers();
    await act(async () => { mount(); });
    expect(screen.getByRole("switch")).toBeEnabled();
    vi.mocked(publicHolidaySettingsApi.get).mockResolvedValue({ showPublicHolidays: false });
    await act(async () => { vi.advanceTimersByTime(60_000); });
    expect(screen.getByTestId("visibility")).toHaveTextContent("false");
    vi.mocked(publicHolidaySettingsApi.get).mockResolvedValue({ showPublicHolidays: true });
    await act(async () => { window.dispatchEvent(new Event("focus")); });
    expect(screen.getByTestId("visibility")).toHaveTextContent("true");
  });
  test("guest provider makes no request", () => {
    render(<QueryClientProvider client={new QueryClient()}><PublicHolidayVisibilityProvider enabled={false}><VisibilityProbe /></PublicHolidayVisibilityProvider></QueryClientProvider>);
    expect(publicHolidaySettingsApi.get).not.toHaveBeenCalled();
  });
});

describe("PublicHolidayListDialog", () => {
  test("2026 list is ordered, has 14 entries and the official link, and supports year boundaries", async () => {
    const close = vi.fn();
    render(<PublicHolidayListDialog open onClose={close} initialYear={2026} />);
    const dialog = screen.getByRole("dialog", { name: "Public holidays in 2026" });
    expect(within(dialog).getAllByRole("listitem")).toHaveLength(14);
    expect([...dialog.querySelectorAll("time")].map(node => node.textContent)).toEqual([
      "01.01.2026", "06.01.2026", "05.04.2026", "06.04.2026", "01.05.2026", "03.05.2026", "24.05.2026", "04.06.2026", "15.08.2026", "01.11.2026", "11.11.2026", "24.12.2026", "25.12.2026", "26.12.2026",
    ]);
    expect(screen.getByRole("link", { name: "Official legal source" })).toHaveAttribute("href", "https://eli.gov.pl/eli/DU/2025/296/ogl/pol");
    expect(screen.getByRole("link")).toHaveAttribute("rel", "noopener noreferrer");
    fireEvent.change(screen.getByRole("spinbutton", { name: "Year" }), { target: { value: "2024" } });
    expect(screen.queryByText("Christmas Eve")).toBeNull();
    await userEvent.click(screen.getByRole("button", { name: "Next year" }));
    expect(screen.getByText("Christmas Eve")).toBeVisible();
    fireEvent.change(screen.getByRole("spinbutton"), { target: { value: "1" } });
    expect(screen.getByRole("button", { name: "Previous year" })).toBeDisabled();
    fireEvent.change(screen.getByRole("spinbutton"), { target: { value: "9999" } });
    expect(screen.getByRole("button", { name: "Next year" })).toBeDisabled();
    fireEvent.change(screen.getByRole("spinbutton"), { target: { value: "10000" } });
    expect(screen.getByRole("spinbutton")).toHaveValue(9999);
    await userEvent.click(screen.getByRole("button", { name: "Close" }));
    expect(close).toHaveBeenCalledOnce();
  });
  test("traps keyboard focus, closes backdrop, resets year on reopening and cleans scroll lock", async () => {
    const close = vi.fn();
    const view = render(<PublicHolidayListDialog open onClose={close} initialYear={2026} />);
    await userEvent.tab({ shift: true });
    expect(screen.getByRole("link")).toHaveFocus();
    await userEvent.tab();
    expect(screen.getByRole("button", { name: "Close" })).toHaveFocus();
    await userEvent.click(screen.getByRole("button", { name: "Next year" }));
    fireEvent.click(screen.getByRole("dialog").parentElement!);
    expect(close).toHaveBeenCalledOnce();
    view.rerender(<PublicHolidayListDialog open={false} onClose={close} initialYear={2026} />);
    expect(document.body.style.overflow).toBe("");
    view.rerender(<PublicHolidayListDialog open onClose={close} initialYear={2026} />);
    expect(screen.getByRole("dialog", { name: "Public holidays in 2026" })).toBeVisible();
  });
});
