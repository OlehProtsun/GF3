import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, expect, test, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter } from "react-router-dom";
import { setLanguage } from "@shared/i18n";
import { employmentContractSettingsApi as api } from "@entities/employment-contracts/api/employmentContractSettingsApi";
import { ManagerSettingsPage } from "./ManagerSettingsPage";
const auth = vi.hoisted(() => ({ session: { role: "manager", managerId: 1, workspaceMode: "pc" } }));
vi.mock("@app/providers/AuthProvider", () => ({ useAuth: () => auth }));
vi.mock("@app/providers/LanguageProvider", () => ({ useLanguage: () => ({ language: "en", ready: true }) }));
vi.mock("@entities/regulations/api/regulationsApi", () => ({ regulationsApi: { pending: vi.fn().mockResolvedValue([]) } }));
vi.mock("@entities/public-holidays/api/publicHolidaySettingsApi", () => ({ publicHolidaySettingsApi: { get: vi.fn().mockResolvedValue({ showPublicHolidays: true }) } }));
vi.mock("@entities/priority-generation/api/priorityGenerationSettingsApi", () => ({ priorityGenerationSettingsApi: { get: vi.fn().mockResolvedValue({ shiftHours: 8, weekendsOff: true, respectAvailability: true }) } }));
vi.mock("@entities/employment-contracts/api/employmentContractSettingsApi", () => ({ employmentContractSettingsApi: { get: vi.fn(), update: vi.fn() } }));
const defaults = { restrictSwapsToEightHours: true, requirePickupApproval: true, requireBelowMinimumGiveApproval: true, automaticWeekdayAvailability: true };
beforeEach(() => {
  setLanguage("en"); auth.session.workspaceMode = "pc";
  vi.mocked(api.get).mockReset().mockResolvedValue({ ...defaults });
  vi.mocked(api.update).mockReset().mockImplementation(async value => { vi.mocked(api.get).mockResolvedValue(value); return value; });
});
function mount() { return render(<QueryClientProvider client={new QueryClient()}><BrowserRouter><ManagerSettingsPage /></BrowserRouter></QueryClientProvider>); }
test("persists each independent switch without resetting the other rules", async () => {
  mount(); const user = userEvent.setup(); const card = await screen.findByRole("region", { name: "Employment contracts" });
  await waitFor(() => expect(within(card).getAllByRole("switch")[0]).toBeEnabled());
  const expected = { ...defaults };
  for (const [index,key] of Object.keys(defaults).entries()) {
    await user.click(within(card).getAllByRole("switch")[index]);
    expected[key as keyof typeof defaults] = false;
    await waitFor(() => expect(api.update).toHaveBeenLastCalledWith(expected));
    await waitFor(() => expect(within(card).getAllByRole("switch")[index]).toBeEnabled());
  }
  expect(within(card).getAllByRole("switch").every(input => !(input as HTMLInputElement).checked)).toBe(true);
});
test("manager phone mode makes all contract controls read only", async () => {
  auth.session.workspaceMode = "phone"; mount();
  const card = await screen.findByRole("region", { name: "Employment contracts" });
  for (const input of within(card).getAllByRole("switch")) expect(input).toBeDisabled();
  expect(api.update).not.toHaveBeenCalled();
});
