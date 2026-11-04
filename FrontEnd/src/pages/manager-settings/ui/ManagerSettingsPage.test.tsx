vi.mock("@entities/employment-contracts/api/employmentContractSettingsApi", () => ({ employmentContractSettingsApi: {
  get: vi.fn().mockResolvedValue({ restrictSwapsToEightHours:true,requirePickupApproval:true,requireBelowMinimumGiveApproval:true,automaticWeekdayAvailability:true }),
  update: vi.fn(),
} }));
import { act, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter } from "react-router-dom";
import { PublicHolidayVisibilityProvider, usePublicHolidayVisibility } from "@app/providers/PublicHolidayVisibilityProvider";
import { priorityGenerationSettingsApi } from "@entities/priority-generation/api/priorityGenerationSettingsApi";
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

vi.mock("@entities/priority-generation/api/priorityGenerationSettingsApi", () => ({
  priorityGenerationSettingsApi: { get: vi.fn(), update: vi.fn() },
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
  vi.mocked(priorityGenerationSettingsApi.get).mockReset().mockResolvedValue({ shiftHours: 8, weekendsOff: true, respectAvailability: true });
  vi.mocked(priorityGenerationSettingsApi.update).mockReset().mockImplementation(async settings => {
    vi.mocked(priorityGenerationSettingsApi.get).mockResolvedValue(settings);
    return settings;
  });
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
    expect(screen.getByRole("switch", { name: "Show public holidays" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Language English" })).toBeDisabled();
  });
  test("does not request settings behind required documents and loads after acceptance", async () => {
    vi.mocked(regulationsApi.pending).mockResolvedValue([{ id: 9 } as RegulationDocument]);
    const { client } = mount(false);
    await waitFor(() => expect(regulationsApi.pending).toHaveBeenCalledOnce());
    expect(publicHolidaySettingsApi.get).not.toHaveBeenCalled();
    expect(screen.getByRole("switch", { name: "Show public holidays" })).toBeDisabled();
    vi.mocked(regulationsApi.pending).mockResolvedValue([]);
    await act(async () => client.invalidateQueries({ queryKey: queryKeys.regulations.all }));
    await waitFor(() => expect(screen.getByRole("switch", { name: "Show public holidays" })).toBeEnabled());
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
    const toggle = screen.getByRole("switch", { name: "Show public holidays" });
    await waitFor(() => expect(toggle).toBeEnabled());
    await userEvent.click(toggle);
    expect(await screen.findByRole("alert")).toHaveTextContent("Save failed");
    expect(toggle).toBeChecked(); expect(screen.getByTestId("visibility")).toHaveTextContent("true");
  });
  test("failed load defaults highlighting on, blocks writes, and retries", async () => {
    vi.mocked(publicHolidaySettingsApi.get).mockRejectedValue(new Error("Load failed"));
    mount();
    expect(await screen.findByRole("alert")).toHaveTextContent("Load failed");
    expect(screen.getByRole("switch", { name: "Show public holidays" })).toBeDisabled();
    expect(screen.getByTestId("visibility")).toHaveTextContent("true");
    vi.mocked(publicHolidaySettingsApi.get).mockResolvedValue({ showPublicHolidays: false });
    await userEvent.click(screen.getByRole("button", { name: "Retry" }));
    await waitFor(() => expect(screen.getByRole("switch", { name: "Show public holidays" })).toBeEnabled());
    expect(screen.getByRole("switch", { name: "Show public holidays" })).not.toBeChecked();
  });
  test("PC opens the dialog even when OFF, restoring focus and scroll", async () => {
    vi.mocked(publicHolidaySettingsApi.get).mockResolvedValue({ showPublicHolidays: false });
    mount();
    await waitFor(() => expect(screen.getByRole("switch", { name: "Show public holidays" })).not.toBeChecked());
    expect(screen.getByRole("switch", { name: "Show public holidays" })).toBeEnabled();
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
    expect(screen.getByRole("switch", { name: "Show public holidays" })).toBeEnabled();
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

test("priority settings persist only after successful PUT and preserve cached values on failure", async () => {
  const { client } = mount();
  const duration = await screen.findByRole("textbox", { name: "Priority shift duration" });
  await waitFor(() => expect(duration).toBeEnabled());
  fireEvent.change(duration, { target: { value: "10" } }); fireEvent.blur(duration);
  await waitFor(() => expect(priorityGenerationSettingsApi.update).toHaveBeenCalledWith({ shiftHours: 10, weekendsOff: true, respectAvailability: true }));
  await waitFor(() => expect(duration).toHaveValue("10"));
  vi.mocked(priorityGenerationSettingsApi.update).mockRejectedValueOnce(new Error("Priority save failed"));
  await userEvent.click(screen.getByRole("switch", { name: "Weekends off for priority employees" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("Priority save failed");
  expect(screen.getByRole("switch", { name: "Weekends off for priority employees" })).toBeChecked();
  expect(client.getQueryState(queryKeys.priorityGenerationSettings.current())?.data).toEqual({ shiftHours: 10, weekendsOff: true, respectAvailability: true });
});

test("priority settings disable writes in phone mode", async () => {
  auth.session.workspaceMode = "phone";
  mount();
  expect(screen.getByRole("textbox", { name: "Priority shift duration" })).toBeDisabled();
  expect(screen.getByRole("switch", { name: "Weekends off for priority employees" })).toBeDisabled();
  expect(screen.getByRole("switch", { name: "Respect priority employee availability" })).toBeDisabled();
  expect(priorityGenerationSettingsApi.update).not.toHaveBeenCalled();
});

test("priority settings failed load can be retried", async () => {
  vi.mocked(priorityGenerationSettingsApi.get).mockRejectedValueOnce(new Error("Priority load failed"));
  mount();
  expect(await screen.findByRole("alert")).toHaveTextContent("Priority load failed");
  expect(screen.getByRole("switch", { name: "Respect priority employee availability" })).toBeDisabled();
  expect(screen.getByRole("switch", { name: "Respect priority employee availability" })).toBeChecked();
  await userEvent.click(screen.getByRole("button", { name: "Retry" }));
  await waitFor(() => expect(screen.getByRole("textbox", { name: "Priority shift duration" })).toBeEnabled());
});

test("availability toggle uses server confirmation, blocks concurrent writes and survives reopening", async () => {
  let resolve!: (data: { shiftHours: number; weekendsOff: boolean; respectAvailability: boolean }) => void;
  vi.mocked(priorityGenerationSettingsApi.update).mockImplementationOnce(() => new Promise(done => { resolve = done; }));
  const view = mount();
  const toggle = screen.getByRole("switch", { name: "Respect priority employee availability" });
  expect(toggle).toBeChecked(); expect(toggle).toBeDisabled();
  await waitFor(() => expect(toggle).toBeEnabled());
  toggle.focus(); await userEvent.keyboard(" ");
  expect(toggle).toHaveFocus(); expect(toggle).toBeChecked(); expect(toggle).toBeDisabled();
  expect(screen.getByRole("switch", { name: "Weekends off for priority employees" })).toBeDisabled();
  expect(screen.getByRole("textbox", { name: "Priority shift duration" })).toBeDisabled();
  expect(priorityGenerationSettingsApi.update).toHaveBeenCalledExactlyOnceWith({ shiftHours: 8, weekendsOff: true, respectAvailability: false });
  const saved = { shiftHours: 8, weekendsOff: true, respectAvailability: false };
  vi.mocked(priorityGenerationSettingsApi.get).mockResolvedValue(saved);
  await act(async () => resolve(saved));
  await waitFor(() => expect(toggle).toBeEnabled()); expect(toggle).not.toBeChecked();
  view.unmount(); mount();
  await waitFor(() => expect(screen.getByRole("switch", { name: "Respect priority employee availability" })).not.toBeChecked());
});

test("duration and weekend saves preserve ignored availability and failed toggle can be retried", async () => {
  vi.mocked(priorityGenerationSettingsApi.get).mockResolvedValue({ shiftHours: 8, weekendsOff: true, respectAvailability: false });
  mount();
  const toggle = screen.getByRole("switch", { name: "Respect priority employee availability" });
  await waitFor(() => expect(toggle).toBeEnabled());
  const duration = screen.getByRole("textbox", { name: "Priority shift duration" });
  fireEvent.change(duration, { target: { value: "10" } }); fireEvent.blur(duration);
  await waitFor(() => expect(priorityGenerationSettingsApi.update).toHaveBeenLastCalledWith({ shiftHours: 10, weekendsOff: true, respectAvailability: false }));
  await waitFor(() => expect(toggle).toBeEnabled());
  await userEvent.click(screen.getByRole("switch", { name: "Weekends off for priority employees" }));
  await waitFor(() => expect(priorityGenerationSettingsApi.update).toHaveBeenLastCalledWith({ shiftHours: 10, weekendsOff: false, respectAvailability: false }));
  await waitFor(() => expect(toggle).toBeEnabled());
  vi.mocked(priorityGenerationSettingsApi.update).mockRejectedValueOnce(new Error("Availability save failed"));
  await userEvent.click(toggle);
  expect(await screen.findByRole("alert")).toHaveTextContent("Availability save failed");
  expect(toggle).not.toBeChecked();
  await userEvent.click(toggle);
  await waitFor(() => expect(toggle).toBeChecked());
});

test.each([
  ["en", "Respect priority employee availability", "Monday–Friday", "Weekends stay off"],
  ["pl", "Uwzględniaj dostępność pracowników priorytetowych", "od poniedziałku do piątku", "Weekendy pozostają wolne"],
  ["uk", "Враховувати доступність пріоритетних працівників", "з понеділка до п’ятниці", "Субота й неділя залишаються вихідними"],
] as const)("availability setting and precedence are translated in %s", async (locale, label, weekdays, weekends) => {
  setLanguage(locale); mount();
  const toggle = screen.getByRole("switch", { name: label });
  await waitFor(() => expect(toggle).toBeEnabled());
  expect(toggle).toHaveAccessibleDescription(expect.stringContaining(weekdays));
  expect(toggle).toHaveAccessibleDescription(expect.stringContaining(weekends));
});

test("a failed duration save restores the persisted value", async () => {
  vi.mocked(priorityGenerationSettingsApi.update).mockRejectedValueOnce(new Error("Duration save failed"));
  mount();
  await waitFor(() => expect(screen.getByRole("textbox", { name: "Priority shift duration" })).toBeEnabled());
  fireEvent.change(screen.getByRole("textbox", { name: "Priority shift duration" }), { target: { value: "10" } });
  fireEvent.blur(screen.getByRole("textbox", { name: "Priority shift duration" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("Duration save failed");
  expect(screen.getByRole("textbox", { name: "Priority shift duration" })).toHaveValue("8");
});
